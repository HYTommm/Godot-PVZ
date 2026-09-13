using Godot;
using static ResourceDB.Sounds.Bgm;

public partial class MainMenu_SelectorScreen : MainNode2D
{
	// 声明AudioStreamPlayer节点
	private Scene _mainMenuScene;
	private PackedScene _mainGameScene;
	private ZombieHand _zombieHand;
	private ColorRect _colorRect;

	/// <summary> 单击开局时选定的关卡，等过场动画播完再切场景 </summary>
	private LevelData _pendingLevel;

	/// <summary> 过场动画是否已经在播，动画期间再点一下要忽略掉 </summary>
	private bool _starting;

	/// <summary> 选关界面（长按 Adventure 时打开）。在 _Ready 里建出来挂到本节点下，不动 .tscn </summary>
	public LevelSelectScreen LevelSelect { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_mainMenuScene = new MainMenuScene(Global.Instance);// 设置场景
		//_mainMenuScene.PlayMainGameBgm(); // 播放BGM
		//_mainMenuScene.TurnToNormalBgm();

		// 按名字找而不是写死节点路径：这两个名字在菜单场景里唯一，而本节点不一定是场景根。
		// FindChild 默认只认有 owner 的节点，正好排除运行时新建的那些（比如选关界面的遮罩）。
		_zombieHand = FindChild("ZombieHand") as ZombieHand;
		_colorRect = FindChild("ColorRect") as ColorRect;
		_mainGameScene = ResourceLoader.Load<PackedScene>("res://MainGame/MainGame.tscn");

		// AnimEnd 只在这里订阅一次。放在按钮脚本里的话两个按钮会各订一次，
		// 而第一次 ChangeSceneToPacked 就把旧场景连同第二个订阅者一起销毁了。
		if (_zombieHand != null)
		{
			_zombieHand.AnimEnd += StartGame;
		}
		else
		{
			GD.PrintErr("[MainMenu_SelectorScreen] 找不到 ZombieHand / ColorRect，单击开局将没有过场动画");
		}

		LevelSelect = new LevelSelectScreen();
		AddChild(LevelSelect);
	}

	public void StopBgm()
	{
		_mainMenuScene.StopAllBgm();
	}

	/// <summary> 打开选关界面 </summary>
	public void OpenLevelSelect()
	{
		LevelSelect?.Open();
	}

	/// <summary> 单击 Adventure：播过场动画，动画放完由 StartGame 切场景 </summary>
	public void StartAdventure(LevelData level)
	{
		if (_starting)
		{
			return;
		}
		if (_zombieHand == null || _colorRect == null)
		{
			EnterLevel(level);
			return;
		}

		_starting = true;
		_pendingLevel = level;
		StopBgm();
		_colorRect.Visible = true;
		_zombieHand.Play();
	}

	/// <summary> 直接进关卡，不播过场动画（选关界面走这条） </summary>
	public void EnterLevel(LevelData level)
	{
		if (Global.Instance != null)
		{
			Global.Instance.CurrentLevelData = level;
		}
		if (_mainGameScene == null)
		{
			GD.PrintErr("[MainMenu_SelectorScreen] MainGame.tscn 加载失败");
			return;
		}
		GetTree().ChangeSceneToPacked(_mainGameScene);
	}

	private void StartGame()
	{
		EnterLevel(_pendingLevel);
	}
}
