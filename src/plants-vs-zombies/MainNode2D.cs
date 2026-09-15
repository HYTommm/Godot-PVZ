using Godot;
using System;

public partial class MainNode2D : Node2D
{
	[Signal]
	public delegate void MouseLeftUpEventHandler();
	[Signal]
	public delegate void MouseLeftDownEventHandler();
	public bool BMouse_left_down = false;
	public bool BMouseRightDown = false;
	public bool BMousePicked = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		// 按下和抬起分开判定。IsAction 对两者都返回真，靠取反来推状态的话，
		// 一旦两边不对称（丢焦点、拖出窗口）状态就反了
		if (@event.IsActionPressed("mouse_left"))
		{
			BMouse_left_down = true;
			EmitSignal(SignalName.MouseLeftDown);
		}
		else if (@event.IsActionReleased("mouse_left"))
		{
			BMouse_left_down = false;
			EmitSignal(SignalName.MouseLeftUp);
		}

		if (@event.IsActionPressed("mouse_right"))
		{
			BMouseRightDown = true;
		}
		else if (@event.IsActionReleased("mouse_right"))
		{
			BMouseRightDown = false;
		}
	}
}
