using Godot;
using static ResourceDB.Sounds;

public partial class StartAdventureButton : GameBaseButton
{
	/// <summary>
	/// 长按判定阈值（秒）。按下超过这个时长，抬起时弹选关界面而不是直接开局。
	/// 单击 / 长按的分流都在这里，Adventure 和 StartAdventure 两个按钮共用本脚本，
	/// 所以两个按钮的行为天然一致。
	/// </summary>
	public const double LongPressSeconds = 1.0;

	/// <summary> 按下时刻（秒，来自 Time.GetTicksMsec）；-1 表示当前没有按下 </summary>
	private double _pressedAtSeconds = -1.0;

	public override void _Ready()
	{
		base._Ready();
		TapSound.Stream = Sound_GraveButton;
		if (Main == null)
		{
			GD.PrintErr($"[StartAdventureButton] {Name} 的 Main 未指定，点击和长按都不会生效");
		}
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

		// 开局流程全在菜单场景里，这里只上报意图
		if (Main is not MainMenu_SelectorScreen menu)
		{
			return;
		}

		if (heldSeconds >= LongPressSeconds)
		{
			// 长按：打开选关界面，不直接开局
			menu.OpenLevelSelect();
			return;
		}

		// 单击：固定进 1-1（第一版不做"推进到下一关"）
		menu.StartAdventure(Global.Instance?.GetLevel(1, 1));
	}
}
