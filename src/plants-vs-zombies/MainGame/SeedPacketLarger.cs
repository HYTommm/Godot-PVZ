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
		
		seedShow = SeedScene.Instantiate<Plants>(); // 实例化种子节点
		
		seedShow.Position = new Vector2(7, 18); // 种子展示位置

		LeftCDTime = 0.0f; // 剩余CD时间
		MaxCDTime = seedShow.CDtime; // 最大CD时间 

		
		CDColorRectMaterial = CDColorRect.Material as ShaderMaterial; // 获取CD遮挡阴影材质

		CDColorRectMaterial?.SetShaderParameter("max_cd_time", MaxCDTime); // 设置最大CD时间
		//GD.Print("ShaderParameter MaxCDTime: " + CDColorRectMaterial.GetShaderParameter("max_cd_time"));
		//GD.Print("MaxCDTime: " + MaxCDTime);

		GetNode<Sprite2D>("SeedPacketLarger").AddChild(seedShow, false, InternalMode.Front); // 将种子展示节点添加到SeedPacketLarger节点下
		seedShow.ZIndex = 0;

		if (seedShow.SunCost >= 0) // 种子花费大于0
			GetNode<Label>("./Label").Text = seedShow.SunCost.ToString(); // 显示花费
		else
			GetNode<Label>("./Label").Text = ""; // 隐藏花费

		SeedLiftSound.Stream = GD.Load<AudioStream>("res://sounds/seedlift.ogg"); // 加载种子包拾取音效
		AddChild(SeedLiftSound); // 添加种子包拾取音效节点
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
			GD.Print("接收到输入事件");
			//GD.Print("SeedPacketLarger: OnInputEvent");
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
}
