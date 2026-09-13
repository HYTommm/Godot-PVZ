using Godot;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// 关卡数据生成器：按 <see cref="LevelDataSpec"/> 创建/覆盖 res://MainGame/Levels/ 下的 .tres。
///
/// ## 触发方式
///
/// 它不挂在任何节点上，靠命令行参数触发（见 Global._Ready）：
/// <code>
/// godot --headless --quit --path &lt;项目目录&gt; -- --generate-levels
/// </code>
/// 不加参数时完全不执行，正常游戏不受影响。
///
/// ## 为什么不直接手写 .tres
///
/// LevelList.Levels 是 Array&lt;Array&lt;LevelData&gt;&gt;，这种嵌套数组的 .tres 序列化语法
/// 容易写错（内层类型标注、sub_resource 与 ext_resource 的取舍），写错时往往是加载后
/// 静默变成空数组。交给 ResourceSaver 落盘，格式由引擎保证。
///
/// ## 两个必须遵守的点（都踩过）
///
/// 1. **每个关卡都要 TakeOverPath(自己的路径)**，否则保存 LevelList 时它们会被
///    **内嵌成子资源**，独立 .tres 变成只写不读的孤儿——在编辑器里改它们毫无效果。
///    顺序也重要：先存各关卡、最后存索引。
/// 2. **等于 LevelData.cs 字段初值的属性不会被写进 .tres**。所以本生成器里显式赋的某些值
///    （例如恰好等于初值的 2.5f、25.0）其实不会落盘，运行时由 C# 初值提供。
///    这不影响正确性——验证器比对的是运行时生效值，能查出任何漂移。
/// </summary>
public static class LevelDataGenerator
{
	/// <summary> 按 spec 重建全部关卡 .tres 与 LevelList.tres，返回失败数 </summary>
	public static int GenerateAll()
	{
		// 先按世界分组构造（世界号 1 起，可能不连续，故用字典 + 排序）。
		// Dictionary 要写全名：本文件同时 using 了 Godot.Collections 与 System.Collections.Generic
		System.Collections.Generic.Dictionary<int, Array<LevelData>> worldLevels = new();
		foreach (LevelDataSpec.LevelExpectation expectation in LevelDataSpec.All())
		{
			LevelData level = Build(expectation);
			// 关键：把它"归属"到自己的文件路径，否则会被内嵌进 LevelList
			level.TakeOverPath(expectation.ResourcePath);

			if (!worldLevels.TryGetValue(expectation.World, out Array<LevelData> levelsOfWorld))
			{
				levelsOfWorld = new Array<LevelData>();
				worldLevels[expectation.World] = levelsOfWorld;
			}
			levelsOfWorld.Add(level);
		}

		// 先存各关卡文件，让它们的路径生效
		int failCount = 0;
		foreach (System.Collections.Generic.KeyValuePair<int, Array<LevelData>> pair in worldLevels)
		{
			foreach (LevelData level in pair.Value)
			{
				failCount += Save(level, level.ResourcePath);
			}
		}

		// 再存索引
		LevelList list = new();
		System.Collections.Generic.List<int> worldNumbers = new(worldLevels.Keys);
		worldNumbers.Sort();
		foreach (int worldNumber in worldNumbers)
		{
			list.Levels.Add(worldLevels[worldNumber]);
		}
		failCount += Save(list, $"{LevelDataSpec.LevelsDir}{LevelDataSpec.ListFileName}.tres");

		GD.Print(failCount == 0
			? $"[生成器] 完成，共 {list.TotalLevelCount()} 关 / {list.WorldCount} 个世界，全部保存成功"
			: $"[生成器] 完成，但有 {failCount} 处保存失败");
		return failCount;
	}

	/// <summary> 按期望值构造一个 LevelData（此时还没有路径） </summary>
	private static LevelData Build(LevelDataSpec.LevelExpectation expectation)
	{
		LevelData level = new()
		{
			LevelName = expectation.Id,
			LevelId = expectation.Id,
			SceneType = expectation.Scene,
			SunStart = expectation.SunStart,
			Waves = expectation.Waves,
			FlagWaveInterval = expectation.FlagWaveInterval,
			FirstWaveDelay = expectation.FirstWaveDelay,
			WaveCapacityMultiplier = expectation.WaveCapacityMultiplier,

			WaveCapacityBase = LevelDataSpec.WaveCapacityBase,
			BigWaveMultiplier = LevelDataSpec.BigWaveMultiplier,
			WaveIntervalMin = LevelDataSpec.WaveIntervalMin,
			WaveIntervalMax = LevelDataSpec.WaveIntervalMax,
			EarlyAdvanceHealthPercentMin = LevelDataSpec.EarlyAdvanceHealthPercentMin,
			EarlyAdvanceHealthPercentMax = LevelDataSpec.EarlyAdvanceHealthPercentMax,
			EarlyAdvanceSeconds = LevelDataSpec.EarlyAdvanceSeconds,
		};

		if (expectation.IntroducedZombie.HasValue)
		{
			level.IntroducedZombie = expectation.IntroducedZombie.Value;
			level.SpawnIntroducedZombie = true;
		}

		foreach (LevelDataSpec.ZombieExpectation zombie in expectation.Pool)
		{
			level.WavePool.Add(new ZombieWaveEntry
			{
				Type = zombie.Type,
				Weight = zombie.Weight,
				Grade = zombie.Grade,
				Allowed = true,
				FirstAllowedWave = zombie.FirstAllowedWave,
			});
		}
		return level;
	}

	/// <summary> 保存到指定路径，返回失败数（0 或 1） </summary>
	private static int Save(Resource resource, string path)
	{
		Error err = ResourceSaver.Save(resource, path);
		if (err != Error.Ok)
		{
			GD.PrintErr($"[生成器] 保存失败 {path}: {err}");
			return 1;
		}
		GD.Print($"[生成器] 已保存 {path}");
		return 0;
	}
}
