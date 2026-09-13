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

    public override void _Ready()
    {
        _instance = this;

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
