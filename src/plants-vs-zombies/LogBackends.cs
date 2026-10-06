using System;
using System.IO;
using System.Text;
using Godot;

/// <summary> 走引擎通道：普通等级进 stdout，Warn / Error 交给警告与错误通道，编辑器里能进 Errors 面板 </summary>
public sealed class GodotLogBackend : ILogBackend
{
    /// <summary> 警告与错误通道自己会打标签 </summary>
    public bool AddsLevelLabel(LogLevel level) => level >= LogLevel.Warn;

    public void Write(LogLevel level, string message)
    {
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

    public void Flush()
    {
    }
}

/// <summary> 控制台上色的策略 </summary>
public enum LogColorMode
{
    /// <summary> 输出接在终端上才上色。重定向到文件或管道时转义码只会把日志弄脏，所以只写纯文本 </summary>
    Auto = 0,

    /// <summary> 一律上色，给认得转义码的查看器看 </summary>
    Always = 1,

    /// <summary> 一律不上色 </summary>
    Never = 2,
}

/// <summary>
/// 纯托管的 stdout / stderr。省掉引擎那一趟：不再把字符串转成 Variant、不再在引擎堆上
/// 分配一份副本。代价是脱离引擎那套通道——编辑器面板、断点、崩溃回执都收不到。
///
/// 收不到 Errors 面板的高亮，就在终端里自己上：Warn 黄、Error 红。
/// </summary>
public sealed class ConsoleLogBackend : ILogBackend
{
    /// <summary>
    /// 上色策略。挂静态是为了让命令行能在后端被造出来之前就先把它定下来。
    /// </summary>
    public static LogColorMode ColorMode = LogColorMode.Auto;

    /// <summary> ANSI 转义序列的引导字符。写成字符码是为了源码里不出现看不见的控制字符 </summary>
    private const char Esc = (char)27;

    private static readonly string WarnColor = Esc + "[33m";
    private static readonly string ErrorColor = Esc + "[31m";
    private static readonly string ResetColor = Esc + "[0m";
    private static readonly string FrameColor = Esc + "[90m";

    public bool AddsLevelLabel(LogLevel level) => false;

    public void Write(LogLevel level, string message)
    {
        // 分开走两条流：重定向到文件时，错误还得能单独捞出来
        if (level >= LogLevel.Warn)
        {
            Console.Error.WriteLine(Paint(level, message, Console.IsErrorRedirected));
        }
        else
        {
            Console.Out.WriteLine(message);
        }
    }

    public void Flush()
    {
        Console.Out.Flush();
        Console.Error.Flush();
    }

    private static string Paint(LogLevel level, string message, bool redirected)
    {
        bool colorless = ColorMode == LogColorMode.Never
            || (ColorMode == LogColorMode.Auto && redirected);

        if (colorless)
        {
            return message;
        }

        string bodyColor = level >= LogLevel.Error ? ErrorColor : WarnColor;
        string[] lines = message.Split('\n');
        StringBuilder builder = new(message.Length + lines.Length * 12);

        // 逐行上色而不是整段一个颜色：栈帧只是线索，跟错误本身用同一个红会喧宾夺主。
        // 认栈帧靠行首前缀，生成那几行的格式见 Log.StackFrameLinePrefix
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            string color = lines[i].StartsWith(Log.StackFrameLinePrefix, StringComparison.Ordinal)
                ? FrameColor
                : bodyColor;

            builder.Append(color).Append(lines[i]).Append(ResetColor);
        }

        return builder.ToString();
    }
}

/// <summary> 丢掉。比 <see cref="Log.Disable"/> 更靠前，连等级判断之外的事都不用做 </summary>
public sealed class NullLogBackend : ILogBackend
{
    public static readonly NullLogBackend Instance = new();

    public bool AddsLevelLabel(LogLevel level) => false;

    public void Write(LogLevel level, string message)
    {
    }

    public void Flush()
    {
    }
}

/// <summary> 同时往多个后端写。想要"引擎里看一份、文件里留一份"就用这一层把两边叠起来 </summary>
public sealed class TeeLogBackend : ILogBackend
{
    private readonly ILogBackend[] _backends;

    public TeeLogBackend(params ILogBackend[] backends)
    {
        _backends = backends ?? Array.Empty<ILogBackend>();
    }

    /// <summary> 只要有一个后端会打标签，这条消息就带着标签出去了，其余后端别再加 </summary>
    public bool AddsLevelLabel(LogLevel level)
    {
        foreach (ILogBackend backend in _backends)
        {
            if (backend.AddsLevelLabel(level))
            {
                return true;
            }
        }
        return false;
    }

    public void Write(LogLevel level, string message)
    {
        foreach (ILogBackend backend in _backends)
        {
            backend.Write(level, message);
        }
    }

    public void Flush()
    {
        foreach (ILogBackend backend in _backends)
        {
            backend.Flush();
        }
    }
}

/// <summary>
/// 写文件。导出之后没有编辑器面板，日志只能靠文件带走。
/// 路径带 <c>user://</c> / <c>res://</c> 前缀的走引擎解析，其余按原样当文件路径。
/// 默认追加：重启一次就抹掉上一次的，恰恰是排查时最不想要的行为。
/// </summary>
public sealed class FileLogBackend : ILogBackend, IDisposable
{
    private readonly StreamWriter _writer;

    public FileLogBackend(string path)
    {
        string resolved = ResolvePath(path);

        // StreamWriter 自己不会建目录，父目录不在就直接抛
        string directory = Path.GetDirectoryName(resolved);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 不带 BOM：日志文件常常被别的东西接过去读，多出来的三个字节会把首行弄脏
        _writer = new StreamWriter(resolved, append: true, encoding: new UTF8Encoding(false));
    }

    public bool AddsLevelLabel(LogLevel level) => false;

    public void Write(LogLevel level, string message)
    {
        _writer.WriteLine(message);
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    private static string ResolvePath(string path)
    {
        bool enginePath = path.StartsWith("user://", StringComparison.Ordinal)
            || path.StartsWith("res://", StringComparison.Ordinal);

        return enginePath ? ProjectSettings.GlobalizePath(path) : path;
    }
}

/// <summary>
/// 攒批：同等级的连续输出先拼成一段，攒够条数或者有人喊 <see cref="Log.Flush"/> 时才交给内层，
/// 一次把 N 条送过边界。省的不只是跨边界那一下，还有每条一次的消息组装——
/// 只有交出去的那一段才真正转成字符串。
///
/// 代价是崩溃时压在桶里的那几条会丢，所以阈值别开太大。
/// </summary>
public sealed class BufferedLogBackend : ILogBackend
{
    /// <summary> Trace 到 Error 各占一个桶；Off 不是输出等级，不占位 </summary>
    private static readonly int BucketCount = (int)LogLevel.Error + 1;

    private readonly ILogBackend _inner;
    private readonly int _threshold;
    private readonly StringBuilder[] _buckets;
    private int _pending;

    /// <param name="inner">攒好的日志往哪交</param>
    /// <param name="threshold">攒够多少条就自动吐一次</param>
    public BufferedLogBackend(ILogBackend inner, int threshold = 32)
    {
        _inner = inner;
        _threshold = Math.Max(1, threshold);
        _buckets = new StringBuilder[BucketCount];
        for (int i = 0; i < BucketCount; i++)
        {
            _buckets[i] = new StringBuilder(512);
        }
    }

    public bool AddsLevelLabel(LogLevel level) => _inner.AddsLevelLabel(level);

    public void Write(LogLevel level, string message)
    {
        // 按等级分桶，因为一次交出去的只能是一种等级——引擎通道里警告和普通输出走的是两条路
        StringBuilder bucket = _buckets[(int)level];
        if (bucket.Length > 0)
        {
            bucket.Append('\n');
        }
        bucket.Append(message);

        if (++_pending >= _threshold)
        {
            Flush();
        }
    }

    public void Flush()
    {
        if (_pending == 0)
        {
            return;
        }

        for (int level = 0; level < BucketCount; level++)
        {
            if (_buckets[level].Length == 0)
            {
                continue;
            }

            _inner.Write((LogLevel)level, _buckets[level].ToString());
            _buckets[level].Clear();
        }

        _inner.Flush();
        _pending = 0;
    }
}

/// <summary>
/// 按命令行写的规格搭后端。语法是用 <c>+</c> 连起来的后端名，前面加 <c>buffered:</c>
/// 表示给整条链再套一层攒批：
///
/// <code>
/// --log-backend=console                    托管 stdout（默认）
/// --log-backend=godot                      引擎通道
/// --log-backend=godot+file                 两处都写
/// --log-backend=file:user://logs/run.log   指定文件
/// --log-backend=buffered:godot+console     攒批之后再分发
/// --log-backend=null                       丢掉
/// </code>
///
/// 名字认不出、文件打不开都返回 null，同时自己把原因报出来，调用方只管别换上去。
/// </summary>
public static class LogBackendFactory
{
    /// <summary> 只写 <c>file</c> 不给路径时落在这里 </summary>
    public const string DefaultFilePath = "user://logs/pvz.log";

    private const string BufferedPrefix = "buffered:";
    private const string FilePrefix = "file:";

    public static ILogBackend Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            Log.Warn("[Log] 日志后端规格是空的");
            return null;
        }

        spec = spec.Trim();

        if (spec.StartsWith(BufferedPrefix, StringComparison.Ordinal))
        {
            ILogBackend inner = Parse(spec[BufferedPrefix.Length..]);
            return inner == null ? null : new BufferedLogBackend(inner);
        }

        string[] names = spec.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            Log.Warn("[Log] 日志后端规格里没有后端名：", spec);
            return null;
        }

        ILogBackend[] backends = new ILogBackend[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            backends[i] = ParseOne(names[i]);
            if (backends[i] == null)
            {
                return null;
            }
        }

        return backends.Length == 1 ? backends[0] : new TeeLogBackend(backends);
    }

    private static ILogBackend ParseOne(string name)
    {
        switch (name)
        {
            case "godot":
                return new GodotLogBackend();

            case "console":
                return new ConsoleLogBackend();

            case "null":
                return NullLogBackend.Instance;

            case "file":
                return OpenFile(DefaultFilePath);

            default:
                if (name.StartsWith(FilePrefix, StringComparison.Ordinal))
                {
                    string path = name[FilePrefix.Length..];
                    return path.Length == 0 ? Unknown(name) : OpenFile(path);
                }
                return Unknown(name);
        }
    }

    private static ILogBackend OpenFile(string path)
    {
        try
        {
            return new FileLogBackend(path);
        }
        catch (Exception exception)
        {
            Log.Error("[Log] 打不开日志文件 ", path, "：", exception.Message);
            return null;
        }
    }

    private static ILogBackend Unknown(string name)
    {
        Log.Warn("[Log] 认不出的日志后端：", name);
        return null;
    }
}
