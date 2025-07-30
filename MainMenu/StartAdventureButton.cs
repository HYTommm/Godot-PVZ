using Godot;
using static ResourceManager.Sounds;
using System;

public partial class StartAdventureButton : GameBaseButton
{
	public PackedScene MainGameScene;
	public override void _Ready()
	{

		base._Ready();
		TapSound.Stream = Sound_GraveButton;
		MainGameScene = ResourceLoader.Load<PackedScene>("res://MainGame/MainGame.tscn");
		
	}
	public override void GetClicked()
	{
		((MainMenu_SelectorScreen)Main).StopBgm();
		// 停止背景音乐
		ZombieHand zombieHand = GetNode<ZombieHand>("../../ZombieHand");
		ColorRect colorRect = GetNode<ColorRect>("../../ColorRect");
		colorRect.Visible = true;
		zombieHand.Play();
		zombieHand.AnimEnd += StartGame;
	}

	public void StartGame()
	{
		GD.Print("start game");
		GetTree().ChangeSceneToPacked(MainGameScene);
	}
}
