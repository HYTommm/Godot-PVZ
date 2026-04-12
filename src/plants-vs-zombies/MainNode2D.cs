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
		if (@event.IsAction("mouse_left"))
        {
            BMouse_left_down = !BMouse_left_down;
            EmitSignal(BMouse_left_down ? SignalName.MouseLeftDown : SignalName.MouseLeftUp);
        }
		if (@event.IsAction("mouse_right"))
		{
			BMouseRightDown = !BMouseRightDown;
		}
	}
}
