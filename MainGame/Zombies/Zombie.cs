using System;
using Godot;
using static Godot.GD;
//using PlantsVsZombies;

public partial class Zombie : HealthEntity
{
	/// <summary>僵尸死亡事件</summary>
	[Signal]
	public delegate void ZombieDieEventHandler();
	/// <summary>是否正在移动</summary>
	[Export]
	public bool BIsMoving = true;
	/// <summary>是否濒死</summary>
	public bool BIsDying = false;
	/// <summary>是否死亡</summary>
	public bool BIsDead = false;
	/// <summary>地面位置</summary>
	public Vector2 ConstGroundPos = new((float)-9.8, 40);
	public Vector2 LastGroundPos;
	/// <summary>动画播放器</summary>
	[Export] private AnimationPlayer _animation;
	[Export] private AnimationPlayer _animCharred;
	/// <summary>地面节点</summary>
	[Export] public Sprite2D Ground;

	/// <summary>所处波数</summary>
	public int Wave;
	/// <summary>所处行</summary>
	public int Row = -1;

	/// <summary>攻击力/秒</summary>
	public int Attack = 100;
	/// <summary>暂存攻击</summary>
	public double AttackTemp = 0;

	/// <summary>行走速度，默认1.0f，在(640/99)/735到(640/99)/459之间</summary>
	public float WalkSpeed = 1.0f;

	/// <summary>主游戏节点</summary>
	public MainGame MainGame;
	/// <summary>防御区域节点</summary>
	[Export] public Area2D DefenseArea;
	/// <summary>攻击区域节点</summary>
	[Export] public Area2D AttackArea;
	
	/// <summary>啃食音效</summary>
	public AudioStreamPlayer EatSound = new();
	/// <summary>是否正在播放啃食音效</summary>
	public bool IsPlayingEatSound = false;

	protected ArmorSystem ArmorSystem { get; } = new();


	public int CriticalHP1;
	public int CriticalHPLast;


	[Export] protected Sprite2D Anim_innerArm1; // 内臂上部
	[Export] protected Sprite2D Anim_innerArm2; // 内臂下部
	[Export] protected Sprite2D Anim_innerArm3; // 内臂手
	[Export] protected Sprite2D Zombie_outerarm_upper; // 外臂上部
	[Export] protected Sprite2D Zombie_outerarm_lower; // 外臂下部
	[Export] protected Sprite2D Zombie_outerarm_hand;  // 外臂手
	[Export] protected Sprite2D Zombie_head;           // 头
	[Export] protected Sprite2D Zombie_jaw;            // 下巴
	[Export] protected Sprite2D Zombie_hair;           // 头发
	[Export] protected Sprite2D Zombie_tongue;         // 舌头

	[Export] protected GpuParticles2D ZombieArmParticles; // 外臂粒子动画
	[Export] protected GpuParticles2D ZombieHeadParticles; // 头部粒子动画

	public Zombie()
	{
		HP = 270;
		MaxHP = 270;
		Index = -1;
		CriticalHP1 = 180;
		CriticalHPLast = 90;
		GD.Print("Base Zombie Constructor called");
	}

	public virtual void Init()
	{
		GD.Print("Zombie Constructor called");
	}


	//public void Init(ZombieTypeEnum zombieTypeEnum)
	//{
	//	switch (zombieTypeEnum)
	//	{
	//		case ZombieTypeEnum.Normal:
	//			((NormalZombie)this).Init();
	//			break;
	//		case ZombieTypeEnum.Conehead:
	//			((ConeheadZombie)this).Init();
	//			break;
	//		case ZombieTypeEnum.Buckethead:
	//			((BucketheadZombie)this).Init();
	//			break;
	//		case ZombieTypeEnum.Screendoor:
	//			((ScreendoorZombie)this).Init();
	//			break;
	//	}
	//}

	public override void _Ready()
	{
		base._Ready();
		LastGroundPos = Ground.Position;
		// 设置僵尸外臂上部的纹理
		Zombie_outerarm_upper.Texture = ResourceManager.Images.Zombies.ImageZombie_OuterarmUpper;
		
		// 设置僵尸外臂下部可见
		Zombie_outerarm_lower.Visible = true;
		
		// 设置僵尸外臂手部可见
		Zombie_outerarm_hand.Visible = true;
	
		// 获取主游戏节点
		MainGame = MainGame.Instance;
	
		// 获取防御区域节点
		//DefenseArea = GetNode<Area2D>("./Zombie/DefenseArea");
		
		// 获取攻击区域节点
		//AttackArea = GetNode<Area2D>("./Zombie/AttackArea");

		// 设置啃食音效
		EatSound.Finished += () => IsPlayingEatSound = false;
	
		// 将啃食的声音节点添加为子节点
		AddChild(EatSound);
		// 随机设置僵尸的行走速度，并打印出速度
		WalkSpeed = MainGame.RNG.RandfRange(640.0f / 99 / 735 * 100, 640.0f / 99 / 459 * 100);
		_animation.Play("Zombie_walk", customBlend: 1 / 6.0f, customSpeed: WalkSpeed);
		Print("Zombie speed: " + WalkSpeed);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		
		if (BIsMoving)
		{
			Vector2 temp = LastGroundPos - Ground.Position;
			if (temp < Vector2.Zero)
				Position += temp;
			LastGroundPos = Ground.Position;
		}
		if (BIsDying && HP > 0)
		{
			HP -= 1;
		}
		if (HP <= 0 && !BIsDead)
		{
			BIsDead = true;
			Die();
		}
		//GetNode<TextEdit>("./TextEdit").Text = "波数：" + Wave.ToString() + "，血量：" + HP.ToString() + "栈：" + Index.ToString();
		//GD.Print(Position);
		// 处理攻击区域
		// 如果植物在攻击区域内，则攻击
		var overlappingAreas = AttackArea.GetOverlappingAreas();
		//Print(overlappingAreas.Count);
		if (AttackArea != null && overlappingAreas.Count > 0 && !BIsDying && !BIsDead)
		{
			int maxStack = -1;
			Area2D attackPlantArea = null;
			Plants attackPlant = null;
			foreach (Area2D area in overlappingAreas)
			{
				// 在这里处理每个重叠的区域
				if (area?.GetParent<Plants>() is { } plant
					&& plant.Row == Row
					&& plant.BIsPlanted
					&& plant.HP > 0)
				{
					// 处理植物
					if (plant.Index >= maxStack)
					{
						maxStack = plant.Index;
						//current_max_stack_array_index = overlappingAreas.IndexOf(area);
						attackPlantArea = area;
					}
				}
			}

			//Print("Plant HP: ")
			if (attackPlantArea != null &&
				(attackPlant = attackPlantArea.GetParent<Plants>()) != null &&

				attackPlant.HP > 0 &&
				attackPlant.BIsPlanted)
			{
				//Print("Eat Plant");
				AttackTemp += Attack * delta; // 攻击暂存
				int attackInt = (int)AttackTemp; // 攻击整数
				AttackTemp -= attackInt; // 攻击余数
				Eat();
				attackPlant.Hurt(new Hurt(damage: attackInt, hurtType: HurtType.Eating));
			}
			else
			{

				AttackTemp = 0;
				
				if (_animation.CurrentAnimation == "Zombie_eat")
				{
					Print(_animation.CurrentAnimation);
					ContinueMove(customBlend: 1 / 6.0f);
				}
			}
		}
		else
		{
			//Print("Attack :" + AttackArea + "BIsDying: " + BIsDying.ToString());
			AttackTemp = 0;
			if (_animation.CurrentAnimation == "Zombie_eat")
			{
				Print(_animation.CurrentAnimation);
				ContinueMove(customBlend: 1 / 6.0f);
			}
		}
	}

	public override void _Process(double delta)
	{
		
	}

	public void OnAreaEntered(Area2D area)
	{
		// 如果检测到植物进入攻击区域，则播放攻击动画
		if (area.GetNode("..") is Plants plant && plant.Row == Row && plant.BIsPlanted && !BIsDying && !BIsDead)
		{
			Eat();
		}
	}
	
	public void ContinueMove(float customBlend = 0) => Move(customBlend);

	public void Move(float customBlend = 0)
	{
		Print("Current Animation: " + _animation.CurrentAnimation);

		// 如果当前动画不是行走动画，则播放行走动画
		if (_animation.CurrentAnimation != "Zombie_walk")
		{
			// 播放速度为WalkSpeed
			_animation.Play("Zombie_walk", customBlend: customBlend, customSpeed: WalkSpeed);

			// 继续移动
			BIsMoving = true;
		}
	}

	/// <summary>
	/// 啃食植物
	/// </summary>
	public async void Eat()
	{
		
		if (!IsPlayingEatSound)
		{
			PlayEatSound(); // 播放啃食音效
		}

		if (_animation.CurrentAnimation != "Zombie_eat" && _animation.CurrentAnimation != "Zombie_death")
		{
			// 停止移动
			BIsMoving = false;
			// 等待下一帧的处理
			await ToSignal(GetTree(), "process_frame");
			// 记录当前位置
			//Pos = Position;
			//播放吃植物动画
			_animation.Play("Zombie_eat", 1.0 / 6.0, 3.0f);

			// 等待下一帧的处理
			await ToSignal(GetTree(), "process_frame");

			Ground.Position = ConstGroundPos;
		}
	}

	public virtual void PlayEatSound()
	{
		IsPlayingEatSound = true;
		uint random = GD.Randi() % 3; // 随机播放啃食音效
		EatSound.Stream = random switch
		{
			0 => ResourceManager.Sounds.Sound_Chomp,
			1 => ResourceManager.Sounds.Sound_Chomp2,
			2 => ResourceManager.Sounds.Sound_ChompSoft,
			_ => EatSound.Stream
		};

		EatSound.Play();
	}

	/// <summary>
	/// 受伤
	/// </summary>
	/// <param name="hurt"></param>
	public override async void Hurt(Hurt hurt)
	{
		if (BIsDead)
		{
			return;
		}
		ArmorSystem.ProcessDamage(hurt);
		Print("Damage: " + hurt.Damage);
		if (hurt.HurtType == HurtType.LawnMower)
		{
			Print("LawnMower");
			BIsDead = true;
			BIsMoving = false;
			_animation.Stop();
			_animation.Play("LawnMoweredZombie", 1.0 / 6.0);
			await ToSignal(_animation, "animation_finished");
			foreach (Node child in GetNode<Node2D>("./Zombie").GetChildren())
			{
				if (child is Sprite2D node2D)
				{
					Print("child.Name: ", node2D.Name);
					node2D.Visible = false;
				}
			}
			Print("LawnMoweredZombie finished");
		}
		

		int damage = Math.Min(hurt.Damage, HP);
		HP -= damage;
		hurt.Damage -= damage;
		if (HP <= CriticalHP1 && HP + damage > CriticalHP1)
		{
			DropArm(); // 断臂
		}
		if (HP < CriticalHPLast && HP + damage >= CriticalHPLast)
		{
			Dying(); // 开始濒死
		}
		if (hurt.HurtType == HurtType.Explosion && HP <= 0)
		{
			BIsDead = true;
			BIsMoving = false;
			GetNode<Node2D>("./Zombie").RemoveChild(DefenseArea);
			GetNode<Node2D>("./Zombie").Visible = false;
			GetNode<Node2D>("./Node2D").Visible = true;
			_animCharred.Play("ALL_ANIMS", 1.0 / 6.0);
			await ToSignal(_animCharred, AnimationMixer.SignalName.AnimationFinished);
			GetNode<Node2D>("./Node2D").Visible = false;
		}
		MainGame.UpdateZombieHP();

		
	}

	/// <summary>
	/// 断臂
	/// </summary>
	public void DropArm()
	{
		//Zombie_outerarm_upper
		Zombie_outerarm_upper.Texture = ResourceManager.Images.Zombies.ImageZombie_OuterarmUpper2;
		//Zombie_outerarm_lower
		Zombie_outerarm_lower.Visible = false;
		//Zombie_outerarm_hand
		Zombie_outerarm_hand.Visible = false;
		// 播放断臂粒子
		ZombieArmParticles.SetDeferred("emitting", true);
	}

	/// <summary>
	/// 死亡
	/// </summary>
	public void Dying()
	{
		// 设置为濒死状态
		BIsDying = true;
		// 发出死亡信号
		EmitSignal("ZombieDie");
		DropHead(); // 掉头
		//GetTree().CallGroup("zombies", "RemoveZombie", this);
	}
	
	/// <summary>
	/// 掉头
	/// </summary>
	public void DropHead()
	{
		Zombie_head.Visible = false; // 隐藏头部
		Zombie_jaw.Visible = false; // 隐藏下巴
		Zombie_hair.Visible = false; // 隐藏头发
		Zombie_tongue.Visible = false; //隐藏舌头
		// 播放掉头粒子
		ZombieHeadParticles.Emitting = true;
	}

	public async void Die()
	{
		// 删除Area2D节点
		GetNode<Node2D>("./Zombie").RemoveChild(DefenseArea);
		// 播放死亡动画
		BIsMoving = false;
		_animation.Play("Zombie_death", 1.0 / 6.0);
		
		//销毁节点
		await ToSignal(_animation, AnimationMixer.SignalName.AnimationFinished);
		// 销毁节点
		FreeZombie();
	}
	/// <summary>
	/// 释放僵尸节点
	/// </summary>
	public void FreeZombie()
	{
		if (Index >= 0)
			MainGame.RemoveZombie(this);
		Visible = false;
		//QueueFree();
	}

	/// <summary>
	/// 刷新僵尸
	/// </summary>
	/// <param name="index"></param>
	/// <param name="scene"></param>
	/// <param name="wave"></param>
	/// <param name="row"></param>
	public void Refresh(int index, Scene scene, int wave, int row)
	{
		HP = MaxHP;
		//Index = index;
		Wave = wave;
		// 随机行
		
		Row = row;
		// 设置调试信息
		GetNode<TextEdit>("./TextEdit").Text = Index.ToString();
		Position = new Vector2(1050, scene.LawnLeftTopPos.Y + Row * scene.LawnUnitWidth - 35);

	}

	public override void SetZIndex()
	{
		GetParent().MoveChild(this, Index);
		ZIndex = (Row + 1) * 10 + (int)ZIndexEnum.Zombies;
	}
}
