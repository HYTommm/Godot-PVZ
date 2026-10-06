using System;
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

        // 放在关卡工具之前：那些工具的输出本身就是日志，等级得先定下来。
        // 项目设置先落地，命令行再盖一层——临时调试只动命令行，不碰项目文件
        ApplyLogProjectSettings();
        ApplyLogCommandLineArgs();

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

                case "--bench-log":
                    Log.Info("[Global] 日志基准结果写到 ", LogBench.Run());
                    break;
            }
        }
    }

    /// <summary>
    /// 日志的旋钮也能在项目设置里调（编辑器里那一节叫 <c>logging/</c>），改完即生效，
    /// 不用重新编译。值跟命令行认的是同一套写法，两边改哪边都行。
    ///
    /// 键不存在、或者值拼错，都留着原来的值不动——项目设置是用来微调的，
    /// 写错一个词不该让日志整个哑掉。
    /// </summary>
    private static void ApplyLogProjectSettings()
    {
        Log.MinLevel = ReadLogLevel("logging/level", Log.MinLevel);
        Log.FlushLevel = ReadLogLevel("logging/flush_level", Log.FlushLevel);
        Log.StackTraceLevel = ReadLogLevel("logging/stack_level", Log.StackTraceLevel);

        string color = ProjectSettings.GetSetting("logging/color", "").AsString();
        if (color.Length > 0)
        {
            if (Enum.TryParse(color, ignoreCase: true, out LogColorMode mode))
            {
                ConsoleLogBackend.ColorMode = mode;
            }
            else
            {
                Log.Warn("[Global] 项目设置 logging/color 认不出的配色策略：", color);
            }
        }

        // 空串表示没配，别去动 Log 里那个默认后端
        string spec = ProjectSettings.GetSetting("logging/backend", "").AsString();
        if (spec.Length > 0)
        {
            // 面板上的后端是下拉，填不下路径，所以路径另开一项，到这里才拼起来
            string filePath = ProjectSettings.GetSetting("logging/file", "").AsString();
            if (filePath.Length > 0)
            {
                spec = AttachLogFilePath(spec, filePath);
            }

            ILogBackend backend = LogBackendFactory.Parse(spec);
            if (backend != null)
            {
                Log.Backend = backend;
            }
        }
    }

    /// <summary>
    /// 把规格里裸的 <c>file</c> 换成带路径的写法。只有面板那条路需要它：
    /// 命令行本来就写得下完整规格，走不到这里。
    /// </summary>
    private static string AttachLogFilePath(string spec, string path)
    {
        string[] names = spec.Split('+');
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == "file")
            {
                names[i] = "file:" + path;
            }
        }

        return string.Join('+', names);
    }

    /// <summary> 从项目设置里读一个等级。读不出来就用传进来的那个，同时报一声——静默失效最难查 </summary>
    private static LogLevel ReadLogLevel(string key, LogLevel fallback)
    {
        string value = ProjectSettings.GetSetting(key, "").AsString();
        if (value.Length == 0)
        {
            return fallback;
        }

        if (Enum.TryParse(value, ignoreCase: true, out LogLevel level))
        {
            return level;
        }

        Log.Warn("[Global] 项目设置 ", key, " 认不出的等级：", value);
        return fallback;
    }

    /// <summary>
    /// 日志等级和出口都能从命令行给，导出后不重新编译就能调：
    /// <code>
    /// --quiet                                    全部关掉
    /// --log-level=trace|debug|info|warn|error|off
    /// --log-backend=godot|console|null|file[:路径]|buffered:...
    /// --log-color=auto|always|never
    /// --log-stack=trace|debug|info|warn|error|off
    /// </code>
    /// 等级的三个参数各指一个阈值，同时给时后写的生效。不给就保持 Log 里的默认值。
    /// 后端规格的语法见 <see cref="LogBackendFactory.Parse"/>；
    /// 配色只对 <see cref="ConsoleLogBackend"/> 有意义，别的后端收了也不算数。
    /// </summary>
    private static void ApplyLogCommandLineArgs()
    {
        const string logLevelArg = "--log-level=";
        const string logBackendArg = "--log-backend=";
        const string logColorArg = "--log-color=";
        const string logStackArg = "--log-stack=";

        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--quiet")
            {
                Log.MinLevel = LogLevel.Off;
            }
            else if (argument.StartsWith(logLevelArg, StringComparison.Ordinal))
            {
                string value = argument[logLevelArg.Length..];
                if (Enum.TryParse(value, ignoreCase: true, out LogLevel level))
                {
                    Log.MinLevel = level;
                }
                else
                {
                    Log.Warn("[Global] 认不出的日志等级：", value);
                }
            }
            else if (argument.StartsWith(logBackendArg, StringComparison.Ordinal))
            {
                // 认不出的规格已经在工厂里报过，这里直接留着原来的后端
                ILogBackend backend = LogBackendFactory.Parse(argument[logBackendArg.Length..]);
                if (backend != null)
                {
                    Log.Backend = backend;
                }
            }
            else if (argument.StartsWith(logColorArg, StringComparison.Ordinal))
            {
                string value = argument[logColorArg.Length..];
                if (Enum.TryParse(value, ignoreCase: true, out LogColorMode mode))
                {
                    ConsoleLogBackend.ColorMode = mode;
                }
                else
                {
                    Log.Warn("[Global] 认不出的日志配色策略：", value);
                }
            }
            else if (argument.StartsWith(logStackArg, StringComparison.Ordinal))
            {
                string value = argument[logStackArg.Length..];
                if (Enum.TryParse(value, ignoreCase: true, out LogLevel stackLevel))
                {
                    Log.StackTraceLevel = stackLevel;
                }
                else
                {
                    Log.Warn("[Global] 认不出的日志栈帧等级：", value);
                }
            }
        }
    }

    /// <summary>FPS 标签的刷新间隔累计。几帧刷一次就够，见 _Process 里的说明</summary>
    private double _fpsRefreshTimer;

    /// <summary>
    /// 每帧把日志放出去。带缓冲的后端靠这一下才不会压着不发；不做缓冲的后端这里是空转。
    /// 放在帧首，好让它跟 _fpsLabel 那道提前返回无关联。
    /// </summary>
    public override void _Process(double delta)
    {
        Log.Flush();

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
    /// 退出前把日志收干净。攒在桶里还没交出去的那几条、以及文件后端没落盘的缓冲，
    /// 不在这里处理掉就随进程一起没了——而它们往往正是崩之前最想看的那几条。
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationExitTree)
        {
            Log.Shutdown();
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
                Log.Error("[Global] 加载 res://MainGame/Levels/LevelList.tres 失败");
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
