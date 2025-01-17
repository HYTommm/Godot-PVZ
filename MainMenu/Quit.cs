using Godot;
using System;

public partial class Quit : Button
{
	public override void GetClicked()
	{
		base.GetClicked();
		GetTree().Quit();
	}
}
