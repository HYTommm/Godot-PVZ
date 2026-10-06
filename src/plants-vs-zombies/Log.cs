using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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
/// 日志后端：一条日志最终落到哪里去。换掉它就能换出口——引擎通道、托管 stdout、
/// 文件、或者干脆丢掉，几种实现见 <see cref="LogBackendFactory"/>。
/// </summary>
public interface ILogBackend
{
	/// <summary>
	/// 这个等级的输出里是不是自带等级标记。引擎的警告与错误通道自己会打
	/// "WARNING: " / "ERROR: "，那种情况 Log 就不该再补一个前缀，否则同一句话说两遍。
	/// </summary>
	bool AddsLevelLabel(LogLevel level);

	/// <summary> 写一条。消息已经组装完毕，含前后缀，等级标记按 <see cref="AddsLevelLabel"/> 的结论决定加没加 </summary>
	void Write(LogLevel level, string message);

	/// <summary> 把攒着的吐出去。不做缓冲的后端是空实现 </summary>
	void Flush();
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
/// ## 六个旋钮
///
/// - 一键关闭：<see cref="Disable"/>()，或把 <see cref="MinLevel"/> 设成 <see cref="LogLevel.Off"/>
/// - 分等级：<see cref="MinLevel"/>，低于它的等级在写出去之前就被丢掉
/// - 前后缀：<see cref="Prefix"/> / <see cref="Suffix"/>，套在正文外面
/// - 出口：<see cref="Backend"/>，换掉它就把日志接到别处去
/// - 刷盘时机：<see cref="FlushLevel"/>，这个等级及以上不等缓冲攒满
/// - 错误栈帧：<see cref="ErrorStackTrace"/>，打 Error 时附上调用栈
///
/// ## 等级怎么选
///
/// - Trace：逐帧、逐发子弹、逐次命中的细粒度输出，默认不显示
/// - Debug：构造、初始化、状态迁移这类开发期关心的事件
/// - Info：一局里值得留痕的节点（关卡装载、选卡落定、游戏结束、工具结论）
/// - Warn：能继续跑但明显不对的状态
/// - Error：真正的失败
///
/// ## 性能
///
/// 参数在调用处就已经求值，日志不做延迟求值。所以逐帧路径上该有的守卫还是要有：
/// 先问 <see cref="IsOn"/>，再拼字符串。
///
/// 后端只管"这条字符串往哪写"，组装开销在它之前就付掉了。真正省下的是引擎那一段：
/// 走引擎通道要把字符串转成 Variant 再分配一次引擎侧内存，换托管出口就没这一步。
/// 想连每条一次的组装开销一起压下去，得让后端攒批——见 <see cref="BufferedLogBackend"/>。
/// </summary>
public static class Log
{
	/// <summary> 日志出口。换掉它等于换后端，旧的会被刷干净并释放 </summary>
	public static ILogBackend Backend
	{
		get => _backend;

		set
		{
			ILogBackend next = value ?? NullLogBackend.Instance;
			if (ReferenceEquals(_backend, next))
			{
				return;
			}

			Release(_backend);
			_backend = next;
		}
	}

	private static ILogBackend _backend = new ConsoleLogBackend();

	/// <summary> 总开关。关掉后任何等级都不输出 </summary>
	public static bool Enabled = true;

	/// <summary> 输出阈值：低于它的等级直接丢弃 </summary>
	public static LogLevel MinLevel = LogLevel.Debug;

	/// <summary>
	/// 这个等级及以上，写完立刻刷出去，不等缓冲攒满、也不等帧末。
	/// 攒批是为了省开销，但错误要是压在桶里等着凑数，进程一崩就没了——
	/// 而那条恰恰是最想看的。
	/// </summary>
	public static LogLevel FlushLevel = LogLevel.Warn;

	/// <summary>
	/// 打 Error 时附上调用栈。错误往往离现场很远，没有栈就只能靠猜是哪一处触发的。
	/// 抓栈不便宜，但错误本来就不该频繁出现。
	/// </summary>
	public static bool ErrorStackTrace = true;

	/// <summary>
	/// 栈帧行的前缀。生成栈帧的地方和渲染它的后端得用同一个值，
	/// 后端靠它把栈帧和正文分开——正文跟等级一个颜色，栈帧退到灰。
	/// </summary>
	public const string StackFrameLinePrefix = "   at ";

	/// <summary> 每条日志的前缀，套在等级标记之前 </summary>
	public static string Prefix = "";

	/// <summary> 每条日志的后缀，套在正文之后 </summary>
	public static string Suffix = "";

	/// <summary>
	/// 是否在正文前打出 [TRACE] 这样的等级标记。后端自带标记的等级不受它影响，
	/// 那种情况下标记由后端给。
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

	/// <summary>
	/// 把后端攒着的吐出去。带缓冲的后端不喊这一声就会压着不发，
	/// 所以它挂在每帧末尾和退出的最后一步上；不缓冲的后端这里是空转。
	/// </summary>
	public static void Flush()
	{
		_backend.Flush();
	}

	/// <summary> 退出前收尾：刷干净、放掉后端。放完换成丢弃，之后再来日志不会写进已释放的对象 </summary>
	public static void Shutdown()
	{
		Release(_backend);
		_backend = NullLogBackend.Instance;
	}

	private static void Release(ILogBackend backend)
	{
		backend.Flush();
		if (backend is IDisposable disposable)
		{
			disposable.Dispose();
		}
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

		// 后端自己会打标记的等级就别替它操心，两边都加就是同一句话说两遍
		string message = Compose(tag, parts, ShowLevelTag && !_backend.AddsLevelLabel(level));

		if (ErrorStackTrace && level == LogLevel.Error)
		{
			message += StackSuffix();
		}

		_backend.Write(level, message);

		// 严重的等级当场落地，不给缓冲留吞掉它的机会
		if (level >= FlushLevel)
		{
			_backend.Flush();
		}
	}

	/// <summary>
	/// 当前调用栈，每帧一行。跳过 StackSuffix、Write、Error 这三帧 Log 自己的，
	/// 报出来的头一行就用得着——是真正写下那句 Log.Error 的地方，而不是日志系统内部。
	///
	/// 方法名总有，文件行号要看有没有调试符号：导出后的发布版通常没有，
	/// 那就只剩方法和类型，够定位到函数。
	/// </summary>
	private static string StackSuffix()
	{
		StackTrace stack = new(skipFrames: 3, fNeedFileInfo: true);
		StringBuilder builder = new(256);

		for (int i = 0; i < stack.FrameCount; i++)
		{
			StackFrame frame = stack.GetFrame(i);
			MethodBase method = frame?.GetMethod();
			if (method == null)
			{
				continue;
			}

			builder.Append('\n').Append(StackFrameLinePrefix)
				.Append(method.DeclaringType?.FullName).Append('.').Append(method.Name);

			string file = frame.GetFileName();
			if (file != null)
			{
				builder.Append(" (").Append(file).Append(':').Append(frame.GetFileLineNumber()).Append(')');
			}
		}

		return builder.ToString();
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
