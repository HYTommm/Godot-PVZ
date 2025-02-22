using Godot;
using System;

public partial class MainMenu_SelectorScreen : MainNode2D
{
	
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
	
}
