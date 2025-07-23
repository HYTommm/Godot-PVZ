using Godot;
using static Godot.GD;
using System;
using System.Threading.Tasks;

public partial class PeaShooterSingle : Plants
{
	private Vector2 _headPos; // 头部位置
	private readonly Vector2 _constStemPos = new((float)37.6, (float)48.7); //常数：茎位置
	private AnimationPlayer _animIdle, _animHead; // Idle动画和Head动画
	private float _speedScaleOfIdle = 1.56f; // Idle动画速度

	public AudioStreamPlayer ShootSound = new(); // 射击音效
	private Node2D _stem, _head;// 茎和头节点
	private double _timeOfIdleWhenShooting = 0.0f; // 射击时Idle动画的时间


	private bool _canShoot = false; // 是否可以射击
	private readonly Timer _canShootTimer = new(); // 射击计时器

	public short ShootCount = 0; //射击次数

	public short ShootMaxInterval = 150; // 射击时间最大间隔
	public short ShootMinInterval = 136; // 射击时间最小间隔

	
	[Export]
	public PackedScene BulletScene { get; set; }

	public PeaShooterSingle()
	{
		SunCost = 100; // 阳光消耗
		CDtime = 1f; // 冷却时间
		//CDtime = CDTime.FAST; // 冷却时间
	}

	public override void _Idle()
	{
		_animIdle.CallDeferred("play", "Idle", -1, _speedScaleOfIdle);
		_animHead.CallDeferred("play", "Head_Idle", -1, _speedScaleOfIdle);
	}

	public override void _Ready()
	{
		base._Ready();
		_animIdle = GetNode<AnimationPlayer>("./Idle");
		_animHead = GetNode<AnimationPlayer>("./Head/Head");
		_speedScaleOfIdle = mainGame.RNG.RandfRange(1.2f, 1.6f);

		_stem = GetNode<Node2D>("./Anim_stem");
		_head = GetNode<Node2D>("./Head");

		ShootSound.Stream = (AudioStream)GD.Load("res://sounds/throw.ogg");
		AddChild(ShootSound);

		_canShootTimer.WaitTime = mainGame.RNG.RandiRange(1, ShootMaxInterval) / 100.0f; // 随机射击时间

		_canShootTimer.OneShot = true;
		_canShootTimer.Timeout += CanShoot;
		AddChild(_canShootTimer);
		
		_headPos = _head.Position;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		_head.Position = _headPos + (_stem.Position - _constStemPos); // 头部跟随茎移动

		if (_canShoot && mainGame != null && HP > 0) //如果可以射击且主游戏不为空
		{

			foreach (Zombie zombie in mainGame.Zombies) // 遍历所有僵尸
			{
				//Zombie zombie = mainGame.zombies[i]; // 取出僵尸
				if (zombie == null) // 如果僵尸不为空
				{
					break;
				}

				//GD.Print("zombie.Row: " + zombie.Row + ", PeaShooterSingle.Row: " + Row);
				if (zombie.BIsDead == false && zombie.Row == Row && // 如果僵尸不死亡且在同一行
					zombie.DefenseArea.GlobalPosition.X > GlobalPosition.X + _constStemPos.X && // 僵尸防守区域在植物的右侧
					zombie.DefenseArea.GlobalPosition.X <
					mainGame.GameScene.CameraCenterPos.X + 800) // 僵尸防守区域在视野范围内
				{
					Shoot(); // 射击
					break;
				}
			}

		}

		

		else if (_canShoot == false)
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
		_canShoot = true;
	}

	/// <summary>
	/// 射击
	/// </summary>
	public async void Shoot()
	{
		if (_animHead.CurrentAnimation == "Head_Idle")
			_timeOfIdleWhenShooting = _animHead.CurrentAnimationPosition; // 记录Idle动画的时间

		_canShoot = false; // 禁止射击
		RandomShootTime(); // 随机射击时间

		_animHead.Play(ShootCount != 0 ? "Head_Shooting2" : "Head_Shooting", 2.0/12.0, 2.85f);  // 头部射击动画
		await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout); // 等待0.35秒
		Bullet bullet = BulletScene.Instantiate<Bullet>(); // 实例化子弹
		//GD.Print(bullet);
		bullet.Position = GetNode<Node2D>("./Head/Idle_mouth").Position + new Vector2(15, -6.5f); // 设置子弹位置为头部的嘴部
		bullet.ShadowPositionY = GetNode<Node2D>("Shadow").GlobalPosition.Y; // 设置子弹阴影位置为阴影的全局位置
		AddChild(bullet); // 添加子弹到场景中
		ShootSound.Play(); // 播放射击音效
		ShootCount++; // 射击次数+1
		
		//Anim_Shoot.Play("RESET");
	}

	/// <summary> 随机射击时间 </summary>
	public void RandomShootTime()
	{
		_canShootTimer.WaitTime = mainGame.RNG.RandiRange(ShootMinInterval, ShootMaxInterval) / 100.0f; // 随机射击时间
		_canShootTimer.Start(); // 计时器开启
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
			
			_animHead.Play("Head_Idle", 2.0/12.0, 0.0f);
			
			_animHead.Seek(_timeOfIdleWhenShooting + temp/2.85f * _speedScaleOfIdle);
			_animHead.Play("Head_Idle", 2.0/12.0, _speedScaleOfIdle);
			GetNode<Sprite2D>("./Head/Idle_shoot_blink").Visible = false;
		}
	}

	/// <summary>
	/// 种植植物
	/// </summary>
	/// <param name="col"> 列 </param>
	/// <param name="row"> 行 </param>
	/// <param name="index"> 索引 </param>
	public override void _Plant(int col,int row, int index)
	{
		base._Plant(col, row, index);
		RandomShootTime(); // 随机射击时间
	}

	public override void FreePlant()
	{
		base.FreePlant();
		RemoveChild(GetNode<Area2D>("./DefenseArea"));
	}
}
