using System.Text;
using Godot;

/// <summary> 日志等级。数值即优先级，阈值比较直接比大小 </summary>
public enum LogLevel
{
	/// <summary> 逐帧、逐子弹、逐次命中这一级的细粒度输出 </summary>
	Trace = 0,

	Debug = 1,
	Info = 2,
	Warn = 3,
	Error = 4,

	/// <summary> 它不是一种输出，只是给阈值用的"比谁都大"：MinLevel 设成它等于全关 </summary>
	Off = 5,
}

/// <summary>
/// 全局日志出口。全项目不再直接调 GD.Print / GD.PrintErr，一律走这里。
///
/// ## 用法
///
/// <code>
/// Log.Info("[MainGame] 关卡 ", Level.LevelId, " 开始");
/// Log.Warn("卡槽数量对不上，多余的卡丢弃");
/// </code>
///
/// 参数是 params object[]，和 GD.Print 一样往下排就行，不必自己拼字符串，
/// 也不要求调用方带等级前缀——前缀由 <see cref="Prefix"/> 统一管。
///
/// ## 三个旋钮
///
/// - 一键关闭：<see cref="Disable"/>()，或把 <see cref="MinLevel"/> 设成 <see cref="LogLevel.Off"/>
/// - 分等级：<see cref="MinLevel"/>，低于它的等级在写出去之前就被丢掉
/// - 前后缀：<see cref="Prefix"/> / <see cref="Suffix"/>，套在正文外面
///
/// ## 等级怎么选
///
/// - Trace：逐帧、逐发子弹、逐次命中的细粒度输出，默认不显示
/// - Debug：构造、初始化、状态迁移这类开发期关心的事件
/// - Info：一局里值得留痕的节点（关卡装载、选卡落定、游戏结束、工具结论）
/// - Warn：能继续跑但明显不对的状态
/// - Error：真正的失败
///
/// 等级同时决定走哪条出口：Warn / Error 交给引擎的警告与错误通道
/// （编辑器里进 Errors 面板、导出后进 stderr），其余走 stdout。
///
/// ## 性能
///
/// 参数在调用处就已经求值，日志不做延迟求值。所以逐帧路径上该有的守卫还是要有：
/// 先问 <see cref="IsOn"/>，再拼字符串。
/// </summary>
public static class Log
{
	/// <summary> 总开关。关掉后任何等级都不输出 </summary>
	public static bool Enabled = true;

	/// <summary> 输出阈值：低于它的等级直接丢弃 </summary>
	public static LogLevel MinLevel = LogLevel.Debug;

	/// <summary> 每条日志的前缀，套在等级标记之前 </summary>
	public static string Prefix = "";

	/// <summary> 每条日志的后缀，套在正文之后 </summary>
	public static string Suffix = "";

	/// <summary>
	/// 是否在正文前打出 [TRACE] 这样的等级标记。
	/// Warn / Error 不受它影响：那两个走引擎通道，前缀由引擎给。
	/// </summary>
	public static bool ShowLevelTag = true;

	/// <summary> 这个等级当前会不会输出。高频路径上先问它，再决定要不要拼消息 </summary>
	public static bool IsOn(LogLevel level)
	{
		return Enabled && level >= MinLevel;
	}

	/// <summary> 一键关掉全部日志 </summary>
	public static void Disable()
	{
		Enabled = false;
	}

	/// <summary> 恢复输出，仍然受 <see cref="MinLevel"/> 限制 </summary>
	public static void Enable()
	{
		Enabled = true;
	}

	public static void Trace(params object[] parts)
	{
		Write(LogLevel.Trace, "TRACE", parts);
	}

	public static void Debug(params object[] parts)
	{
		Write(LogLevel.Debug, "DEBUG", parts);
	}

	public static void Info(params object[] parts)
	{
		Write(LogLevel.Info, "INFO", parts);
	}

	public static void Warn(params object[] parts)
	{
		Write(LogLevel.Warn, "WARN", parts);
	}

	public static void Error(params object[] parts)
	{
		Write(LogLevel.Error, "ERROR", parts);
	}

	private static void Write(LogLevel level, string tag, object[] parts)
	{
		if (!IsOn(level))
		{
			return;
		}

		// Warn / Error 走的是引擎的警告与错误通道，它会自己打上 "WARNING: " / "ERROR: "，
		// 再补一个 [WARN] 就是同一句话说两遍
		bool engineLabels = level >= LogLevel.Warn;
		string message = Compose(tag, parts, ShowLevelTag && !engineLabels);

		switch (level)
		{
			case LogLevel.Error:
				GD.PushError(message);
				break;

			case LogLevel.Warn:
				GD.PushWarning(message);
				break;

			default:
				GD.Print(message);
				break;
		}
	}

	private static string Compose(string tag, object[] parts, bool showTag)
	{
		StringBuilder builder = new(64);

		builder.Append(Prefix);
		if (showTag)
		{
			builder.Append('[').Append(tag).Append("] ");
		}
		foreach (object part in parts)
		{
			builder.Append(part?.ToString() ?? "<null>");
		}
		builder.Append(Suffix);

		return builder.ToString();
	}
}
