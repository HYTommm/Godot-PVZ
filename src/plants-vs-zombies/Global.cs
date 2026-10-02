using Godot;

public partial class Global : Node
{
    public static Global Instance => _instance;

    private static Global _instance;

    /// <summary> 关卡索引，首次访问时才从 res://MainGame/Levels/LevelList.tres 加载 </summary>
    private LevelList _levelList;

    /// <summary>
    /// 当前要玩哪一关。由选关界面（或"单击 Adventure"的 1-1 默认值）设置，
    /// MainGame 在 _Ready 里读取它来装配自己。
    /// </summary>
    public LevelData CurrentLevelData { get; set; }

    /// <summary>左上角的帧率标签。导出后没有编辑器的调试信息，靠它看性能</summary>
    private Label _fpsLabel;

    public override void _Ready()
    {
        _instance = this;

        // headless 跑生成器/验证器时不需要 UI
        if (DisplayServer.GetName() != "headless")
        {
            AddFpsLabel();
        }

        // 关卡数据工具：只在命令行显式要求时运行，正常游戏完全不受影响。
        // 参数要用 "--" 传给应用本身（否则会被 Godot 自己解析）：
        //   godot --headless --quit --path <项目目录> -- --generate-levels
        //   godot --headless --quit --path <项目目录> -- --verify-levels
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            switch (argument)
            {
                case "--generate-levels":
                    LevelDataGenerator.GenerateAll();
                    break;

                case "--verify-levels":
                    LevelDataVerifier.VerifyAll();
                    break;
            }
        }
    }

    /// <summary>FPS 标签的刷新间隔累计。几帧刷一次就够，见 _Process 里的说明</summary>
    private double _fpsRefreshTimer;

    public override void _Process(double delta)
    {
        if (_fpsLabel == null)
        {
            return;
        }

        // 不要每帧刷：Label.Text 一改就要重新排版文本（TextServer 走一遍 shaping），
        // 那是实打实的开销，而 Engine.GetFramesPerSecond() 本来就是秒级平均值，
        // 每帧设一次等于白花几百次排版换一个不动的数字。四分之一秒刷一次足够跟手
        _fpsRefreshTimer += delta;
        if (_fpsRefreshTimer < 0.25)
        {
            return;
        }
        _fpsRefreshTimer = 0.0;

        // 子弹数只在关卡里才有意义，主菜单时那一位不显示。
        // 它是数据集合里的活跃条数，调子弹性能时比单看帧率有用
        MainGame game = MainGame.Instance;

        // 排查泄漏用：孤儿节点就是"建出来却没挂到树上"的节点，每个都带着一个 CanvasItem RID。
        // 这个数一直涨，就是 RID 泄漏的源头。按 F9 能把它们的类型打到控制台。
        // 定位完就把这一整段连同 _Input 里的热键删掉
        int nodes = (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        int orphans = (int)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        string tail = $"   节点 {nodes}   孤儿 {orphans}";
        _fpsLabel.Text = game == null
            ? $"FPS {Engine.GetFramesPerSecond()}{tail}"
            : $"FPS {Engine.GetFramesPerSecond()}   子弹 {game.Bullets.Count}{tail}";
    }

    /// <summary>
    /// 排查孤儿节点用：按 F9 把当前所有孤儿节点的类型打到控制台。
    /// 和标签上那个"孤儿"计数配套——计数告诉你有没有在漏，这个告诉你漏的是谁。
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.F9 })
        {
            Node.PrintOrphanNodes();
        }
    }

    /// <summary>
    /// 纯代码建一个帧率标签，不占场景文件。挂在最上层的 CanvasLayer 上，
    /// 所以选卡面板、过场这些也盖不住它。
    /// </summary>
    private void AddFpsLabel()
    {
        CanvasLayer layer = new() { Layer = 128 };
        AddChild(layer);

        _fpsLabel = new Label
        {
            Text = "FPS --",
            Position = new Vector2(8, 4),
        };
        // 草地底色浅，不加描边白字看不清
        _fpsLabel.AddThemeFontSizeOverride("font_size", 20);
        _fpsLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _fpsLabel.AddThemeConstantOverride("outline_size", 5);

        layer.AddChild(_fpsLabel);
    }

    /// <summary> 取关卡索引；加载失败返回 null </summary>
    public LevelList GetLevelList()
    {
        if (_levelList == null)
        {
            _levelList = ResourceLoader.Load<LevelList>("res://MainGame/Levels/LevelList.tres");
            if (_levelList == null)
            {
                GD.PrintErr("[Global] 加载 res://MainGame/Levels/LevelList.tres 失败");
            }
        }
        return _levelList;
    }

    /// <summary> 取某关，world / index 均 1-based；取不到返回 null </summary>
    public LevelData GetLevel(int world, int index)
    {
        return GetLevelList()?.GetLevel(world, index);
    }

    /// <summary> 按 LevelId 取关；取不到返回 null </summary>
    public LevelData GetLevelById(string levelId)
    {
        return GetLevelList()?.GetLevelById(levelId);
    }

}
