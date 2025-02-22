using Godot;
using System;

public partial class Zombie : Node2D
{
	[Signal]
	public delegate void ZombieDieEventHandler();
	[Export]
	public bool isMoving = true;                         //是否正在移动
	public Vector2 Pos;                                  //位置记录
	public Vector2 ConstGroundPos = new((float)-9.8, 40);//地面位置
	public AnimationPlayer animation;                    //动画播放器
	public Sprite2D Ground;                              //地面节点
	public int HP = 100;                                 // 生命值
	public int MaxHP = 100;                              // 最大生命值
	// 索引
	public int Index;
	public int Wave;                                     // 所处波数
	public int Row;                                      // 所处行
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		
		if (isMoving)
		{
			Position = Pos + (ConstGroundPos - Ground.Position);
		}
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
		animation.Play("Walk");
	}

	//受伤
	public void Hurt(int damage)
	{
		// 减血
		HP -= damage;
		// 如果血量 <= 0 则死亡
		if (HP <= 50 && HP + damage > 50 && HP > 0 )
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
		if (HP <= 0)
		{
			Die();
		}
		GetParent<MainGame>().Call("UpdateZombieHP");
	}
	//死亡
	async public void Die()
	{
		// 发出死亡信号
		EmitSignal("ZombieDie");
		// 播放掉头粒子
		GetNode<GpuParticles2D>("./Particle_ZombieHead").Emitting = true;
		// 删除Area2D节点
		RemoveChild(GetNode<Area2D>("./Area2D"));
		
		// 播放死亡动画
		animation.Play("Death");
		
		isMoving = false;
		//销毁节点
		await ToSignal(animation, "animation_finished");
		FreeZombie();
		//GetTree().CallGroup("zombies", "RemoveZombie", this);
	}

	public void FreeZombie()
	{
		GetParent<MainGame>().Call("RemoveZombie", this);
		QueueFree();
	}

	public void Refresh(int index, Scene scene, int wave, int row)
	{
		HP = 100;
		Index = index;
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
