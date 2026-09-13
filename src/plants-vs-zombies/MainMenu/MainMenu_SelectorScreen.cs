using Godot;
using System;
using static ResourceDB.Sounds.Bgm;

public partial class MainMenu_SelectorScreen : MainNode2D
{
	// 声明AudioStreamPlayer节点
	private Scene _mainMenuScene;

	/// <summary> 选关界面（长按 Adventure 时打开）。在 _Ready 里建出来挂到本节点下，不动 .tscn </summary>
	public LevelSelectScreen LevelSelect { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_mainMenuScene = new MainMenuScene(Global.Instance);// 设置场景
		//_mainMenuScene.PlayMainGameBgm(); // 播放BGM
		//_mainMenuScene.TurnToNormalBgm();

		LevelSelect = new LevelSelectScreen();
		AddChild(LevelSelect);
	}

	public void StopBgm()
	{
		_mainMenuScene.StopAllBgm();
	}

	/// <summary> 打开选关界面 </summary>
	public void OpenLevelSelect()
	{
		LevelSelect?.Open();
	}

}
