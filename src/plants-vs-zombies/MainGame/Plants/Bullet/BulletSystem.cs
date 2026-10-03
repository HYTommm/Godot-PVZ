using Godot;
using System.Collections.Generic;

/// <summary>
/// 子弹的数据集合：一个预分配的 <see cref="BulletData"/> 数组，每帧几趟遍历处理。
///
/// 子弹没有节点。画面走 RenderingServer 直渲——**每行每类一个 MultiMesh**，
/// 每帧只提交十几次绘制命令，而不是每条子弹两次；
/// 判定自己算矩形，物理引擎不参与。
///
/// 数组用栈式 swap-remove：活跃的永远挤在 [0, Count) 这一段，遍历不会撞上空槽，
/// 也不会像节点那样每条一次指针跳转。
///
/// 逻辑三趟（移动 → 碰撞 → 压实）跑物理帧，绘制单独跑渲染帧，见
/// <see cref="StepPhysics"/> 与 <see cref="SubmitRender"/>。
/// </summary>
public class BulletSystem
{
	/// <summary>
	/// 上限。满了就不再生成（静默丢弃，不然高压下会刷屏）。
	/// 8192 条 × 48 字节 ≈ 393 KB，预分配一次，随便给。
	/// </summary>
	public const int Capacity = 8192;

	/// <summary>子弹速度（像素/秒）。沿用节点版本 Bullet._PhysicsProcess 里的字面量</summary>
	private const float SpeedPxPerSec = 333f;

	/// <summary>飞过这个 x 就作废。同样沿用节点版本里的字面量</summary>
	private const float CullX = 1000f;

	private const int Damage = 20;

	/// <summary>
	/// 每行每类预分配的实例数。一行里同类子弹超过这个数，多出来的那部分画不出来
	/// （逻辑照跑，只是看不见）。
	///
	/// 这个值同时决定每帧要整段上传多少 buffer（InstancesPerRow × 8 个 float），
	/// 所以不能随便往大给：1024 大约是每帧 32 KB/行/类，十几路加起来不到 500 KB。
	/// 摆两排植物时一行能堆到近千颗，512 会画不全，所以取 1024
	/// </summary>
	private const int InstancesPerRow = 1024;

	/// <summary>MultiMesh 里一个 2D 实例在 buffer 里占的 float 数</summary>
	private const int FloatsPerInstance = 8;

	private readonly BulletData[] _bullets = new BulletData[Capacity];
	private int _count;

	/// <summary>每帧重建的活跃僵尸列表。复用同一个 List，不每帧新建</summary>
	private readonly List<Zombie> _activeZombies = new();

	/// <summary>
	/// 子弹下标按行分桶，外加一个跨行子弹的全局桶。
	///
	/// 分桶是为了让每只僵尸只跟自己那一行的子弹比，而不是扫全部子弹——PVZ 的规则本来就是
	/// "子弹只跟同行的僵尸发生关系"，按行切开就是最贴合规则的空间划分，比四叉树那种
	/// 通用空间结构简单也更快。每帧重建一次，O(子弹数)，很便宜。
	///
	/// **全局桶**装的是不属于任何一行的子弹：杨桃那种斜线弹会跨行，行桶装不下它，
	/// 每只僵尸都得过一遍。Row 小于 0 就进这里。
	/// </summary>
	private int[][] _rowBuckets;
	private int[] _rowBucketCounts;
	private int[] _globalBucket;
	private int _globalBucketCount;

	/// <summary>命中表现（溅射粒子 + 音效）。同样是池化的，运行时零节点创建</summary>
	private readonly HitEffectSystem _effects = new();

	// ── 渲染 ──

	/// <summary>每行一个画布项。子弹的绘制层级是按行的，一个画布项只能整体设一个 z_index</summary>
	private Rid[] _rowCanvasItems;

	// 每行三个 MultiMesh：豌豆本体、寒冰本体、阴影。
	// 弹种贴图不同就得分开，MultiMesh 一套只能配一张贴图
	private MultiMesh[] _peaMeshes;
	private MultiMesh[] _snowPeaMeshes;
	private MultiMesh[] _shadowMeshes;

	// 每帧往里写实例变换，预分配好，避免每帧新建数组
	private float[][] _peaBuffers;
	private float[][] _snowPeaBuffers;
	private float[][] _shadowBuffers;

	// 每行当前实际写了几个实例，用来设 visible_instance_count
	private int[] _peaCounts;
	private int[] _snowPeaCounts;
	private int[] _shadowCounts;

	private Rid _peaTextureRid;
	private Rid _snowPeaTextureRid;
	private Rid _shadowAtlasRid;

	private readonly Texture2D _peaTexture;
	private readonly Texture2D _snowPeaTexture;
	private readonly Texture2D _shadowTexture;
	private readonly Vector2 _peaSize;
	private readonly Vector2 _snowPeaSize;
	private readonly Vector2 _shadowFrameSize;

	private bool _initialized;

	/// <summary>活跃子弹数。调优时看它</summary>
	public int Count => _count;

	/// <summary>表现层（开火音、溅射粒子与音效）。池化的，外面只负责触发，不用自己管节点</summary>
	public HitEffectSystem Effects => _effects;

	/// <summary>
	/// 临时隔离开关。关掉之后整个 RenderingServer 这一层都不碰——
	/// 不建画布项、不提交绘制，子弹在画面上不可见，但移动、碰撞、扣血照跑。
	/// 用来判断开销是不是出在渲染这一层。查完就该改回 true。
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
	/// 建每行的画布项与 MultiMesh。得等场景就绪（host 进了树、行数定下来）才能调，
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
		_peaMeshes = new MultiMesh[rowCount];
		_snowPeaMeshes = new MultiMesh[rowCount];
		_shadowMeshes = new MultiMesh[rowCount];
		_peaBuffers = new float[rowCount][];
		_snowPeaBuffers = new float[rowCount][];
		_shadowBuffers = new float[rowCount][];
		_peaCounts = new int[rowCount];
		_snowPeaCounts = new int[rowCount];
		_shadowCounts = new int[rowCount];

		_rowBuckets = new int[rowCount][];
		_rowBucketCounts = new int[rowCount];
		for (int row = 0; row < rowCount; row++)
		{
			// 每个桶都按最坏情况给满：所有子弹挤在同一行是可能的
			_rowBuckets[row] = new int[Capacity];
		}
		_globalBucket = new int[Capacity];

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

			_peaMeshes[row] = CreateMesh(_peaSize);
			_snowPeaMeshes[row] = CreateMesh(_snowPeaSize);
			_shadowMeshes[row] = CreateMesh(_shadowFrameSize);

			_peaBuffers[row] = new float[InstancesPerRow * FloatsPerInstance];
			_snowPeaBuffers[row] = new float[InstancesPerRow * FloatsPerInstance];
			_shadowBuffers[row] = new float[InstancesPerRow * FloatsPerInstance];
		}

		_peaTextureRid = _peaTexture?.GetRid() ?? default;
		_snowPeaTextureRid = _snowPeaTexture?.GetRid() ?? default;
		_shadowAtlasRid = BuildShadowAtlas();

		// 表现层的池子也在这里一次建好，之后每次命中都不再新建节点
		_effects.Initialize(host);

		_initialized = true;
	}

	/// <summary>
	/// 阴影贴图是横排两帧，MultiMesh 一套只能配一整张贴图、没法只取一半，
	/// 所以套一层 AtlasTexture 把左半帧框出来
	/// </summary>
	private Rid BuildShadowAtlas()
	{
		if (_shadowTexture == null)
		{
			return default;
		}

		AtlasTexture atlas = new()
		{
			Atlas = _shadowTexture,
			Region = new Rect2(0f, 0f, _shadowFrameSize.X, _shadowFrameSize.Y),
		};
		return atlas.GetRid();
	}

	private static MultiMesh CreateMesh(Vector2 size)
	{
		QuadMesh quad = new()
		{
			Size = size,
			// QuadMesh 默认以中心为原点，而原来的 Sprite2D 是 centered = false（原点即左上角）。
			// 把中心推到右下半个尺寸，顶点范围就变成 (0,0)~(size,size)，两边才对得上。
			// 它是 3D 的 mesh（2D MultiMesh 也借它用），所以偏移是 Vector3，z 留 0
			CenterOffset = new Vector3(size.X * 0.5f, size.Y * 0.5f, 0f),
		};

		return new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
			Mesh = quad,
			// 实例数建好就固定，每帧只改"显示前几个"
			InstanceCount = InstancesPerRow,
		};
	}

	public void Dispose()
	{
		_effects.Dispose();

		if (!_initialized || _rowCanvasItems == null)
		{
			return;
		}

		foreach (Rid item in _rowCanvasItems)
		{
			RenderingServer.FreeRid(item);
		}
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
		BuildBuckets();

		for (int zi = 0; zi < _activeZombies.Count; zi++)
		{
			Zombie zombie = _activeZombies[zi];
			IHitBox defense = zombie.DefenseHitBox;
			if (!zombie.Alive || defense == null)
			{
				continue;
			}

			Rect2 zombieRect = defense.GlobalRect;

			// 先比同一行的
			int row = zombie.Row;
			if (row >= 0 && row < _rowBuckets.Length)
			{
				HitAgainst(zombie, zombieRect, _rowBuckets[row], _rowBucketCounts[row]);
			}

			// 跨行子弹每只僵尸都要过一遍
			HitAgainst(zombie, zombieRect, _globalBucket, _globalBucketCount);
		}
	}

	/// <summary>每帧把子弹下标填进各行的桶（Row 小于 0 的进全局桶）。一趟 O(子弹数)</summary>
	private void BuildBuckets()
	{
		System.Array.Clear(_rowBucketCounts, 0, _rowBucketCounts.Length);
		_globalBucketCount = 0;

		for (int i = 0; i < _count; i++)
		{
			int row = _bullets[i].Row;
			if (row < 0 || row >= _rowBuckets.Length)
			{
				_globalBucket[_globalBucketCount++] = i;
			}
			else
			{
				_rowBuckets[row][_rowBucketCounts[row]++] = i;
			}
		}
	}

	private void HitAgainst(Zombie zombie, Rect2 zombieRect, int[] bucket, int count)
	{
		for (int k = 0; k < count; k++)
		{
			if (!zombie.Alive)
			{
				return; // 刚被前面的子弹打死，剩下的不该再喂给它
			}

			ref BulletData b = ref _bullets[bucket[k]];
			if (!b.Alive) // 桶是重建过的，但同一帧里前面的僵尸可能已经把它打掉了
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

	private void Hit(ref BulletData b, Zombie zombie)
	{
		zombie.Hurt(new Hurt(b.Damage, b.Type));
		if (b.Kind == BulletKind.SnowPea)
		{
			zombie.AddStatusEffect(new SlowEffect());
		}

		// 表现层：池化的溅射粒子与音效。纯碰撞测试模式下跳过
		if (!MainGame.BCollisionOnlyTest)
		{
			_effects.PlaySplat(new Vector2(b.X, b.Y), b.Kind);
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
	///
	/// 提交量是每行每类一次，也就是十几二十次调用——而不是每条子弹两次。
	/// 后者在 1800 条子弹时是每帧 3600 次 C#→C++ 跨界调用，实测撑不住，
	/// 这正是改用 MultiMesh 的原因。
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
			_peaCounts[row] = 0;
			_snowPeaCounts[row] = 0;
			_shadowCounts[row] = 0;
		}

		// 一趟写实例变换：每条子弹往它那一行、它那一类的 buffer 里追加一个
		for (int i = 0; i < _count; i++)
		{
			ref readonly BulletData b = ref _bullets[i];
			int row = ClampRow(b.Row);

			WriteInstance(_shadowBuffers[row], ref _shadowCounts[row], b.X, b.ShadowY);

			if (b.Kind == BulletKind.SnowPea)
			{
				WriteInstance(_snowPeaBuffers[row], ref _snowPeaCounts[row], b.X, b.Y);
			}
			else
			{
				WriteInstance(_peaBuffers[row], ref _peaCounts[row], b.X, b.Y);
			}
		}

		for (int row = 0; row < _rowCanvasItems.Length; row++)
		{
			Rid item = _rowCanvasItems[row];

			SubmitMesh(item, _shadowMeshes[row], _shadowBuffers[row], _shadowCounts[row], _shadowAtlasRid);
			SubmitMesh(item, _peaMeshes[row], _peaBuffers[row], _peaCounts[row], _peaTextureRid);
			SubmitMesh(item, _snowPeaMeshes[row], _snowPeaBuffers[row], _snowPeaCounts[row], _snowPeaTextureRid);
		}
	}

	private static void SubmitMesh(Rid item, MultiMesh mesh, float[] buffer, int count, Rid texture)
	{
		if (count == 0 || texture == default)
		{
			return;
		}

		Rid meshRid = mesh.GetRid();
		// 整段 buffer 一次传过去，不做逐实例调用——那样等于没省
		RenderingServer.MultimeshSetBuffer(meshRid, buffer);
		RenderingServer.MultimeshSetVisibleInstances(meshRid, count);
		RenderingServer.CanvasItemAddMultimesh(item, meshRid, texture);
	}

	/// <summary>
	/// 往实例 buffer 里追加一个"无旋转、无缩放、原点在 (x, y)"的 2D 变换。
	///
	/// 顺序按官方文档来——**行主序**，X 轴、Y 轴、原点各占一行，每行前面一个填充位：
	///   (x.x, y.x, padding, origin.x, x.y, y.y, padding, origin.y)
	/// 注意 origin 落在 [3] 和 [7]，不是连续的 [4][5]：按列主序写的话
	/// 整个矩阵都是错的，结果是画面上什么都不显示，还不会报错。
	///
	/// 单位变换下 x=(1,0)、y=(0,1)，所以真正在变的只有原点那两位。
	/// </summary>
	private static void WriteInstance(float[] buffer, ref int count, float x, float y)
	{
		if (count >= InstancesPerRow)
		{
			return; // 这一行这一类的实例位用完了，多出来的画不下
		}

		int offset = count * FloatsPerInstance;
		buffer[offset] = 1f;     // x.x
		buffer[offset + 1] = 0f; // y.x
		buffer[offset + 2] = 0f; // 填充
		buffer[offset + 3] = x;  // origin.x
		buffer[offset + 4] = 0f; // x.y
		buffer[offset + 5] = 1f; // y.y
		buffer[offset + 6] = 0f; // 填充
		buffer[offset + 7] = y;  // origin.y

		count++;
	}

	private int ClampRow(int row)
	{
		if (row < 0)
		{
			return 0;
		}
		return row >= _rowCanvasItems.Length ? _rowCanvasItems.Length - 1 : row;
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
}
