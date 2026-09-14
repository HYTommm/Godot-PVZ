using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 每关开打前的选卡界面。
///
/// 用 CanvasLayer + 原生 Control 搭（与 LevelSelectScreen 同一套选型）：
///   - 这是覆盖层而不是世界物体，不需要相机变换，全屏遮罩 + 居中布局正是 Control 的场景
///   - 卡片数量将来会随解锁进度变化，交给 GridContainer 自动排，不必手算坐标
///   - Control 的 MouseFilter 会自动吞掉下层点击；Node2D / Area2D 体系不受 Control 遮挡影响，
///     那样还得像 LevelSelectScreen 一样手动 SetProcessInput(false) 才挡得住
///
/// 面板上的卡片暂时是普通 Button 占位，等美术资源到位后换取本体即可，下面的逻辑不用动。
/// </summary>
public partial class SeedSelectScreen : CanvasLayer
{
	private const int Columns = 5;
	private const int CellWidth = 132;
	private const int CellHeight = 92;
	private const int CellGap = 12;

	private SeedSelection _selection;
	private TaskCompletionSource<bool> _result;
	private readonly List<Button> _cards = new();
	private readonly Dictionary<PlantTypeEnum, string> _costCache = new();
	private Label _slotLabel;
	private Button _confirmButton;

	/// <summary>面板关闭后的结果：true = 已确认开局，false = 被取消</summary>
	public Task<bool> Result => _result.Task;

	/// <summary>
	/// 弹出选卡界面并等玩家确认。
	/// 返回 true 表示可以开局，此时 selection.Selected 的顺序就是卡槽顺序。
	/// </summary>
	/// <param name="parent">挂到哪个节点下（通常是 MainGame）</param>
	/// <param name="selection">本局的选卡状态，界面会直接在上面增删</param>
	public static async Task<bool> ShowFor(Node parent, SeedSelection selection)
	{
		SeedSelectScreen screen = new();
		screen.Begin(selection);
		parent.AddChild(screen);
		bool confirmed = await screen.Result;
		screen.QueueFree();
		return confirmed;
	}

	/// <summary>建界面。要在 AddChild 之前调用，好让 Result 从入树那一刻起就有效</summary>
	public void Begin(SeedSelection selection)
	{
		_selection = selection;
		_result = new TaskCompletionSource<bool>();

		Layer = 110; // 盖在游戏内的 CanvasLayer 之上
		Build();
		Refresh();
	}

	private void Build()
	{
		ColorRect backdrop = new()
		{
			Color = new Color(0, 0, 0, 0.72f),
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
		box.AddThemeConstantOverride("separation", 16);
		center.AddChild(box);

		Label title = new()
		{
			Text = "选择植物",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 32);
		box.AddChild(title);

		GridContainer grid = new() { Columns = Columns };
		grid.AddThemeConstantOverride("h_separation", CellGap);
		grid.AddThemeConstantOverride("v_separation", CellGap);
		box.AddChild(grid);

		// _cards 的下标与 _selection.Available 一一对应，Refresh 里直接按位取
		foreach (PlantTypeEnum type in _selection.Available)
		{
			Button card = new()
			{
				CustomMinimumSize = new Vector2(CellWidth, CellHeight),
				ToggleMode = true, // 选中态直接反映在按钮外观上
			};
			card.Pressed += () => OnCardPressed(type);
			grid.AddChild(card);
			_cards.Add(card);
		}

		_slotLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_slotLabel.AddThemeFontSizeOverride("font_size", 20);
		box.AddChild(_slotLabel);

		_confirmButton = new Button
		{
			Text = "开始游戏",
			CustomMinimumSize = new Vector2(CellWidth * 2, CellHeight * 0.8f),
		};
		_confirmButton.Pressed += Confirm;
		box.AddChild(_confirmButton);
	}

	private void OnCardPressed(PlantTypeEnum type)
	{
		if (_selection.Toggle(type))
		{
			Refresh();
		}
	}

	/// <summary>把选卡状态刷到界面上：卡面文字、选中态、已选计数、开始按钮可用性</summary>
	private void Refresh()
	{
		IReadOnlyList<PlantTypeEnum> available = _selection.Available;
		for (int i = 0; i < _cards.Count && i < available.Count; i++)
		{
			PlantTypeEnum type = available[i];
			Button card = _cards[i];
			int slot = _selection.SlotNumber(type);
			string name = PlantTypes.Instance.GetDisplayName(type);

			// 序号 = 点选顺序，玩家能一眼看出哪张卡会落在哪个槽位
			card.Text = slot > 0 ? $"{slot}. {name}\n{DescribeCost(type)}" : $"{name}\n{DescribeCost(type)}";
			card.ButtonPressed = slot > 0;
			card.Disabled = slot == 0 && _selection.IsFull;
		}

		_slotLabel.Text = $"已选 {_selection.SelectedCount} / {_selection.MaxSlots}";
		_confirmButton.Disabled = !_selection.CanStart;
	}

	/// <summary>
	/// 卡面的阳光花费。数值从植物场景实例上读，注册表里不留第二份。
	/// 结果缓存，避免每次点卡都重新实例化一遍。
	/// </summary>
	private string DescribeCost(PlantTypeEnum type)
	{
		if (_costCache.TryGetValue(type, out string cached))
		{
			return cached;
		}

		string text = "—";
		PackedScene scene = PlantTypes.Instance.GetScene(type);
		if (scene != null)
		{
			Plants plant = scene.Instantiate<Plants>();
			if (plant != null)
			{
				text = plant.SunCost >= 0 ? $"☀ {plant.SunCost}" : "—";
				plant.Free(); // 只为读数值而实例化，没入过树，可以立刻释放
			}
		}

		_costCache[type] = text;
		return text;
	}

	private void Confirm()
	{
		if (!_selection.CanStart)
		{
			return;
		}
		GD.Print($"[SeedSelectScreen] 选卡确认，共 {_selection.SelectedCount} 种");
		_result.TrySetResult(true);
	}
}
