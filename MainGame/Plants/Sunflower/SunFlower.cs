using Godot;
using System;

public partial class SunFlower : Plants
{
	public AnimationPlayer Anim_Idle;

	public override void _Idle()
	{
		Anim_Idle.Play("Idle");
		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Anim_Idle = GetNode<AnimationPlayer>("./Idle");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
