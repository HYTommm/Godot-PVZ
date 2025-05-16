using Godot;
using System;

public partial class StartAdventureButton : GameBaseButton
{
	public PackedScene MainGameSence;
	public override void _Ready()
	{
		base._Ready();
		TapSound.Stream = (AudioStream)GD.Load("res://sounds/gravebutton.ogg");
		MainGameSence = ResourceLoader.Load<PackedScene>("res://MainGame/MainGame.tscn");

	}
	public override void GetClicked()
	{
		ZombieHand zombieHand = GetNode<ZombieHand>("../../ZombieHand");
		ColorRect colorRect = GetNode<ColorRect>("../../ColorRect");
		colorRect.Visible = true;
		zombieHand.Play();
		zombieHand.AnimEnd += StartGame;
	}

	public void StartGame()
	{
		GD.Print("start game");
		GetTree().ChangeSceneToPacked(MainGameSence);
	}
}
