using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ResourceDB.Sounds;

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

	/// <summary>面板从屏幕下方升入的时长（秒）</summary>
	private const float PanelRiseDuration = 0.4f;

	/// <summary>卡片在面板与卡槽之间飞行的时长（秒）</summary>
	private const float CardFlyDuration = 0.25f;

	/// <summary>取消选卡后，后面的卡往左补位滑一格的时长（秒）。比飞回面板快，看着更像被抽走</summary>
	private const float SlotShiftDuration = 0.15f;

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

	/// <summary>按下卡片那一下的按钮音</summary>
	private readonly AudioStreamPlayer _tapSound = new();

	/// <summary>面板根节点。整个面板靠移动它来升降，飞行卡片则独立于它</summary>
	private Control _root;

	/// <summary>卡片场景，面板与飞行卡片共用</summary>
	private PackedScene _cardScene;

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
		_tapSound.Stream = Sound_Tap;
		AddChild(_tapSound);

		// 摆位要用视口尺寸，只能等入树之后再建
		Build();
		Refresh();
		RisePanel();

		// 点卡槽要把卡收回面板，这件事归面板管
		if (SeedBank.Instance != null)
		{
			SeedBank.Instance.PacketClickedWhileSelecting = OnSlotClicked;
		}
	}

	public override void _ExitTree()
	{
		if (SeedBank.Instance != null)
		{
			SeedBank.Instance.PacketClickedWhileSelecting = null;
		}
	}

	/// <summary>
	/// 面板从屏幕下方升到落位处。位移量就是面板自身高度，
	/// 起点让面板整个藏在屏幕底边之外。
	/// </summary>
	private void RisePanel()
	{
		Vector2 viewport = GetViewport().GetVisibleRect().Size;
		float hiddenY = viewport.Y;
		float shownY = viewport.Y - PanelSize.Y;

		Tween tween = CreateTween();
		tween.TweenMethod(
			Callable.From<float>(t =>
				_root.Position = new Vector2(PanelLeft, Mathf.Lerp(hiddenY, shownY, Ease(t)))),
			0f, 1f, PanelRiseDuration);
	}

	/// <summary>位移用的缓动：3t²−2t³ 套两层，两端更平、中段更快</summary>
	private static float Ease(float t)
	{
		return SmoothStep(SmoothStep(t));
	}

	private static float SmoothStep(float t)
	{
		return t * t * (3f - 2f * t);
	}

	private void Build()
	{
		// 左边缘顶到屏幕边，纵向先摆到屏幕下方，随后由 RisePanel 升上来
		Vector2 viewport = GetViewport().GetVisibleRect().Size;
		_root = new Control
		{
			Position = new Vector2(PanelLeft, viewport.Y),
			// 面板只占左侧一块，其余区域的点击不该被它吞掉
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_root);

		TextureRect background = new()
		{
			Texture = GD.Load<Texture2D>(BackgroundTexturePath),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_root.AddChild(background);

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
		_root.AddChild(title);

		BuildCards(_root);
		BuildRockButton(_root);
	}

	private void BuildCards(Control root)
	{
		_cardScene = GD.Load<PackedScene>(CardScenePath);
		if (_cardScene == null)
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
			SeedPacketLarger card = _cardScene.Instantiate<SeedPacketLarger>();
			card.SeedScene = PlantTypes.Instance.GetScene(type); // 必须在入树前设好，_Ready 要用
			card.BIsSelectionPreview = true; // 面板里的卡只做展示，不按阳光数压暗
			card.Position = new Vector2(
				originX + index % Columns * (CardSize.X + CardGap),
				GridTop + index / Columns * (CardSize.Y + CardGap));
			root.AddChild(card);

			// 卡片自带的 OnInputEvent 只服务种子栏，这里另接一个用于面板选择
			ConnectCardInput(card.GetNode<Area2D>("Area2D"), type);
			_cards.Add(card);
			index++;
		}

		// 网格按整行补满：剩下的格子放空槽，露出白色卡槽底
		int totalSlots = Mathf.CeilToInt(_selection.Available.Count / (float)Columns) * Columns;
		for (int i = _selection.Available.Count; i < totalSlots; i++)
		{
			SeedPacketLarger slot = _cardScene.Instantiate<SeedPacketLarger>();
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

	private async void OnCardClicked(PlantTypeEnum type)
	{
		// 面板里点已选中的卡什么都不做：放回要靠点卡槽
		if (_selection.IsSelected(type))
		{
			return;
		}

		List<PlantTypeEnum> oldOrder = SnapshotSelected();
		if (!_selection.Toggle(type))
		{
			return;
		}

		_tapSound.Play();

		// 选中立刻压暗，不等飞行结束——这层暗色是"已经选过了"的标记
		Refresh();

		(PlantTypeEnum, Vector2, Vector2)? lifted = null;
		if (TryGetSlotPosition(_selection.SelectedCount - 1, out Vector2 slot))
		{
			lifted = (type, CardScreenPosition(type), slot);
		}
		await CommitSelection(oldOrder, lifted);
	}

	/// <summary>点卡槽：把这张卡收回面板</summary>
	private async void OnSlotClicked(SeedPacketLarger packet)
	{
		List<SeedPacketLarger> packets = SeedBank.Instance?.GetSeedPackets();
		int slotIndex = packets?.IndexOf(packet) ?? -1;
		if (slotIndex < 0 || slotIndex >= _selection.Selected.Count)
		{
			return;
		}

		// 起点要在 Toggle 之前取：那之后这张卡就不在已选列表里，槽位序号也变了
		if (!TryGetSlotPosition(slotIndex, out Vector2 origin))
		{
			return;
		}

		List<PlantTypeEnum> oldOrder = SnapshotSelected();
		PlantTypeEnum type = oldOrder[slotIndex];
		if (!_selection.Toggle(type))
		{
			return;
		}

		_tapSound.Play();

		// 收回途中面板上这一张要保持"已选中"的暗色，落地才亮
		Refresh(type);
		await CommitSelection(oldOrder, (type, origin, CardScreenPosition(type)));
		Refresh();
	}

	/// <summary>
	/// 把选卡结果落到种子栏，并把"卡在槽位之间移动"补成动画。
	///
	/// 种子栏的内容是按下标重刷的：位次一变，那个位置上的植物就被原地换成另一张。
	/// 所以先写新顺序，再对每张位次变了的卡放一个飞行体，从旧槽飞到新槽盖住换脸那一下。
	/// </summary>
	/// <param name="oldOrder">变更前的已选顺序</param>
	/// <param name="lifted">往返于面板与种子栏的那张卡（放入 / 收回）；没有就传 null</param>
	private async Task CommitSelection(
		IReadOnlyList<PlantTypeEnum> oldOrder,
		(PlantTypeEnum Type, Vector2 From, Vector2 To)? lifted)
	{
		IReadOnlyList<PlantTypeEnum> newOrder = _selection.Selected;
		SeedBank.Instance?.ApplySeedSelection(newOrder);

		List<SeedPacketLarger> packets = SeedBank.Instance?.GetSeedPackets();
		if (packets == null)
		{
			return;
		}

		List<Task> flights = new();

		// 位次变了的：目标槽先遮住，让飞行体从旧槽滑过来，落地再露
		for (int i = 0; i < newOrder.Count && i < packets.Count; i++)
		{
			int from = IndexOf(oldOrder, newOrder[i]);
			if (from < 0 || from == i)
			{
				continue;
			}
			if (!TryGetSlotPosition(packets, from, out Vector2 fromPos)
				|| !TryGetSlotPosition(packets, i, out Vector2 toPos))
			{
				continue;
			}
			SeedPacketLarger slot = packets[i];
			slot.Visible = false;
			flights.Add(FlyCard(newOrder[i], fromPos, toPos, SlotShiftDuration, () => slot.Visible = true));
		}

		if (lifted is { } lift)
		{
			int index = IndexOf(newOrder, lift.Type);
			SeedPacketLarger slot = index >= 0 && index < packets.Count ? packets[index] : null;
			// 只收起卡面，白色卡槽底留着——整块藏掉的话，卡还没飞到槽位就空了
			slot?.SetCardFaceHidden(true);
			flights.Add(FlyCard(lift.Type, lift.From, lift.To, CardFlyDuration,
				() => slot?.SetCardFaceHidden(false)));
		}

		await Task.WhenAll(flights);
	}

	/// <summary>已选顺序的副本。Toggle 会改动原列表，比对得留着变更前的样子</summary>
	private List<PlantTypeEnum> SnapshotSelected() => new(_selection.Selected);

	/// <summary>type 在列表中的位置，不在里面返回 -1</summary>
	private static int IndexOf(IReadOnlyList<PlantTypeEnum> list, PlantTypeEnum type)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == type)
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>type 在面板里的那一格</summary>
	private int IndexInAvailable(PlantTypeEnum type) => IndexOf(_selection.Available, type);

	/// <summary>面板里这张卡当前在屏幕上的位置。面板可能在升起途中，所以每次现算</summary>
	private Vector2 CardScreenPosition(PlantTypeEnum type)
	{
		int index = IndexInAvailable(type);
		if (index < 0 || index >= _cards.Count)
		{
			return _root.Position;
		}
		return _root.Position + _cards[index].Position;
	}

	/// <summary>取卡槽在屏幕上的位置。卡槽此刻多半是空的，位置照旧有效</summary>
	private static bool TryGetSlotPosition(List<SeedPacketLarger> packets, int slotIndex, out Vector2 position)
	{
		position = Vector2.Zero;
		if (packets == null || slotIndex < 0 || slotIndex >= packets.Count)
		{
			return false;
		}
		position = packets[slotIndex].GlobalPosition;
		return true;
	}

	private static bool TryGetSlotPosition(int slotIndex, out Vector2 position)
	{
		return TryGetSlotPosition(SeedBank.Instance?.GetSeedPackets(), slotIndex, out position);
	}

	/// <summary>
	/// 临时实例一张卡从 from 飞到 to，飞完丢掉；onLanded 是落地那一刻的收尾，
	/// 用来把被它盖住的真实槽位露出来。
	/// 飞行卡挂在面板这一层、用屏幕坐标，所以面板自身的升降不影响它。
	/// </summary>
	private async Task FlyCard(PlantTypeEnum type, Vector2 from, Vector2 to, float duration, Action onLanded = null)
	{
		if (_cardScene == null)
		{
			return;
		}

		SeedPacketLarger flyer = _cardScene.Instantiate<SeedPacketLarger>();
		flyer.SeedScene = PlantTypes.Instance.GetScene(type); // 入树前设好，_Ready 要用
		flyer.BIsSelectionPreview = true; // 飞行中的卡同样只做展示
		flyer.Position = from;
		AddChild(flyer);

		Tween tween = CreateTween();
		tween.TweenMethod(
			Callable.From<float>(t => flyer.Position = from.Lerp(to, Ease(t))),
			0f, 1f, duration);
		await ToSignal(tween, Tween.SignalName.Finished);

		flyer.QueueFree();

		onLanded?.Invoke();
	}

	/// <summary>刷新提示条与各卡的明暗</summary>
	/// <param name="frozen">这次不动这张卡的明暗。收回途中它要保持"已选中"的暗色</param>
	private void Refresh(PlantTypeEnum? frozen = null)
	{
		// 已选中的卡压暗：用的就是局内"阳光不足"那一层 CostColorRect，
		// 卡片场景里它是 alpha 0.5 的黑。选卡阶段 _Process 不跑，不会自动改回来
		IReadOnlyList<PlantTypeEnum> available = _selection.Available;
		for (int i = 0; i < _cards.Count && i < available.Count; i++)
		{
			PlantTypeEnum type = available[i];
			if (type == frozen)
			{
				continue;
			}
			_cards[i].GetNode<ColorRect>("CostColorRect").Visible = _selection.IsSelected(type);
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
