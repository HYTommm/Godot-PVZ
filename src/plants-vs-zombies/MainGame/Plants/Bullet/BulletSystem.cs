using Godot;
using System.Collections.Generic;

/// <summary>
/// 子弹的数据集合：一个预分配的 <see cref="BulletData"/> 数组，每帧几趟遍历处理。
///
/// 子弹没有节点。画面走 RenderingServer 直渲（每行一个画布项），判定自己算矩形。
/// 数组用栈式 swap-remove：活跃的永远挤在 [0, Count) 这一段，遍历不会撞上空槽，
/// 也不会像节点那样每条一次指针跳转。
///
/// 每帧分四趟，同一种操作连续做完再换下一种——别写成"一条子弹一条龙走完"：
/// 移动 → 碰撞 → 压实 → 提交渲染。
/// </summary>
public class BulletSystem
{
	/// <summary>
	/// 上限。满了就不再生成（静默丢弃，不然高压下会刷屏）。
	/// 8192 条 × 48 字节 ≈ 393 KB，预分配一次，随便给。
	/// 留这么大是因为超频豌豆 20 株能压到 2000 发/秒，子弹又在场上滞留一秒多。
	/// </summary>
	public const int Capacity = 8192;

	/// <summary>子弹速度（像素/秒）。沿用节点版本 Bullet._PhysicsProcess 里的字面量</summary>
	private const float SpeedPxPerSec = 333f;

	/// <summary>飞过这个 x 就作废。同样沿用节点版本里的字面量</summary>
	private const float CullX = 1000f;

	private const int Damage = 20;

	private readonly BulletData[] _bullets = new BulletData[Capacity];
	private int _count;

	/// <summary>每帧重建的活跃僵尸列表。复用同一个 List，不每帧新建</summary>
	private readonly List<Zombie> _activeZombies = new();

	// ── 渲染 ──
	/// <summary>每行一个画布项。子弹的绘制层级是按行的，一个画布项只能整体设一个 z_index</summary>
	private Rid[] _rowCanvasItems;
	private readonly Texture2D _peaTexture;
	private readonly Texture2D _snowPeaTexture;
	private readonly Texture2D _shadowTexture;
	private readonly Vector2 _peaSize;
	private readonly Vector2 _snowPeaSize;
	private readonly Vector2 _shadowFrameSize;
	private bool _initialized;

	/// <summary>活跃子弹数。调优时看它</summary>
	public int Count => _count;

	/// <summary>诊断用：累计建过多少个画布项。跟 <see cref="TotalFreed"/> 对上才说明没漏</summary>
	public static int TotalCreated { get; private set; }

	/// <summary>诊断用：累计释放过多少个画布项</summary>
	public static int TotalFreed { get; private set; }

	/// <summary>
	/// 临时隔离开关。关掉之后整个 RenderingServer 这一层都不碰——
	/// 不建画布项、不提交绘制，子弹在画面上不可见，但移动、碰撞、扣血照跑。
	/// 用来判断 RID 泄漏是不是渲染这一层引起的。查完就该改回 true。
	/// </summary>
	public static bool BEnableRendering = true;

	public BulletSystem()
	{
		_peaTexture = GD.Load<Texture2D>("res://art/MainGame/Plants/Bullet/ProjectilePea.png");
		_snowPeaTexture = GD.Load<Texture2D>("res://art/MainGame/Plants/Bullet/ProjectileSnowPea.png");
		_shadowTexture = GD.Load<Texture2D>("res://art/MainGame/Plants/Bullet/pea_shadows.png");

		_peaSize = _peaTexture?.GetSize() ?? new Vector2(20, 20);
		_snowPeaSize = _snowPeaTexture?.GetSize() ?? _peaSize;
		// 影子贴图是横排 2 帧（节点版本里 hframes = 2），取其中一帧的尺寸
		Vector2 shadowSize = _shadowTexture?.GetSize() ?? new Vector2(20, 6);
		_shadowFrameSize = new Vector2(shadowSize.X / 2f, shadowSize.Y);
	}

	/// <summary>
	/// 建每行的画布项。得等场景就绪（host 进了树、行数定下来）才能调，
	/// 所以不在构造函数里做。
	/// </summary>
	public void Initialize(Node2D host, int rowCount)
	{
		if (_initialized || rowCount <= 0)
		{
			return;
		}

		if (!BEnableRendering)
		{
			// 隔离实验：一个画布项都不建，逻辑照跑。SubmitRender 见 _rowCanvasItems 为 null 会自己返回
			GD.Print("[BulletSystem] 渲染已关闭（BEnableRendering = false），只跑碰撞");
			_initialized = true;
			return;
		}

		_rowCanvasItems = new Rid[rowCount];
		Rid parent = host.GetCanvasItem();

		for (int row = 0; row < rowCount; row++)
		{
			Rid item = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetParent(item, parent);
			// 像素画必须设最近邻，默认的双线性会把贴图糊掉
			RenderingServer.CanvasItemSetDefaultTextureFilter(item,
				RenderingServer.CanvasItemTextureFilter.Nearest);
			// 和实体同一套公式：(row + 1) * 10 + 类别，所以子弹正好在同行植物之上
			RenderingServer.CanvasItemSetZIndex(item, (row + 1) * 10 + (int)ZIndexEnum.Bullets);
			_rowCanvasItems[row] = item;
		}

		TotalCreated += rowCount;
		GD.Print($"[BulletSystem] Initialize：建 {rowCount} 个画布项（累计建 {TotalCreated} / 释 {TotalFreed}）");
		_initialized = true;
	}

	public void Dispose()
	{
		if (!_initialized || _rowCanvasItems == null)
		{
			return;
		}

		foreach (Rid item in _rowCanvasItems)
		{
			RenderingServer.FreeRid(item);
		}
		TotalFreed += _rowCanvasItems.Length;
		GD.Print($"[BulletSystem] Dispose：释放 {_rowCanvasItems.Length} 个画布项" +
			$"（累计建 {TotalCreated} / 释 {TotalFreed}）");

		_rowCanvasItems = null;
		_initialized = false;
	}

	/// <summary>生成一条子弹。数组满了就丢掉这次——宁可少一发，也不要在这里扩容</summary>
	public void Spawn(BulletKind kind, float x, float y, int row, float shadowY)
	{
		if (!_initialized || _count >= Capacity)
		{
			return;
		}

		ref BulletData b = ref _bullets[_count++];
		b.X = x;
		b.Y = y;
		b.VelX = SpeedPxPerSec;
		b.VelY = 0f;
		b.AccY = 0f;
		b.ShadowY = shadowY;
		b.Kind = kind;
		b.Damage = Damage;
		b.Type = HurtType.Direct;
		b.PenetrateLeft = 1;
		b.Row = row;
		b.Alive = true;
	}

	/// <summary>
	/// 逻辑那三趟：移动 → 碰撞 → 压实。
	///
	/// 由 MainGame._PhysicsProcess 驱动，跑在固定的物理帧上——跟原节点版本一样。
	/// 判定频率不该跟着渲染帧率浮动：高刷机器上渲染帧是物理帧的好几倍，
	/// 挂在渲染帧上的话同一批子弹会被反复判定，纯属白烧。
	/// </summary>
	public void StepPhysics(double delta)
	{
		if (!_initialized)
		{
			return;
		}

		float dt = (float)delta;
		Move(dt);
		Collide();
		Compact();
	}

	// ── 第一趟：移动并标记越界 ──
	private void Move(float dt)
	{
		for (int i = 0; i < _count; i++)
		{
			ref BulletData b = ref _bullets[i];
			if (b.AccY != 0f)
			{
				b.VelY += b.AccY * dt;
			}
			b.X += b.VelX * dt;
			b.Y += b.VelY * dt;

			if (b.X > CullX)
			{
				b.Alive = false;
			}
		}
	}

	// ── 第二趟：碰撞 ──
	/// <summary>
	/// 僵尸矩形 × 子弹矩形，全自己算，物理引擎不参与。
	///
	/// **僵尸当外层、子弹当内层**。反过来写的话，外层每条子弹都要扫一遍 1000 个僵尸槽位，
	/// 而槽位绝大部分是 null；场上的僵尸只有几十只，让它们当外层，
	/// 内层扫的就是紧凑的子弹数组 [0, _count)，量级差两个数量级。
	/// Row 过滤留在这里：僵尸本来就只跟自己那一行的子弹发生关系。
	///
	/// 僵尸按 x 从小到大扫，所以两只僵尸重叠时子弹先被最靠前那只吃掉——正是原版的规则。
	/// </summary>
	private void Collide()
	{
		Zombie[] zombies = MainGame.Instance?.Zombies;
		if (zombies == null)
		{
			return;
		}

		CollectActiveZombies(zombies);

		for (int zi = 0; zi < _activeZombies.Count; zi++)
		{
			Zombie zombie = _activeZombies[zi];
			IHitBox defense = zombie.DefenseHitBox;
			if (!zombie.Alive || defense == null)
			{
				continue;
			}

			Rect2 zombieRect = defense.GlobalRect;

			for (int i = 0; i < _count; i++)
			{
				if (!zombie.Alive)
				{
					break; // 刚被前面的子弹打死，剩下的子弹不该再喂给它
				}

				ref BulletData b = ref _bullets[i];
				if (!b.Alive || b.Row != zombie.Row)
				{
					continue;
				}

				if (!HitRect(b).Intersects(zombieRect, true))
				{
					continue;
				}

				Hit(ref b, zombie);
			}
		}
	}

	/// <summary>
	/// 收活跃僵尸并按 x 升序排。僵尸只有几十只，所以用插入排序：它零分配，
	/// 这个量级下也比 List.Sort 快（后者要包一层比较器）。
	/// 排序是为了让"最靠前的那只先吃子弹"，重叠时才有一致的先后。
	/// </summary>
	private void CollectActiveZombies(Zombie[] zombies)
	{
		_activeZombies.Clear();
		foreach (Zombie zombie in zombies)
		{
			if (zombie != null && zombie.Alive && zombie.DefenseHitBox != null)
			{
				_activeZombies.Add(zombie);
			}
		}

		for (int i = 1; i < _activeZombies.Count; i++)
		{
			Zombie key = _activeZombies[i];
			float keyX = key.Position.X;
			int j = i - 1;
			while (j >= 0 && _activeZombies[j].Position.X > keyX)
			{
				_activeZombies[j + 1] = _activeZombies[j];
				j--;
			}
			_activeZombies[j + 1] = key;
		}
	}

	private static void Hit(ref BulletData b, Zombie zombie)
	{
		zombie.Hurt(new Hurt(b.Damage, b.Type));
		if (b.Kind == BulletKind.SnowPea)
		{
			zombie.AddStatusEffect(new SlowEffect());
		}
		b.Alive = false;
	}

	// ── 第三趟：把作废的挤出去 ──
	private void Compact()
	{
		int i = 0;
		while (i < _count)
		{
			if (_bullets[i].Alive)
			{
				i++;
			}
			else
			{
				// 拿尾巴那条顶替，然后**不推进下标**——顶上来的还没检查过
				_bullets[i] = _bullets[--_count];
			}
		}
	}

	// ── 第四趟：提交渲染 ──
	/// <summary>
	/// 把当前所有子弹提交给 RenderingServer。
	///
	/// 这一趟必须每**渲染**帧都跑（由 MainGame._Process 驱动）：立即模式下
	/// 每帧开始时绘制列表都是空的，不重新提交就什么都看不见。
	/// 物理帧之间数据没变时是重复提交，但这是这一层的要求，省不掉。
	/// </summary>
	public void SubmitRender()
	{
		if (!_initialized || _rowCanvasItems == null)
		{
			return;
		}

		for (int row = 0; row < _rowCanvasItems.Length; row++)
		{
			RenderingServer.CanvasItemClear(_rowCanvasItems[row]);
		}

		bool hasShadow = _shadowTexture != null;
		Rid shadowRid = hasShadow ? _shadowTexture.GetRid() : default;
		var shadowSrc = new Rect2(0f, 0f, _shadowFrameSize.X, _shadowFrameSize.Y);

		for (int i = 0; i < _count; i++)
		{
			ref readonly BulletData b = ref _bullets[i];
			Rid item = _rowCanvasItems[ClampRow(b.Row)];

			// 影子先提交：同一画布项里，后提交的盖在先提交的上面
			if (hasShadow)
			{
				RenderingServer.CanvasItemAddTextureRectRegion(item, ShadowRect(b), shadowRid,
					shadowSrc, Colors.White, false, false);
			}

			RenderingServer.CanvasItemAddTextureRect(item, BodyRect(b), BodyTexture(b.Kind),
				false, Colors.White, false);
		}
	}

	private int ClampRow(int row)
	{
		if (row < 0)
		{
			return 0;
		}
		return row >= _rowCanvasItems.Length ? _rowCanvasItems.Length - 1 : row;
	}

	/// <summary>子弹本体的绘制矩形。Bullet.tscn 里 Sprite2D 是 centered = false，原点就是左上角</summary>
	private Rect2 BodyRect(in BulletData b)
	{
		Vector2 size = b.Kind == BulletKind.SnowPea ? _snowPeaSize : _peaSize;
		return new Rect2(b.X, b.Y, size);
	}

	/// <summary>阴影同理，也是 centered = false，位置就是左上角</summary>
	private Rect2 ShadowRect(in BulletData b)
	{
		return new Rect2(b.X, b.ShadowY, _shadowFrameSize);
	}

	/// <summary>
	/// 子弹的命中矩形。用的是旧 Bullet.tscn 里那套碰撞箱的数值：
	/// HitBox 在 (-14, 0)、其 CollisionShape2D 在 (28, 13.5)、形状 56×27，
	/// 合起来就是左上角在 (X - 14, Y)、尺寸 56×27。
	/// 它比贴图大一圈，这是原样搬过来的——判定的语义要复刻，不跟着美术走。
	/// </summary>
	private static Rect2 HitRect(in BulletData b)
	{
		return new Rect2(b.X - 14f, b.Y, 56f, 27f);
	}

	private Rid BodyTexture(BulletKind kind)
	{
		Texture2D texture = kind == BulletKind.SnowPea ? _snowPeaTexture : _peaTexture;
		return texture?.GetRid() ?? default;
	}
}
