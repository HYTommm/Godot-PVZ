using Godot;
using System;

public partial class MainMenu_SelectorScreen : MainNode2D
{
	// 声明AudioStreamPlayer节点
	private AudioStreamPlayer Menubgmplay;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Sprite2D StartAdventureButton = GetNode<Sprite2D>("BG_Right/StartAdventureButton");
		Sprite2D ContinueAdventureButton = GetNode<Sprite2D>("BG_Right/AdventureButton");
		//ContinueAdventureButton.Visible = false;
		// 创建AudioStreamPlayer节点
		Menubgmplay = new AudioStreamPlayer();

		// 加载背景音乐文件
		AudioStream Menubgm = GD.Load<AudioStream>("res://sounds/MainGame/main_menu.mp3");
		// 将音频流设置到AudioStreamPlayer
		Menubgmplay.Stream = Menubgm;

		// 将AudioStreamPlayer节点添加到场景中
		AddChild(Menubgmplay);

		// 播放背景音乐
		Menubgmplay.Play();

	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void StopBGM()
	{
		if (Menubgmplay != null && Menubgmplay.Playing)
		{
			Menubgmplay.Stop();
		}
	}
	
}
