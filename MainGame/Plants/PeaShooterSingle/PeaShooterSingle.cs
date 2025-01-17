using Godot;
using System;

public partial class PeaShooterSingle : Node2D
{
	Vector2 HeadPos;
	Vector2 ConstStemPos = new((float)37.6, (float)48.7);
	AnimationPlayer Anim_Idle;
	AnimationPlayer Anim_Head;
	Node2D Stem, Head;
	double ShootTimer = 0.0f;
	public override void _Ready()
	{
		Anim_Idle = GetNode<AnimationPlayer>("./Idle");
		Anim_Head = GetNode<AnimationPlayer>("./Head/Head");
		Stem = GetNode<Node2D>("./Anim_stem");
		Head = GetNode<Node2D>("./Head");
		HeadPos = Head.Position;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Head.Position = HeadPos + (Stem.Position - ConstStemPos);

	}

	public void Shoot()
	{
		ShootTimer = Anim_Head.CurrentAnimationPosition;

		Anim_Head.Play("Head_Shooting", -1.0f, 2.5f);
		//Anim_Shoot.Play("RESET");
	}

	public void StopShooting(StringName anim)
	{
		if (anim == "Head_Shooting")
		{
			Anim_Head.Play("Head_Idle", -1.0f, 0.0f);
			Anim_Head.Seek(ShootTimer + 2/2.5f);
			Anim_Head.Play("Head_Idle", -1.0f, 1.0f);
		}
	}
}
