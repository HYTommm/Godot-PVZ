using Godot;
using System;
using static ResourceDB.Sounds.Bgm;

public partial class MainMenu_SelectorScreen : MainNode2D
{
	// 声明AudioStreamPlayer节点
	private Scene _mainMenuScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_mainMenuScene = new MainMenuScene(Global.Instance);// 设置场景
		//_mainMenuScene.PlayMainGameBgm(); // 播放BGM
		//_mainMenuScene.TurnToNormalBgm();

	}

	public void StopBgm()
	{
		_mainMenuScene.StopAllBgm();
	}
	
}
