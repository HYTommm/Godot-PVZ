using Godot;
using System;

public partial class MainMenu_SelectorScreen : Node
{
	[Signal]
	public delegate void MouseLeftUpEventHandler();
	[Signal]
	public delegate void MouseLeftDownEventHandler();
	public bool mouse_left_down = false;
	public bool mouse_right_down = false;
	public bool mouse_picked = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Sprite2D StartAdventureButton = GetNode<Sprite2D>("BG_Right/StartAdventureButton");
		Sprite2D ContinueAdventureButton = GetNode<Sprite2D>("BG_Right/AdventureButton");
		//ContinueAdventureButton.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public override void _Input(InputEvent @event)
	{
		if (@event.IsAction("mouse_left"))
		{
			mouse_left_down = !mouse_left_down;
			if (!mouse_left_down)
			{
				EmitSignal(SignalName.MouseLeftUp);
			}
			else
			{
				EmitSignal(SignalName.MouseLeftDown);
			}
		}
		if (@event.IsAction("mouse_right"))
		{
			mouse_right_down= !mouse_right_down;
		}
	}
}
