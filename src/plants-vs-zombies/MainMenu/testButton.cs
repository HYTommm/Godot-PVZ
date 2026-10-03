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
		Log.Debug("Button pressed!");
	}

	public void OnButtonUp()
	{
		Log.Debug("Button up!");
	}

	public void OnButtonDown()
	{
		Log.Debug("Button down!");
	}

	public void OnButtonToggled(bool toggled)
	{
		Log.Debug("Button toggled!");
		Log.Debug(toggled);
	}
}
