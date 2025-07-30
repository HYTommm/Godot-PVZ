using Godot;
using System;

public partial class SeedBank : Sprite2D
{
	public MainGame MainGame; // 主游戏节点
	public AudioStreamPlayer FlashWarningSound = new();
	// 布尔值：是否禁止选卡
	public bool BIsForbiddenSelect = false;
	public override void _Ready()
	{
		//UpdateSunCount();
		MainGame = MainGame.Instance;
		FlashWarningSound.Stream = GD.Load<AudioStream>("res://sounds/buzzer.ogg");
		AddChild(FlashWarningSound);
	}
	public void UpdateSunCount()
	{
		int count = MainGame.SunCount;
		GetNode<Label>("SunCountLabel").Text = count.ToString();
	}

	public void SunCountFlashWarning()
	{
		GetNode<AnimationPlayer>("SunCountFlashWarning").Play("SunCountFlashWarning");

		FlashWarningSound.Play();
	}
	
	
}
