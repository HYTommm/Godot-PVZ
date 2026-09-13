using Godot;
using static ResourceDB.Sounds;
using System;

public partial class StartAdventureButton : GameBaseButton
{
	/// <summary>
	/// 长按判定阈值（秒）。按下超过这个时长，抬起时弹选关界面而不是直接开局。
	/// 单击 / 长按的分流都在这里，Adventure 和 StartAdventure 两个按钮共用本脚本，
	/// 所以两个按钮的行为天然一致。
	/// </summary>
	public const double LongPressSeconds = 1.0;

	public PackedScene MainGameScene;

	/// <summary> 按下时刻（秒，来自 Time.GetTicksMsec）；-1 表示当前没有按下 </summary>
	private double _pressedAtSeconds = -1.0;

	/// <summary> 单击开局时选定的关卡，等过渡动画播完再由 StartGame 使用 </summary>
	private LevelData _pendingLevel;

	private ZombieHand _zombieHand;
	private ColorRect _colorRect;

	public override void _Ready()
	{
		base._Ready();
		TapSound.Stream = Sound_GraveButton;
		MainGameScene = ResourceLoader.Load<PackedScene>("res://MainGame/MainGame.tscn");

		// 节点和信号在 _Ready 里一次接好：ZombieHand 此时已经在树上，
		// 没必要每次点击都重新取节点、重新订阅（原来的 += 会累积连接，
		// 导致重复点击时 StartGame 被调用多次）
		_zombieHand = GetNode<ZombieHand>("../../ZombieHand");
		_colorRect = GetNode<ColorRect>("../../ColorRect");
		_zombieHand.AnimEnd += StartGame;

	}

	public override void _Input(InputEvent @event)
	{
		// 先记下按下时刻，再交给基类分派按下 / 抬起。
		// 基类是在"抬起"时回调 GetClicked 的，所以到那时才可能算出按了多久。
		if (@event is InputEventMouseButton mouseEvent
			&& mouseEvent.ButtonIndex == MouseButton.Left
			&& mouseEvent.Pressed)
		{
			_pressedAtSeconds = Time.GetTicksMsec() / 1000.0;
		}
		base._Input(@event);
	}

	public override void GetClicked()
	{
		double heldSeconds = _pressedAtSeconds < 0
			? 0
			: Time.GetTicksMsec() / 1000.0 - _pressedAtSeconds;
		_pressedAtSeconds = -1.0;

		if (heldSeconds >= LongPressSeconds)
		{
			// 长按：打开选关界面，不直接开局
			GD.Print($"[StartAdventureButton] 长按 {heldSeconds:F2}s，打开选关界面");
			(Main as MainMenu_SelectorScreen)?.OpenLevelSelect();
			return;
		}

		// 单击：固定进 1-1（第一版不做"推进到下一关"）
		_pendingLevel = Global.Instance?.GetLevel(1, 1);
		GD.Print($"[StartAdventureButton] 单击 {heldSeconds:F2}s，进入关卡 {_pendingLevel?.LevelId ?? "null"}");
		PlayEnterAnimation();
	}

	/// <summary> 播进入游戏的过场动画，动画结束后由已接好的 AnimEnd 触发 StartGame </summary>
	private void PlayEnterAnimation()
	{
		(Main as MainMenu_SelectorScreen)?.StopBgm();
		// 停止背景音乐
		_colorRect.Visible = true;
		_zombieHand.Play();
	}

	public void StartGame()
	{
		GD.Print("start game");
		if (Global.Instance != null)
		{
			Global.Instance.CurrentLevelData = _pendingLevel;
		}
		GetTree().ChangeSceneToPacked(MainGameScene);
	}
}
