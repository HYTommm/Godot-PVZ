using Godot;
using System.Collections.Generic;

/// <summary>
/// 选关界面。长按主菜单的 Adventure 按钮弹出的网格，按关卡索引 LevelList 的行列结构生成。
///
/// 用 CanvasLayer + 原生 Control 搭，而不是复用 GameBaseButton：
/// GameBaseButton 是 Sprite2D，悬停靠场景里接的 Area2D 信号，位置要跟着相机走；
/// 而这里需要的是相机无关、锚点自动居中的覆盖层，用 Control 才是对的工具。
/// 视觉暂用原生 Control（D12：现在无需考虑动效），后续要换回自实现按钮风格再说。
/// </summary>
public partial class LevelSelectScreen : CanvasLayer
{
	/// <summary> 每行放几个关卡格子 </summary>
	private const int Columns = 5;
	private const int CellWidth = 150;
	private const int CellHeight = 64;
	private const int CellGap = 14;

	private readonly List<GameBaseButton> _menuButtonsDisabled = new();
	private PackedScene _mainGameScene;
	private bool _built;

	public override void _Ready()
	{
		Layer = 100;
		Visible = false;
		_mainGameScene = ResourceLoader.Load<PackedScene>("res://MainGame/MainGame.tscn");
		Build();
	}

	private void Build()
	{
		if (_built)
		{
			return;
		}
		_built = true;

		ColorRect backdrop = new()
		{
			Color = new Color(0, 0, 0, 0.78f),
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(backdrop);

		CenterContainer center = new()
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		AddChild(center);

		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 18);
		center.AddChild(box);

		Label title = new()
		{
			Text = "选择关卡",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 32);
		box.AddChild(title);

		GridContainer grid = new() { Columns = Columns };
		grid.AddThemeConstantOverride("h_separation", CellGap);
		grid.AddThemeConstantOverride("v_separation", CellGap);
		box.AddChild(grid);

		LevelList levelList = Global.Instance?.GetLevelList();
		if (levelList == null)
		{
			Label error = new()
			{
				Text = "关卡索引加载失败：\nres://MainGame/Levels/LevelList.tres",
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			box.AddChild(error);
		}
		else
		{
			int cellCount = 0;
			levelList.ForEachLevel((world, index, level) =>
			{
				grid.AddChild(CreateCell(world, index, level));
				cellCount++;
			});
			GD.Print($"[LevelSelectScreen] 生成 {cellCount} 个关卡格子，共 {levelList.WorldCount} 个世界");
		}

		Button back = new()
		{
			Text = "返回",
			CustomMinimumSize = new Vector2(CellWidth, CellHeight * 0.7f),
		};
		back.Pressed += Close;
		box.AddChild(back);
	}

	private Button CreateCell(int world, int index, LevelData level)
	{
		// 格子文字用 LevelName 而不是拼 "world-index"，
		// 因为调试图关所在的位置不该显示成 "2-1"
		Button cell = new()
		{
			Text = level.LevelName,
			CustomMinimumSize = new Vector2(CellWidth, CellHeight),
			TooltipText = $"{level.LevelName}（{level.Waves} 波，场景 {level.SceneType}）",
		};
		cell.Pressed += () => ChooseLevel(level);
		return cell;
	}

	private void ChooseLevel(LevelData level)
	{
		if (level == null)
		{
			GD.PrintErr("[LevelSelectScreen] 选到的关卡为空");
			return;
		}
		GD.Print($"[LevelSelectScreen] 选择关卡 {level.LevelId}");
		if (Global.Instance != null)
		{
			Global.Instance.CurrentLevelData = level;
		}
		if (_mainGameScene == null)
		{
			GD.PrintErr("[LevelSelectScreen] MainGame.tscn 加载失败");
			return;
		}
		GetTree().ChangeSceneToPacked(_mainGameScene);
	}

	public void Open()
	{
		if (GetParent() is MainMenu_SelectorScreen menu)
		{
			menu.StopBgm();
		}
		SetMenuButtonsEnabled(false);
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
		SetMenuButtonsEnabled(true);
	}

	/// <summary>
	/// 开关本界面时启停菜单里那些自实现按钮的 _Input。
	/// 必须做：GameBaseButton 的悬停判定走 Area2D，不受 Control 遮挡影响，
	/// 不关掉的话隔着遮罩点到 Adventure 按钮所在区域仍会触发它。
	/// </summary>
	private void SetMenuButtonsEnabled(bool enabled)
	{
		if (!enabled)
		{
			_menuButtonsDisabled.Clear();
			CollectMenuButtons(GetParent(), _menuButtonsDisabled);
		}
		foreach (GameBaseButton button in _menuButtonsDisabled)
		{
			if (IsInstanceValid(button))
			{
				button.SetProcessInput(enabled);
			}
		}
		if (enabled)
		{
			_menuButtonsDisabled.Clear();
		}
	}

	private static void CollectMenuButtons(Node node, List<GameBaseButton> result)
	{
		if (node == null)
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child is GameBaseButton button)
			{
				result.Add(button);
			}
			CollectMenuButtons(child, result);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
		{
			return;
		}
		if (@event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
