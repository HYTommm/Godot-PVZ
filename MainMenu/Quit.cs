using Godot;
using System;

public partial class Quit : GameButton
{
	public override void GetClicked()
	{
		base.GetClicked();
		GetTree().Quit();
	}
}
