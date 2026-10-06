using Godot;
using Godot.Collections;

namespace LogSettings
{
#if TOOLS
    /// <summary>
    /// 把日志那几个旋钮登记进项目设置面板，让它们带类型和取值范围，而不是一列裸字符串。
    ///
    /// 这件事只能由插件做：类型提示必须在编辑器加载项目的时候登记，而游戏里的 autoload
    /// 在编辑器下根本不实例化——读到这些设置的 Global 要到运行游戏才跑起来。
    ///
    /// 这里只管面板长什么样。值怎么用是运行时的事，键名两边必须一致。
    /// </summary>
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        /// <summary> 等级的取值，几个等级旋钮共用 </summary>
        private const string LevelOptions = "trace,debug,info,warn,error,off";

        /// <summary>
        /// 后端的取值。带上 file 的组合是"顺带留一份到文件"——路径那一项只在选了它们时才有用。
        /// 这里放的都是能一眼看懂的组合，更花哨的规格（比如叠加攒批）留给命令行。
        /// </summary>
        private const string BackendOptions = "godot,console,null,file,godot+file,console+file";

        /// <summary> 日志文件默认落这儿。下面的目录创建和它配套 </summary>
        private const string DefaultLogFile = "user://logs/pvz.log";

        public override void _EnterTree()
        {
            // 默认路径指着这个目录。它不在的话，浏览对话框一打开就是空的，
            // 跟"配置坏了"看着没区别——顺手建出来
            DirAccess.MakeDirRecursiveAbsolute("user://logs");

            Register("logging/level", Variant.Type.String, "debug", PropertyHint.Enum, LevelOptions);
            Register("logging/flush_level", Variant.Type.String, "warn", PropertyHint.Enum, LevelOptions);
            Register("logging/color", Variant.Type.String, "auto", PropertyHint.Enum, "auto,always,never");
            Register("logging/backend", Variant.Type.String, "console", PropertyHint.Enum, BackendOptions);
            Register("logging/file", Variant.Type.String, DefaultLogFile, PropertyHint.File, "*.log");
            Register("logging/stack_level", Variant.Type.String, "warn", PropertyHint.Enum, LevelOptions);
        }

        /// <summary>
        /// 键不在就补一个默认值——面板里得先有这一项，用户才知道有这么个东西可调。
        /// 已经在的值一律不动：那可能是用户自己改的，插件不该替他做决定。
        /// </summary>
        private static void Register(
            string name,
            Variant.Type type,
            Variant initial,
            PropertyHint hint = PropertyHint.None,
            string hintString = "")
        {
            if (!ProjectSettings.HasSetting(name))
            {
                ProjectSettings.SetSetting(name, initial);
                ProjectSettings.SetInitialValue(name, initial);
            }

            ProjectSettings.AddPropertyInfo(new Dictionary
            {
                ["name"] = name,
                ["type"] = Variant.From(type),
                ["hint"] = Variant.From(hint),
                ["hint_string"] = hintString,
            });
        }
    }
#endif
}
