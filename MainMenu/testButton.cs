using Godot;
using System;

public partial class testButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void OnButtonPressed()
	{
		GD.Print("Button pressed!");
	}

	public void OnButtonUp()
	{
		GD.Print("Button up!");
	}

	public void OnButtonDown()
	{
		GD.Print("Button down!");
	}

	public void OnButtonToggled(bool toggled)
	{
		GD.Print("Button toggled!");
		GD.Print(toggled);
	}
}
