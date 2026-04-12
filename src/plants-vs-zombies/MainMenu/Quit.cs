using Godot;
using System;

public partial class Quit : GameBaseButton
{
	public override void GetClicked()
	{
		base.GetClicked();
		GetTree().Quit();
	}
}
