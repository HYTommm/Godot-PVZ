using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Godot;

/// <summary>
/// 日志开销基准，命令行加 --bench-log 触发。
///
/// 结果写文件而不是打出来：被测量的正是各个后端的输出开销，
/// 把结果混进同一条流里，测到的就不只是日志了。跑的时候把 stdout 丢掉即可。
///
/// 逐帧路径上真正贵的是调用处的字符串拼接，所以 Trace 那几组才是重点——
/// 后端换得再快，也盖不住为一条会被丢弃的日志拼一次串。
/// </summary>
public static class LogBench
{
    /// <summary> 每组跑多少轮，取最小值 </summary>
    private const int Rounds = 3;

    /// <summary> 每轮跑多少次。太少噪声盖过差异，太多要等 </summary>
    private const int Iterations = 50_000;

    /// <summary> Warn 那几组的次数 </summary>
    private const int WarnIterations = 20_000;

    /// <summary> Error 那几组：默认要抓调用栈，跑满五万次纯属浪费 </summary>
    private const int ErrorIterations = 3_000;

    /// <summary>
    /// 正式计时前先跑这么多下，让 JIT 和缓存热起来。
    /// 这个数不能小：一千次预热时，第一组（也是整个进程第一次大量调引擎 print 的那组）
    /// 会比稳定后高出近一倍——那是预热不足，不是它真的慢。
    /// </summary>
    private const int Warmup = 20_000;

    /// <summary> 跑完整套，返回结果文件的路径 </summary>
    public static string Run()
    {
        LogLevel savedLevel = Log.MinLevel;
        ILogBackend savedBackend = Log.Backend;

        StringBuilder report = new();
        report.AppendLine($"每组 {Rounds} 轮 × {Iterations} 次取最小，预热 {Warmup} 次");
        report.AppendLine();

        try
        {
            // 放开到 Info，下面各组才真的走到后端；Trace 那几组靠这个阈值被丢弃
            Log.MinLevel = LogLevel.Info;

            // 两组都只拼一次串，差别在谁来拼：多参数那个重载拿 StringBuilder 自己拼，
            // 传字符串进去则直接命中单字符串重载。量的就是这个，跟跨语言参数转换无关
            Measure(report, "GD.Print 三个参数直调（旧写法）", i => GD.Print("[Bench] 第 ", i, " 条"));
            Measure(report, "GD.Print 先拼好再打印", i => GD.Print("[Bench] 第 " + i + " 条"));

            Log.Backend = new GodotLogBackend();
            Measure(report, "Log.Info 三个参数 + godot", i => Log.Info("[Bench] 第 ", i, " 条"));
            Measure(report, "Log.Info 先拼好 + godot", i => Log.Info("[Bench] 第 " + i + " 条"));

            Log.Backend = new ConsoleLogBackend();
            Measure(report, "Log.Info + console", i => Log.Info("[Bench] 第 ", i, " 条"));

            Log.Backend = new BufferedLogBackend(new GodotLogBackend());
            Measure(report, "Log.Info + buffered:godot", i => Log.Info("[Bench] 第 ", i, " 条"));

            Log.Backend = NullLogBackend.Instance;
            Measure(report, "Log.Info + null（只剩组装）", i => Log.Info("[Bench] 第 ", i, " 条"));

            report.AppendLine();
            report.AppendLine("Warn / Error——这两级都会当场刷盘，默认也都带调用栈");

            LogLevel savedStackTrace = Log.StackTraceLevel;

            Log.Backend = new GodotLogBackend();
            Measure(report, "Log.Warn + godot（带栈帧）", i => Log.Warn("[Bench] 第 ", i, " 条"), WarnIterations);
            Log.StackTraceLevel = LogLevel.Off;
            Measure(report, "Log.Warn + godot（不带栈帧）", i => Log.Warn("[Bench] 第 ", i, " 条"), WarnIterations);

            Log.Backend = new ConsoleLogBackend();
            Log.StackTraceLevel = LogLevel.Warn;
            Measure(report, "Log.Warn + console（带栈帧）", i => Log.Warn("[Bench] 第 ", i, " 条"), WarnIterations);
            Log.StackTraceLevel = LogLevel.Off;
            Measure(report, "Log.Warn + console（不带栈帧）", i => Log.Warn("[Bench] 第 ", i, " 条"), WarnIterations);

            Log.Backend = new GodotLogBackend();
            Measure(report, "Log.Error + godot（带栈帧）", i => Log.Error("[Bench] 第 ", i, " 条"), ErrorIterations);
            Log.StackTraceLevel = LogLevel.Off;
            Measure(report, "Log.Error + godot（不带栈帧）", i => Log.Error("[Bench] 第 ", i, " 条"), ErrorIterations);

            Log.Backend = new ConsoleLogBackend();
            Log.StackTraceLevel = LogLevel.Warn;
            Measure(report, "Log.Error + console（带栈帧）", i => Log.Error("[Bench] 第 ", i, " 条"), ErrorIterations);
            Log.StackTraceLevel = LogLevel.Off;
            Measure(report, "Log.Error + console（不带栈帧）", i => Log.Error("[Bench] 第 ", i, " 条"), ErrorIterations);

            Log.StackTraceLevel = savedStackTrace;

            report.AppendLine();
            report.AppendLine("Trace 路径——阈值是 Info，下面这些调用一条都不会输出");

            Measure(report, "  不守卫，参数照样拼串", i => Log.Trace("[Bench] 第 " + i + " 条"));
            Measure(report, "  不守卫，拼的是格式化过的值", i => Log.Trace("[Bench] 第 " + i.ToString("D8") + " 条"));
            Measure(report, "  带 IsOn 守卫，早退", i => { if (Log.IsOn(LogLevel.Trace)) Log.Trace("[Bench] 第 " + i + " 条"); });
            Measure(report, "  空循环（下限）", i => { });
        }
        finally
        {
            // 基准动过的全局状态一律还原，跑完这个进程还要接着干别的
            Log.MinLevel = savedLevel;
            Log.Backend = savedBackend;
        }

        string path = ProjectSettings.GlobalizePath("user://bench-log.txt");
        File.WriteAllText(path, report.ToString());
        return path;
    }

    private static void Measure(StringBuilder report, string name, Action<int> body, int iterations = Iterations)
    {
        // 慢的组次数少，预热也跟着按比例缩，不然预热本身比测量还久
        int warmup = Math.Min(Warmup, iterations);

        for (int i = 0; i < warmup; i++)
        {
            body(i);
        }

        // 取最小值而不是平均：这台机器上偶尔会有别的进程来抢，均值会被那几次拖高，
        // 最小值才是"没人打扰时到底多快"
        double best = double.MaxValue;
        for (int round = 0; round < Rounds; round++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                body(i);
            }
            watch.Stop();

            best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1000.0 / iterations);
        }

        report.AppendLine($"{name}：{best:F3} 微秒每次");
    }
}
