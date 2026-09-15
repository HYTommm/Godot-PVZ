using Godot;
using System;

public partial class SeedPacketLarger : Node2D
{
	
	MainGame MainGame; // 主游戏节点
	[Export]
	public PackedScene SeedScene; // 种子节点

	public Plants seedShow, seed, seedClone; // 卡片展示的种子节点，跟随鼠标位置的种子节点，位于可种植区的克隆种子节点

	ColorRect CostColorRect; // 花费遮挡阴影
	ColorRect CDColorRect; // CD遮挡阴影
	AnimationPlayer SeedPacketFlash; // 种子卡片闪烁

	ShaderMaterial CDColorRectMaterial; // CD遮挡阴影材质

	public float LeftCDTime; // 剩余CD时间
	public float MaxCDTime; // 最大CD时间

	public bool isCDCooling; // 是否正在冷却CD

	public AudioStreamPlayer SeedLiftSound = new();

	public override void _Ready()
	{

		MainGame = MainGame.Instance; // 获取主游戏节点
		CostColorRect = GetNode<ColorRect>("./CostColorRect"); // 获取花费遮挡阴影节点
		CDColorRect = GetNode<ColorRect>("./CDColorRect"); // 获取CD遮挡阴影节点
		SeedPacketFlash = GetNode<AnimationPlayer>("./SeedPacketFlash/SeedPacketFlash"); // 获取种子卡片闪烁节点

		CDColorRectMaterial = CDColorRect.Material as ShaderMaterial; // 获取CD遮挡阴影材质

		ApplySeedScene(); // 按 SeedScene 建立展示植物与各项数值

		SeedLiftSound.Stream = GD.Load<AudioStream>("res://sounds/seedlift.ogg"); // 加载种子包拾取音效
		AddChild(SeedLiftSound); // 添加种子包拾取音效节点
	}

	/// <summary>
	/// 换成本卡实际要放的植物，并重建它的展示节点与数值。选卡确认后由 SeedBank 调用。
	///
	/// 卡槽节点本身是场景里静态摆好的、输入信号也是场景里连好的，
	/// 所以这里只换 SeedScene，不重建节点——连接因此得以保留。
	/// </summary>
	public void SetSeedScene(PackedScene seedScene)
	{
		if (seedScene == null)
		{
			GD.PrintErr("[SeedPacketLarger] SetSeedScene 收到空场景，本卡保持原样");
			return;
		}

		if (SeedScene == seedScene)
		{
			return; // 已经是这个植物了，不必推倒重建。选卡时会频繁重刷同一批槽位
		}

		SeedScene = seedScene;
		ApplySeedScene();
	}

	/// <summary>把这张卡清成空槽，只留 SeedPacketSilhouette 那层白色卡槽底</summary>
	public void SetEmptySlot()
	{
		SeedScene = null;
		ApplySeedScene();
	}

	/// <summary>
	/// 按当前 SeedScene 重建展示植物、卡面花费、CD 上限与遮挡状态。
	/// SeedScene 为 null 时进入空槽状态。
	/// </summary>
	private void ApplySeedScene()
	{
		if (seedShow != null && IsInstanceValid(seedShow))
		{
			// 先从父节点摘掉再排队释放，否则旧植物会和新植物重叠一帧
			seedShow.GetParent()?.RemoveChild(seedShow);
			seedShow.QueueFree();
			seedShow = null;
		}

		Sprite2D cardFace = GetNode<Sprite2D>("SeedPacketLarger");
		Sprite2D slotBase = GetNode<Sprite2D>("SeedPacketSilhouette");

		if (SeedScene == null)
		{
			// 空槽：场景里那层 SeedPacketSilhouette 就是没有植物时的白色卡槽底，
			// 卡面与各项数值全部收起来
			SetProcess(false); // 没有展示植物，开着 _Process 会拿 seedShow 空引用
			cardFace.Visible = false;
			slotBase.Visible = true;
			GetNode<Label>("./Label").Text = "";
			CostColorRect.Visible = false;
			LeftCDTime = 0.0f;
			MaxCDTime = 0.0f;
			isCDCooling = false;
			CDColorRectMaterial?.SetShaderParameter("max_cd_time", MaxCDTime);
			CDColorRectMaterial?.SetShaderParameter("left_cd_time", LeftCDTime);
			return;
		}

		cardFace.Visible = true;
		slotBase.Visible = false;
		SetProcess(true); // 有植物了，"阳光不足"的判定要跑起来

		seedShow = SeedScene.Instantiate<Plants>(); // 实例化种子节点

		// 必须在入树之前置位：卡槽里的植物只显示外观，各植物的 _Ready 是照种植场景写的，
		// 不去跳过的话，找 %AttackHitBox 这类种植期节点会报 Node not found
		seedShow.BIsDisplayOnly = true;

		seedShow.Position = new Vector2(7, 18); // 种子展示位置

		GetNode<Sprite2D>("SeedPacketLarger").AddChild(seedShow, false, InternalMode.Front); // 将种子展示节点添加到SeedPacketLarger节点下

		// 这一句必须在 AddChild 之后：入树会触发 _Ready，而 Entity._Ready 里调了 SetZIndex()，
		// 它按 Row 把 ZIndex 算成 (Row + 1) * 10 + NormalPlants。卡槽里的植物 Row 是 0，
		// 若先设 0 再入树，这行就被它覆盖掉，植物会盖住卡面的文字与遮罩
		seedShow.ZIndex = 0;

		// 卡槽里的植物只拿来看：碰撞箱照常在物理世界判定，卡槽一旦落进僵尸走道就会误触发
		// （土豆雷的 DamageZombies 首当其冲，它没有 _attackHitBox）。整棵子树在这里关掉判定
		DisableHitBoxes(seedShow);

		LeftCDTime = 0.0f; // 剩余CD时间
		MaxCDTime = seedShow.CDtime; // 最大CD时间
		isCDCooling = false;

		CDColorRectMaterial?.SetShaderParameter("max_cd_time", MaxCDTime); // 设置最大CD时间
		CDColorRectMaterial?.SetShaderParameter("left_cd_time", LeftCDTime);

		CostColorRect.Visible = false; // 换卡后不再处于"阳光不足"状态，交给 _Process 重新判

		if (seedShow.SunCost >= 0) // 种子花费大于0
			GetNode<Label>("./Label").Text = seedShow.SunCost.ToString(); // 显示花费
		else
			GetNode<Label>("./Label").Text = ""; // 隐藏花费
	}

	public override void _Process(double delta)
	{

		if (MainGame != null)
		{
			if (MainGame.SunCount < seedShow.SunCost)
			{
				CostColorRect.Visible = true;
				//SeedPacketFlash.Play("SeedPacketFlash");
			}
		}

		if (LeftCDTime > 0.0f)
		{
			if (isCDCooling)
				LeftCDTime -= (float)delta;
			if (LeftCDTime < 0.0f)
			{
				LeftCDTime = 0.0f;
			}
		}
		else
		{
			//GD.Print(CostColorRect.Visible);
			if (MainGame.SunCount >= seedShow.SunCost && CostColorRect.Visible == true)
			{
				CostColorRect.Visible = false;
				SeedPacketFlash.Play("SeedPacketFlash");
			}
		}
		CDColorRectMaterial.SetShaderParameter("left_cd_time", LeftCDTime);
		//GD.Print("ShaderParameter LeftCDTime: " + CDColorRectMaterial.GetShaderParameter("left_cd_time"));
		//GD.Print("LeftCDTime: " + LeftCDTime);
	}

	public override void _PhysicsProcess(double delta)
	{
		//seed.Position = GetViewport().GetMousePosition() - new Vector2(35, 60);
	}


	private void OnInputEvent(Node viewport, InputEvent @event, int shape_idx)
	{

		if (@event.IsAction("mouse_left"))
		{
			// 只有挂在种子栏上的卡才响应种植。选卡面板复用了同一个卡片场景，
			// 面板里的卡由面板自己处理点击，这里必须挡掉
			if (GetParent() is not SeedBank bank)
			{
				return;
			}

			// 选卡阶段禁止点卡。卡片是 Area2D 判定，不受面板那层 Control 遮挡影响，
			// 所以不能只靠"种子栏还没升起"来挡，得显式闸住
			if (bank.BIsForbiddenSelect)
			{
				return;
			}

			GD.Print("接收到输入事件");

			if (MainGame.BMouse_left_down && MainGame.BIsSeedCardSelected == false)
			{
				// 如果阳光大于植物 costs 并且 CD 冷却完毕
				if (MainGame.SunCount >= seedShow.SunCost && LeftCDTime <= 0.0f)
				{
					seed = SeedScene.Instantiate<Plants>();
					seedClone = SeedScene.Instantiate<Plants>();
					//seed.Scale = new Vector2(2.0f, 2.0f);

					MainGame.AddSeed(this, seed, seedClone);
					SeedLiftSound.Play();
					ResetCD();
				}
				else if (MainGame.SunCount < seedShow.SunCost)
				{
					GetParent<SeedBank>().SunCountFlashWarning();
				}
				else if (LeftCDTime > 0.0f)
				{
					GetParent<SeedBank>().FlashWarningSound.Play();
				}
			}
		}
		
	}

	/// <summary>
	/// 重置CD
	/// </summary>
	public void ResetCD()
	{
		isCDCooling = false;
		LeftCDTime = MaxCDTime;
		CostColorRect.Visible = true;
		CDColorRectMaterial.SetShaderParameter("left_cd_time", LeftCDTime);
	}
	/// <summary>
	/// 将CD置为0
	/// </summary>
	public void SetCDZero()
	{
		LeftCDTime = 0.0f;
		CostColorRect.Visible = false;
		CDColorRectMaterial.SetShaderParameter("left_cd_time", LeftCDTime);
	}
	

	public void OnMouseEnter()
	{
		//GD.Print("SeedPacketLarger: OnMouseEnter");
	}

	/// <summary>
	/// 递归关掉一棵子树里所有碰撞箱的物理判定。卡槽里展示用的植物要走这一遭：
	/// 关闭的是判定，不是显示，卡面和动画照常。认 IHitBox 接口而不是 Area2D，
	/// 免得将来换了碰撞箱实现就漏掉。
	/// </summary>
	private static void DisableHitBoxes(Node node)
	{
		if (node is IHitBox hitBox)
		{
			hitBox.Monitoring = false;
			hitBox.Monitorable = false;
		}

		foreach (Node child in node.GetChildren())
		{
			DisableHitBoxes(child);
		}
	}
}
