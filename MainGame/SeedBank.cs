using Godot;
using System;

public partial class SeedBank : Sprite2D
{
    MainGame MainGame; // 主游戏节点
    public AudioStreamPlayer FlashWarningSound = new AudioStreamPlayer();
	// 布尔值：是否禁止选卡
	public bool isForbiddenSelect = false;
	public override void _Ready()
	{
		//UpdateSunCount();
		MainGame = this.GetMainGame();
		FlashWarningSound.Stream = GD.Load<AudioStream>("res://sounds/buzzer.ogg");
		AddChild(FlashWarningSound);
	}
	public void UpdateSunCount()
	{
		int count = this.GetMainGame().SunCount;
		GetNode<Label>("SunCountLabel").Text = count.ToString();
	}

	public void SunCountFlashWarning()
	{
		GetNode<AnimationPlayer>("SunCountFlashWarning").Play("SunCountFlashWarning");

		FlashWarningSound.Play();
	}
	
	
}
