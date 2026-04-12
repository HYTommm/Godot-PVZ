using Godot;
using static ResourceDB.Sounds;
using System;

public partial class SeedBank : Sprite2D
{
	public static SeedBank Instance => _instance;
	private static SeedBank _instance;

	//public MainGame MainGame; // 主游戏节点
	[Export] private Label _sunCountLabel;
	[Export] private AnimationPlayer _animSunCountFlashWarning;

	public AudioStreamPlayer FlashWarningSound = new();
	// 布尔值：是否禁止选卡
	public bool BIsForbiddenSelect = false;
	public override void _Ready()
	{
		//UpdateSunCount();
		//MainGame = MainGame.Instance;
		FlashWarningSound.Stream = Sound_Buzzer;
		AddChild(FlashWarningSound);

		_instance = this;
	}
	public void UpdateSunCount()
	{
		int count = MainGame.Instance.SunCount;
		_sunCountLabel.Text = count.ToString();
	}

	public void SunCountFlashWarning()
	{
		_animSunCountFlashWarning.Play("SunCountFlashWarning");
		FlashWarningSound.Play();
	}
	
	
}
