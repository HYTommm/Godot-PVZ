using Godot;
using static Godot.GD;
using System;
using System.Threading.Tasks;
//using PlantsVsZombies;

public partial class Zombie : HealthEntity
{
	/// <summary>僵尸死亡事件</summary>
	[Signal]
	public delegate void ZombieDieEventHandler();
	/// <summary>是否正在移动</summary>
	[Export]
	public bool isMoving = true;
	/// <summary>是否濒死</summary>
	public bool isDying = false;
	/// <summary>是否死亡</summary>
	public bool isDead = false;
	/// <summary>位置记录</summary>
	//[Export] public Vector2 Pos;
	/// <summary>地面位置</summary>
	public Vector2 ConstGroundPos = new((float)-9.8, 40);
	public Vector2 LastGroundPos;
	/// <summary>动画播放器</summary>
	public AnimationPlayer animation;
	/// <summary>地面节点</summary>
	public Sprite2D Ground;

	/// <summary>所处波数</summary>
	public int Wave;
	/// <summary>所处行</summary>
	public int Row = -1;

	/// <summary>权重</summary>
	//public int Weight = 400;
	/// <summary>等级</summary>
	//public int Grade = 1;
	/// <summary>攻击力/秒</summary>
	public int Attack = 100;
	/// <summary>暂存攻击</summary>
	public double AttackTemp = 0;

	/// <summary>行走速度，默认1.0f，在(640/99)/735到(640/99)/459之间</summary>
	public float WalkSpeed = 1.0f;

	/// <summary>主游戏节点</summary>
	public MainGame MainGame;
	/// <summary>防御区域节点</summary>
	public Area2D DefenseArea;
	/// <summary>攻击区域节点</summary>
	public Area2D AttackArea;
	
	/// <summary>啃食音效</summary>
	public AudioStreamPlayer EatSound = new AudioStreamPlayer();
	/// <summary>是否正在播放啃食音效</summary>
	[Export]
	public bool IsPlayingEatSound = false;

	protected ArmorSystem ArmorSystem { get; } = new ArmorSystem();

	protected Sprite2D Anim_innerarm1; // 内臂上部
	protected Sprite2D Anim_innerarm2; // 内臂下部
	protected Sprite2D Anim_innerarm3; // 内臂手
	protected Sprite2D Zombie_outerarm_upper; // 外臂上部
	protected Sprite2D Zombie_outerarm_lower; // 外臂下部
	protected Sprite2D Zombie_outerarm_hand;  // 外臂手
	protected Sprite2D Zombie_head;           // 头
	protected Sprite2D Zombie_jaw;            // 下巴
	protected Sprite2D Zombie_hair;           // 头发
	protected Sprite2D Zombie_tongue;         // 舌头

	protected GpuParticles2D ZombieArmParticles; // 外臂粒子动画
	protected GpuParticles2D ZombieHeadParticles; // 头部粒子动画

	public Zombie()
	{
		HP = 270;
		MaxHP = 270;
		Index = -1;
		GD.Print("Base Zombie Constructor called");
	}

	public virtual void Init()
	{
		GD.Print("Zombie Constructor called");
	}


	public void Init(ZombieTypeEnum zombieTypeEnum)
	{
		switch (zombieTypeEnum)
		{
			case ZombieTypeEnum.Normal:
				((NormalZombie)this).Init();
				break;
			case ZombieTypeEnum.Conehead:
				((ConeheadZombie)this).Init();
				break;
			case ZombieTypeEnum.Buckethead:
				((BucketheadZombie)this).Init();
				break;
			case ZombieTypeEnum.Screendoor:
				((ScreendoorZombie)this).Init();
				break;
		}
	}

	public override void _Ready()
	{
		
		// 获取动画播放器节点
		animation = GetNode<AnimationPlayer>("./Zombie/AnimationPlayer");
		
		// 获取地面节点
		Ground = GetNode<Sprite2D>("./Zombie/_ground");
		LastGroundPos = Ground.Position;

		Anim_innerarm1 = GetNode<Sprite2D>("./Zombie/Anim_innerarm1");
		Anim_innerarm2 = GetNode<Sprite2D>("./Zombie/Anim_innerarm2");
		Anim_innerarm3 = GetNode<Sprite2D>("./Zombie/Anim_innerarm3");
		Zombie_outerarm_upper = GetNode<Sprite2D>("./Zombie/Zombie_outerarm_upper");
		Zombie_outerarm_lower = GetNode<Sprite2D>("./Zombie/Zombie_outerarm_lower");
		Zombie_outerarm_hand = GetNode<Sprite2D>("./Zombie/Zombie_outerarm_hand");
		Zombie_head = GetNode<Sprite2D>("./Zombie/Anim_head1");
		Zombie_jaw = GetNode<Sprite2D>("./Zombie/Anim_head2");
		Zombie_hair = GetNode<Sprite2D>("./Zombie/Anim_hair");
		Zombie_tongue = GetNode<Sprite2D>("./Zombie/Anim_tongue");
		ZombieArmParticles = GetNode<GpuParticles2D>("./Zombie/Particle_ZombieArm");
		ZombieHeadParticles = GetNode<GpuParticles2D>("./Zombie/Particle_ZombieHead");
		
		// 设置僵尸外臂上部的纹理
		Zombie_outerarm_upper.Texture = GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper.png");
		
		// 设置僵尸外臂下部可见
		Zombie_outerarm_lower.Visible = true;
		
		// 设置僵尸外臂手部可见
		Zombie_outerarm_hand.Visible = true;
	
		// 获取主游戏节点
		MainGame = this.GetMainGame();
	
		// 获取防御区域节点
		DefenseArea = GetNode<Area2D>("./Zombie/DefenseArea");
		
		// 获取攻击区域节点
		AttackArea = GetNode<Area2D>("./Zombie/AttackArea");

		// 设置啃食音效
		EatSound.VolumeDb -= 5;
		EatSound.Finished += () => IsPlayingEatSound = false;
	
		// 将啃食的声音节点添加为子节点
		AddChild(EatSound);
		// 随机设置僵尸的行走速度，并打印出速度
		WalkSpeed = MainGame.RNG.RandfRange(640.0f / 99 / 735 * 100, 640.0f / 99 / 459 * 100);
		Print("Zombie speed: " + WalkSpeed.ToString());
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		
		if (isMoving)
		{
			Vector2 temp = LastGroundPos - Ground.Position;
			if (temp < Vector2.Zero)
				Position += temp;
			LastGroundPos = Ground.Position;
		}
		if (isDying && HP > 0)
		{
			HP -= 1;
		}
		if (HP <= 0 && !isDead)
		{
			isDead = true;
			Die();
		}
		//GetNode<TextEdit>("./TextEdit").Text = "波数：" + Wave.ToString() + "，血量：" + HP.ToString() + "栈：" + Index.ToString();
		//GD.Print(Position);
		// 处理攻击区域
		// 如果植物在攻击区域内，则攻击
		var overlappingAreas = AttackArea.GetOverlappingAreas();
		//Print(overlappingAreas.Count);
		if (AttackArea != null && overlappingAreas.Count > 0 && !isDying)
		{
			int max_stack = -1;
			Area2D AttackPlantArea = null;
			Plants AttackPlant = null;
			foreach (Area2D area in overlappingAreas)
			{
				// 在这里处理每个重叠的区域
				if (area != null
					&& area.GetParent<Plants>() is Plants plant
					&& plant != null
					&& plant.Row == Row
					&& plant.isPlanted
					&& plant.HP > 0)
				{
					// 处理植物
					if (plant.Index >= max_stack)
					{
						max_stack = plant.Index;
						//current_max_stack_array_index = overlappingAreas.IndexOf(area);
						AttackPlantArea = area;
					}
				}
			}

			//Print("Plant HP: ")
			if (AttackPlantArea != null &&
				(AttackPlant = AttackPlantArea.GetParent<Plants>()) != null &&

				AttackPlant.HP > 0 &&
				AttackPlant.isPlanted)
			{
				Print("Eat Plant");
				AttackTemp += Attack * delta; // 攻击暂存
				int AttackInt = (int)AttackTemp; // 攻击整数
				AttackTemp -= AttackInt; // 攻击余数
				Eat();
				AttackPlant.Hurt(AttackInt);
			}
			else
			{

				AttackTemp = 0;
				
				if (animation.CurrentAnimation == "Zombie_eat")
				{
					Print(animation.CurrentAnimation);
					ContinueMove(customBlend: 1 / 6.0f);
				}
			}
		}
		else
		{
			//Print("Attack :" + AttackArea + "isDying: " + isDying.ToString());
			AttackTemp = 0;
			if (animation.CurrentAnimation == "Zombie_eat")
			{
				Print(animation.CurrentAnimation);
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
		if (area.GetNode("..") is Plants plant && plant != null && plant.Row == Row && plant.isPlanted && !isDying)
		{
			Eat();
		}
	}
	
	public void ContinueMove(float customBlend = 0) => Move(customBlend);

	public void Move(float customBlend = 0)
	{
		Print("Current Animation: " + animation.CurrentAnimation);

		// 如果当前动画不是行走动画，则播放行走动画
		if (animation.CurrentAnimation != "Zombie_walk")
		{
			// 播放速度为WalkSpeed
			animation.Play("Zombie_walk", customBlend, WalkSpeed);

			// 继续移动
			isMoving = true;
		}
	}

	/// <summary>
	/// 啃食植物
	/// </summary>
	public async void Eat()
	{
		
		if (!IsPlayingEatSound)
			PlayEatSound(); // 播放啃食音效
		if (animation.CurrentAnimation != "Zombie_eat" && animation.CurrentAnimation != "Zombie_death")
		{
			// 停止移动
			isMoving = false;
			// 等待下一帧的处理
			await ToSignal(GetTree(), "process_frame");
			// 记录当前位置
			//Pos = Position;
			//播放吃植物动画
			animation.Play("Zombie_eat", 1.0 / 6.0, 3.0f);

			// 等待下一帧的处理
			await ToSignal(GetTree(), "process_frame");

			Ground.Position = ConstGroundPos;
		}
	}

	public void PlayEatSound()
	{
		IsPlayingEatSound = true;
		uint random = GD.Randi() % 3; // 随机播放啃食音效
		switch (random)
		{
			case 0:
				EatSound.Stream = (AudioStream)GD.Load("res://sounds/chomp.ogg");
				break;
			case 1:
				EatSound.Stream = (AudioStream)GD.Load("res://sounds/chomp2.ogg");
				break;
			case 2:
				EatSound.Stream = (AudioStream)GD.Load("res://sounds/chompsoft.ogg");
				break;
		}
		

		EatSound.Play();
	}

	/// <summary>
	/// 受伤
	/// </summary>
	/// <param name="damage"></param>
	public override int Hurt(int damage)
	{
		int returnDamage = damage;
		if (HP <= damage)
		{
			returnDamage = HP > 0 ? HP : 0;
		}
		// 减血
		HP -= damage;
		// 如果血量 <= 0 则死亡
		if (HP <= 180 && HP + damage > 180)
		{
			DropArm(); // 断臂
		}
		if (HP < 90 && HP + damage >= 90)
		{
			Dying(); // 开始濒死
		}
		//if (HP <= 0)
		//{
		//	Die();
		//}
		MainGame.UpdateZombieHP();
		return returnDamage;
	}


	public async void Hurt(Hurt hurt)
	{
		ArmorSystem.ProcessDamage(hurt);
		GD.Print("Damage: " + hurt.Damage);
		if (hurt.HurtType == HurtType.LawnMower)
		{
			GD.Print("LawnMower");
			isDead = true;
			isMoving = false;
			animation.Stop();
			animation.Play("LawnMoweredZombie", 1.0 / 6.0, 1.0f);
			await ToSignal(animation, "animation_finished");
			foreach (var child in GetNode<Node2D>("./Zombie").GetChildren())
			{
				if (child is Sprite2D node2D)
				{
					Print("child.Name: ", node2D.Name);
					node2D.Visible = false;
				}
			}
			GD.Print("LawnMoweredZombie finished");
		}
		hurt.HurtHealthEntity(this);
	}

	/// <summary>
	/// 断臂
	/// </summary>
	public void DropArm()
	{
		//Zombie_outerarm_upper
		Zombie_outerarm_upper.Texture = GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper2.png");
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
		isDying = true;
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

	async public void Die()
	{
		// 删除Area2D节点
		GetNode<Node2D>("./Zombie").RemoveChild(GetNode<Area2D>("./Zombie/DefenseArea"));
		// 播放死亡动画
		isMoving = false;
		animation.Play("Zombie_death", 1.0 / 6.0);
		
		//销毁节点
		await ToSignal(animation, "animation_finished");
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
		// 设置光照遮挡层
		//GetNode<LightOccluder2D>("./GroundConstraint").OccluderLightMask = 2 << (Row - 1);
		//GetNode<GpuParticles2D>("./Particle_ZombieArm").LightMask = 2 << (Row - 1);
		//GetNode<GpuParticles2D>("./Particle_ZombieHead").LightMask = 2 << (Row - 1);
		// 设置调试信息——波数
		GetNode<TextEdit>("./TextEdit").Text = Wave.ToString();
		Position = new Vector2(1050, scene.LawnLeftTopPos.Y + Row * scene.LawnUnitWidth - 35);

	}

	
}
