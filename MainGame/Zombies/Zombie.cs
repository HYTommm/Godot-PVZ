using Godot;
using static Godot.GD;
using System;

public partial class Zombie : Node2D
{
	[Signal]
	public delegate void ZombieDieEventHandler();
	[Export]
	public bool isMoving = true;                         //是否正在移动
	public bool isDying = false;                         //是否濒死
	public bool isDead = false;                          //是否死亡
	public Vector2 Pos;                                  //位置记录
	public Vector2 ConstGroundPos = new((float)-9.8, 40);//地面位置
	public AnimationPlayer animation;                    //动画播放器
	public Sprite2D Ground;                              //地面节点

	public int HP = 270;                                 // 生命值
	public int MaxHP = 270;                              // 最大生命值
	
	public int Index = -1;                               // 索引
	public int Wave;                                     // 所处波数
	public int Row = -1;                                 // 所处行

	public int Weight = 400;                             // 权重
	public int Grade = 1;                                // 等级

	public float WalkSpeed = 1.0f;                       // 行走速度，默认1.0f，在(640/99)/735到(640/99)/459之间

	public MainGame MainGame;                            // 主游戏节点

	

	public override void _Ready()
	{
		Pos = Position;
		animation = GetNode<AnimationPlayer>("./AnimationPlayer");
		Ground = GetNode<Sprite2D>("./_ground");
		//Zombie_outerarm_upper
		GetNode<Sprite2D>("./Zombie_outerarm_upper").Texture = GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper.png");
		//Zombie_outerarm_lower
		GetNode<Sprite2D>("./Zombie_outerarm_lower").Visible = true;
		//Zombie_outerarm_hand
		GetNode<Sprite2D>("./Zombie_outerarm_hand").Visible = true;
		MainGame = GetParent<MainGame>();

		WalkSpeed = MainGame.RNG.RandfRange(640.0f/99/735 * 100, 640.0f/99/459 * 100);
		Print("Zombie speed: " + WalkSpeed.ToString());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		
		if (isMoving)
		{
			Position = Pos + (ConstGroundPos - Ground.Position);
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
		GetNode<TextEdit>("./TextEdit").Text = "波数：" + Wave.ToString() + "，血量：" + HP.ToString() + "栈：" + Index.ToString();
		//GD.Print(Position);

	}
	public void PosUpdate(StringName animName)
	{
		//isMoving = false;
		Move();
		Ground.Position = ConstGroundPos;
		Pos = Position;
		isMoving = true;
	}
	public void Move()
	{
		//播放行走动画
		animation.Play("Walk", customSpeed: WalkSpeed);
	}

	/// <summary>
	/// 受伤
	/// </summary>
	/// <param name="damage"></param>
	public void Hurt(int damage)
	{
		// 减血
		HP -= damage;
		// 如果血量 <= 0 则死亡
		if (HP <= 180 && HP + damage > 180 && HP > 0 )
		{
			//Zombie_outerarm_upper
			GetNode<Sprite2D>("./Zombie_outerarm_upper").Texture = GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper2.png");
			//Zombie_outerarm_lower
			GetNode<Sprite2D>("./Zombie_outerarm_lower").Visible = false;
			//Zombie_outerarm_hand
			GetNode<Sprite2D>("./Zombie_outerarm_hand").Visible = false;
			// 播放断臂粒子
			GetNode<GpuParticles2D>("./Particle_ZombieArm").Emitting = true;
		}
		if (HP < 90 && HP + damage >= 90 && HP > 0)
		{
			Dying();
		}
		//if (HP <= 0)
		//{
		//	Die();
		//}
		GetParent<MainGame>().Call("UpdateZombieHP");
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
		GetNode<Sprite2D>("./Anim_head1").Visible = false; // 隐藏头部
		GetNode<Sprite2D>("./Anim_head2").Visible = false; // 隐藏下巴
		GetNode<Sprite2D>("./Anim_hair").Visible = false; // 隐藏头发
		// 播放掉头粒子
		GetNode<GpuParticles2D>("./Particle_ZombieHead").Emitting = true;
		

		//GetTree().CallGroup("zombies", "RemoveZombie", this);
	}

	async public void Die()
	{
		// 删除Area2D节点
		RemoveChild(GetNode<Area2D>("./DefenseArea"));
		// 播放死亡动画
		animation.Play("Death");
		isMoving = false;
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
			GetParent<MainGame>().Call("RemoveZombie", this);
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
		GetNode<LightOccluder2D>("./GroundConstraint").OccluderLightMask = 2 << (Row - 1);
		GetNode<GpuParticles2D>("./Particle_ZombieArm").LightMask = 2 << (Row - 1);
		GetNode<GpuParticles2D>("./Particle_ZombieHead").LightMask = 2 << (Row - 1);
		// 设置调试信息——波数
		GetNode<TextEdit>("./TextEdit").Text = Wave.ToString();
		Position = new Vector2(1050, scene.LawnLeftTopPos.Y + Row * scene.LawnUnitWidth - 35);

	}
}
