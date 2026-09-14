using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 每关开打前的选卡面板。
///
/// 虽然同样是 CanvasLayer + 原生 Control，但面板本身是**贴在游戏视口左侧的一块**，
/// 不是全屏弹窗——原版选卡时右侧草坪照常可见，也没有压暗遮罩。
///
/// 底板与按钮直接用美术素材，卡片复用局内的 SeedPacketLarger 场景、缩到一半显示。
/// 这样卡面、植物肖像、价格数字的呈现跟种子栏里完全一致，不必维护第二套画法。
/// </summary>
public partial class SeedSelectScreen : CanvasLayer
{
	// ---- 布局常量。数值是对着原版截图量的，飞行动效定下来之后可能要重调 ----

	/// <summary>面板左边缘顶到屏幕左边，横向不留边距</summary>
	private const float PanelLeft = 0f;

	/// <summary>底板素材尺寸，用来推算内部元素的落点</summary>
	private static readonly Vector2 PanelSize = new(465, 513);

	/// <summary>卡片网格列数</summary>
	private const int Columns = 8;

	/// <summary>
	/// 单张卡片的落位尺寸，也是网格步长。
	/// 卡片场景内部已经把 100×140 的素材连 Sprite2D 带 Area2D 一起缩到 0.5 了，
	/// 所以实例出来就是 50×70，这里不要再对它缩放一次。
	/// </summary>
	private static readonly Vector2 CardSize = new(50, 70);

	/// <summary>
	/// 卡片之间的间隙。卡片素材是整张卡面、边框占满 100×140，
	/// 紧挨着排会变成两条边框贴在一起，看着发挤。
	/// </summary>
	private const float CardGap = 2f;

	/// <summary>网格顶边相对面板顶边的距离（让开顶部的标题条）</summary>
	private const float GridTop = 38f;

	// ---- 素材路径 ----

	private const string BackgroundTexturePath = "res://art/MainGame/SeedChooser_Background.png";
	private const string ButtonTexturePath = "res://art/MainGame/SeedChooser_Button.png";
	private const string ButtonDisabledTexturePath = "res://art/MainGame/SeedChooser_Button_Disabled.png";
	private const string CardScenePath = "res://MainGame/SeedPacketLarger.tscn";

	// ---- 状态 ----

	private SeedSelection _selection;
	private TaskCompletionSource<bool> _result;
	private readonly List<SeedPacketLarger> _cards = new();
	private TextureButton _rockButton;
	private Label _hintLabel;

	/// <summary>面板关闭后的结果：true = 已确认开局，false = 被取消</summary>
	public Task<bool> Result => _result.Task;

	/// <summary>
	/// 弹出选卡面板并等玩家确认。
	/// 返回 true 表示可以开局，此时 selection.Selected 的顺序就是卡槽顺序。
	/// </summary>
	public static async Task<bool> ShowFor(Node parent, SeedSelection selection)
	{
		SeedSelectScreen screen = new();
		screen.Begin(selection);
		parent.AddChild(screen);
		bool confirmed = await screen.Result;
		screen.QueueFree();
		return confirmed;
	}

	/// <summary>初始化。要在 AddChild 之前调用，好让 Result 从入树那一刻起就有效</summary>
	public void Begin(SeedSelection selection)
	{
		_selection = selection;
		_result = new TaskCompletionSource<bool>();
		Layer = 110; // 盖在游戏内的 CanvasLayer 之上
	}

	public override void _Ready()
	{
		// 摆位要用视口尺寸，只能等入树之后再建
		Build();
		Refresh();
	}

	private void Build()
	{
		// 左边缘与下边缘都顶到屏幕边，纵坐标由视口高度减面板高度得来
		Vector2 viewport = GetViewport().GetVisibleRect().Size;
		Control root = new()
		{
			Position = new Vector2(PanelLeft, viewport.Y - PanelSize.Y),
			// 面板只占左侧一块，其余区域的点击不该被它吞掉
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(root);

		TextureRect background = new()
		{
			Texture = GD.Load<Texture2D>(BackgroundTexturePath),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		root.AddChild(background);

		Label title = new()
		{
			Text = "选择植物",
			HorizontalAlignment = HorizontalAlignment.Center,
			Position = new Vector2(0, 8),
			Size = new Vector2(PanelSize.X, 28),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", Colors.White);
		root.AddChild(title);

		BuildCards(root);
		BuildRockButton(root);
	}

	private void BuildCards(Control root)
	{
		PackedScene cardScene = GD.Load<PackedScene>(CardScenePath);
		if (cardScene == null)
		{
			GD.PrintErr($"[SeedSelectScreen] 卡片场景加载失败：{CardScenePath}");
			return;
		}

		// 水平居中：按实际用到的列数算网格总宽，两侧留等宽边距
		int usedColumns = Mathf.Min(Columns, _selection.Available.Count);
		float gridWidth = usedColumns * (CardSize.X + CardGap) - CardGap;
		float originX = (PanelSize.X - gridWidth) / 2f;

		int index = 0;
		foreach (PlantTypeEnum type in _selection.Available)
		{
			SeedPacketLarger card = cardScene.Instantiate<SeedPacketLarger>();
			card.SeedScene = PlantTypes.Instance.GetScene(type); // 必须在入树前设好，_Ready 要用
			card.Position = new Vector2(
				originX + index % Columns * (CardSize.X + CardGap),
				GridTop + index / Columns * (CardSize.Y + CardGap));
			root.AddChild(card);

			// 面板里的卡不参与局内的阳光不足判定与冷却遮罩。
			// 入树之后再关，免得树外设置被引擎忽略
			card.SetProcess(false);

			// 卡片自带的 OnInputEvent 只服务种子栏，这里另接一个用于面板选择
			ConnectCardInput(card.GetNode<Area2D>("Area2D"), type);
			_cards.Add(card);
			index++;
		}

		// 网格按整行补满：剩下的格子放空槽，露出白色卡槽底
		int totalSlots = Mathf.CeilToInt(_selection.Available.Count / (float)Columns) * Columns;
		for (int i = _selection.Available.Count; i < totalSlots; i++)
		{
			SeedPacketLarger slot = cardScene.Instantiate<SeedPacketLarger>();
			slot.Position = new Vector2(
				originX + i % Columns * (CardSize.X + CardGap),
				GridTop + i / Columns * (CardSize.Y + CardGap));
			root.AddChild(slot);

			// 空槽没有展示植物，_Process 里的 seedShow 会是 null，必须关掉
			slot.SetProcess(false);
			slot.SetEmptySlot();
		}
	}

	private void ConnectCardInput(Area2D area, PlantTypeEnum type)
	{
		area.InputEvent += (Node viewport, InputEvent @event, long shapeIdx) =>
		{
			if (@event.IsActionPressed("mouse_left"))
			{
				OnCardClicked(type);
			}
		};
	}

	private void BuildRockButton(Control root)
	{
		Texture2D normal = GD.Load<Texture2D>(ButtonTexturePath);
		Vector2 buttonSize = normal?.GetSize() ?? new Vector2(156, 42);

		_rockButton = new TextureButton
		{
			TextureNormal = normal,
			TextureDisabled = GD.Load<Texture2D>(ButtonDisabledTexturePath),
			Position = new Vector2((PanelSize.X - buttonSize.X) / 2f, PanelSize.Y - buttonSize.Y - 16),
			Size = buttonSize,
		};
		_rockButton.Pressed += Confirm;

		Label caption = new()
		{
			Text = "LET'S ROCK!",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Size = buttonSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		caption.AddThemeFontSizeOverride("font_size", 20);
		caption.AddThemeColorOverride("font_color", Colors.White);
		_rockButton.AddChild(caption);
		root.AddChild(_rockButton);

		_hintLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Position = new Vector2(0, PanelSize.Y - buttonSize.Y - 42),
			Size = new Vector2(PanelSize.X, 24),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_hintLabel.AddThemeFontSizeOverride("font_size", 16);
		_hintLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.8f));
		root.AddChild(_hintLabel);
	}

	private void OnCardClicked(PlantTypeEnum type)
	{
		if (!_selection.Toggle(type))
		{
			return;
		}

		Refresh();

		// 种子栏实时跟着变。原版这里是卡片平移过去的动画，
		// 动效还没定，先用直接重排占位
		SeedBank.Instance?.ApplySeedSelection(_selection.Selected);
	}

	private void Refresh()
	{
		// 已选中的卡压暗：用的就是局内"阳光不足"那一层 CostColorRect，
		// 卡片场景里它是 alpha 0.5 的黑。面板卡关掉了 _Process，不会被自动改回来
		IReadOnlyList<PlantTypeEnum> available = _selection.Available;
		for (int i = 0; i < _cards.Count && i < available.Count; i++)
		{
			_cards[i].GetNode<ColorRect>("CostColorRect").Visible = _selection.IsSelected(available[i]);
		}

		_hintLabel.Text = $"已选 {_selection.SelectedCount} / {_selection.MaxSlots}";
		_rockButton.Disabled = !_selection.CanStart;
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
