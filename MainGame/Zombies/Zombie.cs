using Godot;
using System;

public partial class Zombie : Node2D
{
	bool isMoving = true;
	Vector2 Pos;
	Vector2 ConstGroundPos = new((float)-9.8, 40);
	AnimationPlayer animation;
	Sprite2D Ground;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pos = Position;
		animation = GetNode<AnimationPlayer>("./AnimationPlayer");
		Ground = GetNode<Sprite2D>("./_ground");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (isMoving)
		{
			Position = Pos + (ConstGroundPos - Ground.Position);
		}

	}
	public void PosUpdate(StringName animName)
	{
		//isMoving = false;
		//Move();
		Pos = Position;
	}
	public void Move()
	{
		
		animation.Play("Walk");
		isMoving = true;
	}
}
