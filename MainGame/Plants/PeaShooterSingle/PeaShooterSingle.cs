using Godot;
using static Godot.GD;
using System;
using System.Threading.Tasks;

public partial class PeaShooterSingle : Plants
{
	Vector2 headPos; // 头部位置
	Vector2 const_StemPos = new((float)37.6, (float)48.7); //常数：茎位置
	AnimationPlayer anim_Idle, anim_Head; // Idle动画和Head动画
	float SpeedScaleOfIdle = 1.56f; // Idle动画速度

	public AudioStreamPlayer shootSound = new AudioStreamPlayer(); // 射击音效
	Node2D Stem, Head;// 茎和头节点
	double TimeOfIdleWhenShooting = 0.0f; // 射击时Idle动画的时间

	

	public bool canShoot = false; // 是否可以射击
	Timer canShootTimer = new Timer(); // 射击计时器

	public short ShootCount = 0; //射击次数

	public short ShootMaxInterval = 150; // 射击时间最大间隔
	public short ShootMinInterval = 136; // 射击时间最小间隔

	
	[Export]
	public PackedScene BulletScene { get; set; }

	public PeaShooterSingle()
	{
		SunCost = 100; // 阳光消耗
		//CDtime = CDTime.FAST; // 冷却时间
		CDtime = CDTime.FAST; // 冷却时间
	}

	public override void _Idle()
	{
		anim_Idle.CallDeferred("play", "Idle", -1, SpeedScaleOfIdle);
		anim_Head.CallDeferred("play", "Head_Idle", -1, SpeedScaleOfIdle);
	}

	public override void _Ready()
	{
		base._Ready();
		anim_Idle = GetNode<AnimationPlayer>("./Idle");
		anim_Head = GetNode<AnimationPlayer>("./Head/Head");
		SpeedScaleOfIdle = mainGame.RNG.RandfRange(1.2f, 1.6f);

		Stem = GetNode<Node2D>("./Anim_stem");
		Head = GetNode<Node2D>("./Head");

		shootSound.Stream = (AudioStream)GD.Load("res://sounds/throw.ogg");
		shootSound.VolumeDb = -5;
		AddChild(shootSound);

		canShootTimer.WaitTime = mainGame.RNG.RandiRange(1, ShootMaxInterval) / 100.0f; // 随机射击时间

		canShootTimer.OneShot = true;
		canShootTimer.Timeout += CanShoot;
		AddChild(canShootTimer);
		
		headPos = Head.Position;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Head.Position = headPos + (Stem.Position - const_StemPos); // 头部跟随茎移动

		if (canShoot && mainGame != null && HP > 0) //如果可以射击且主游戏不为空
		{

			foreach (Zombie zombie in mainGame.zombies) // 遍历所有僵尸
			{
				//Zombie zombie = mainGame.zombies[i]; // 取出僵尸
				if (zombie != null) // 如果僵尸不为空
				{
					//GD.Print("zombie.Row: " + zombie.Row + ", PeaShooterSingle.Row: " + Row);
					if (zombie.isDead == false && zombie.Row == Row && // 如果僵尸不死亡且在同一行
						zombie.DefenseArea.GlobalPosition.X > GlobalPosition.X + const_StemPos.X && // 僵尸防守区域在植物的右侧
						zombie.DefenseArea.GlobalPosition.X < mainGame.GameScene.CameraCenterPos.X + 800) // 僵尸防守区域在视野范围内
					{
						Shoot(); // 射击
						break;
					}
					else
					{
						continue;
					}
				}
				else
				{
					break;
				}
			}

		}

		

		else if (canShoot == false)
		{
			//GD.Print("cannot shoot");
		}
		else
		{
			//GD.Print("mainGame is null");
		}
	}

	/// <summary>
	/// 允许射击
	/// </summary>
	public void CanShoot()
	{
		//Print("CanShoot()");
		canShoot = true;
	}

	/// <summary>
	/// 射击
	/// </summary>
	async public void Shoot()
	{
		if (anim_Head.CurrentAnimation == "Head_Idle")
			TimeOfIdleWhenShooting = anim_Head.CurrentAnimationPosition; // 记录Idle动画的时间

		canShoot = false; // 禁止射击
		canShootTimer.WaitTime = mainGame.RNG.RandiRange(ShootMinInterval, ShootMaxInterval) / 100.0f; // 随机射击时间
		canShootTimer.Start(); // 计时器开启

		anim_Head.Play(ShootCount != 0 ? "Head_Shooting2" : "Head_Shooting", 2.0/12.0, 2.85f);  // 头部射击动画
		await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout); // 等待0.35秒
		Bullet bullet = BulletScene.Instantiate<Bullet>(); // 实例化子弹
		//GD.Print(bullet);
		bullet.Position = GetNode<Node2D>("./Head/Idle_mouth").Position + new Vector2(15, -6.5f); // 设置子弹位置为头部的嘴部
		bullet.ShadowPositionY = GetNode<Node2D>("Shadow").GlobalPosition.Y; // 设置子弹阴影位置为阴影的全局位置
		AddChild(bullet); // 添加子弹到场景中
		shootSound.Play(); // 播放射击音效
		ShootCount++; // 射击次数+1
		
		//Anim_Shoot.Play("RESET");
	}

	/// <summary>
	/// 停止射击动画
	/// </summary>
	/// <param name="anim"></param>
	public void StopShooting(StringName anim)
	{
		if (anim == "Head_Shooting" || anim == "Head_Shooting2")
		{
			//Print("Stop shooting");
			
			float temp = 0;
			if (anim == "Head_Shooting")
			{
				temp = 2;
			}
			else if (anim == "Head_Shooting2")
			{
				temp = 11/12.0f;
			}
			
			anim_Head.Play("Head_Idle", 2.0/12.0, 0.0f);
			
			anim_Head.Seek(TimeOfIdleWhenShooting + temp/2.85f * SpeedScaleOfIdle);
			anim_Head.Play("Head_Idle", 2.0/12.0, SpeedScaleOfIdle);
			GetNode<Sprite2D>("./Head/Idle_shoot_blink").Visible = false;
		}
	}

	/// <summary>
	/// 种植植物
	/// </summary>
	/// <param name="row"></param>
	/// <param name="index"></param>
	public override void _Plant(int row, int index)
	{
		base._Plant(row, index);
		canShoot = true;
	}

	public override void FreePlant()
	{
		base.FreePlant();
		RemoveChild(GetNode<Area2D>("./DefenseArea"));
	}
}
