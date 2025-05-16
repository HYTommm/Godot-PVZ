using Godot;
using System;

public partial class SeedBank : Sprite2D
{
	public AudioStreamPlayer FlashWarningSound = new AudioStreamPlayer();
	public override void _Ready()
	{
		//UpdateSunCount();
		FlashWarningSound.Stream = GD.Load<AudioStream>("res://sounds/buzzer.ogg");
		FlashWarningSound.VolumeDb -= 5;
		AddChild(FlashWarningSound);
	}
	public void UpdateSunCount()
	{
		int count = GetParent<MainGame>().SunCount;
		GetNode<Label>("SunCountLabel").Text = count.ToString();
	}

	public void SunCountFlashWarning()
	{
		GetNode<AnimationPlayer>("SunCountFlashWarning").Play("SunCountFlashWarning");
		
		FlashWarningSound.Play();
	}
}
