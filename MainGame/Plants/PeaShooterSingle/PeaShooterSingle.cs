using Godot;
using System;

public partial class PeaShooterSingle : Plants
{
	Vector2 HeadPos;
	Vector2 ConstStemPos = new((float)37.6, (float)48.7);
	AnimationPlayer Anim_Idle;
	AnimationPlayer Anim_Head;
	public AudioStreamPlayer ShootSound = new AudioStreamPlayer();
	Node2D Stem, Head;
	double ShootTimer = 0.0f;

	[Export]
	public PackedScene BulletScene { get; set; }

	public override void _Idle()
	{
		Anim_Idle.Play("Idle");
		Anim_Head.Play("Head_Idle");
	}

	public override void _Ready()
	{
		Anim_Idle = GetNode<AnimationPlayer>("./Idle");
		Anim_Head = GetNode<AnimationPlayer>("./Head/Head");
		ShootSound.Stream = (AudioStream)GD.Load("res://sounds/throw.ogg");
		ShootSound.VolumeDb = -5;
		AddChild(ShootSound);
		Stem = GetNode<Node2D>("./Anim_stem");
		Head = GetNode<Node2D>("./Head");
		HeadPos = Head.Position;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Head.Position = HeadPos + (Stem.Position - ConstStemPos);

	}

	async public void Shoot()
	{
		ShootTimer = Anim_Head.CurrentAnimationPosition;
		
		Anim_Head.Play("Head_Shooting", 1.0/3.0, 2.5f);
		await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
		Bullet bullet = BulletScene.Instantiate<Bullet>();
		//GD.Print(bullet);
		bullet.Position = GetNode<Node2D>("./Head/Idle_mouth").Position + new Vector2(12, 7);
		bullet.ZIndex = 1;
		AddChild(bullet);
		ShootSound.Play();
		//Anim_Shoot.Play("RESET");
	}

	public void StopShooting(StringName anim)
	{
		if (anim == "Head_Shooting")
		{
			Anim_Head.Play("Head_Idle", 1.0/3.0, 0.0f);
			Anim_Head.Seek(ShootTimer + 2/2.5f);
			Anim_Head.Play("Head_Idle", 1.0/3.0, 1.0f);
		}
	}

	public override void _Plant()
	{
		base._Plant();
		// 计时器开启
		GetNode<Timer>("./Timer").Start();
	}
}
