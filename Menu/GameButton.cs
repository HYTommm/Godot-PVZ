using Godot;
using System;

public partial class GameButton : GameBaseButton
{
	public override void _Ready()
	{
		base._Ready();
		has_frame = false;
		TapSound.Stream = (AudioStream)GD.Load("res://sounds/gravebutton.ogg");
	}

	public override void Bleep()
	{
		;
	}
}
