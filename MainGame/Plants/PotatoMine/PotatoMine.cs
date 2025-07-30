using Godot;
using System;

public partial class PotatoMine : Plants
{
	[Export] public AnimationPlayer Anim_idle;
	[Export] public AnimationPlayer Anim_blink;
	[Export] public AnimationPlayer Anim_rise;
	[Export] public float TimeToRise = 15f;
	public Timer TimerToRise = new();

	//public override Vector2 Offset { get => base.Offset - TempOffset; set => base.Offset = value; }
	public override void _Ready()
	{
		base._Ready();
		AddChild(TimerToRise);
		TimerToRise.WaitTime = TimeToRise;
		TimerToRise.OneShot = true;
		TimerToRise.Timeout += Rise;
		
		//Position += new Vector2(30, 20);
	}

	public override void _Plant(int col, int row, int index)
	{
		base._Plant(col, row, index);
		TimerToRise.Start();
	}

	public override void _Idle()
	{
		Anim_idle.Play("PotatoMine_idle");
	}

	public void Rise()
	{
		Anim_rise.Play("PotatoMine_rise");
	}
}
