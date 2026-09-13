using Godot;
using System.Collections.Generic;

/// <summary>
/// 关卡数据验证器：把 res://MainGame/Levels/ 下**运行时真正生效的值**与
/// <see cref="LevelDataSpec"/> 逐字段比对，报告任何不一致。
///
/// ## 触发方式
///
/// <code>
/// godot --headless --quit --path &lt;项目目录&gt; -- --verify-levels
/// </code>
/// 不加参数时完全不执行。
///
/// ## 它能查出什么
///
/// 这个工具是为了兜住两个已经踩过的静默坑而写的：
///
/// - 关卡被内嵌进 LevelList、独立 .tres 不被读取。
///   本验证器会检查每个关卡的 <c>ResourcePath</c> 是否指向它自己的文件，一旦被重新内嵌就会报出来。
/// - 等于 `LevelData.cs` 字段初值的属性不会被写进 .tres，值由 C# 初值提供。
///   所以**改字段初值会静默改变所有依赖默认值的关卡**。本验证器比对的是**运行时生效值**，
///   这种"文件没变、行为却变了"的漂移同样会被报出来。
///
/// 另外：在编辑器里手工调整某个 .tres 是允许的，但那样它就会与本表不一致，
/// 验证器会报出来——这是有意的，让"手工改过"这件事始终可见。
///
/// ## 局限
///
/// 失败时只打印报告，**不设置进程退出码**（Godot 的退出码由 --quit 决定），
/// 所以判断结果请看输出的最后一行 [验证器]。
/// </summary>
public static class LevelDataVerifier
{
	/// <summary> 比对全部关卡，返回不一致的条目数（0 表示通过） </summary>
	public static int VerifyAll()
	{
		string listPath = $"{LevelDataSpec.LevelsDir}{LevelDataSpec.ListFileName}.tres";
		LevelList list = ResourceLoader.Load<LevelList>(listPath);
		if (list == null)
		{
			GD.PrintErr($"[验证器] 加载失败：{listPath}");
			return 1;
		}

		List<string> failures = new();
		HashSet<string> checkedIds = new();

		foreach (LevelDataSpec.LevelExpectation expectation in LevelDataSpec.All())
		{
			LevelData level = GetFromList(list, expectation.World, expectation.Index);
			if (level == null)
			{
				failures.Add($"{expectation.Id}：在 LevelList 的第 {expectation.World} 个世界第 {expectation.Index} 关位置取不到");
				continue;
			}
			checkedIds.Add(level.LevelId);
			CompareLevel(failures, expectation, level);
		}

		// 反向检查：LevelList 里有没有 spec 之外的关卡
		list.ForEachLevel((world, index, level) =>
		{
			if (level != null && !checkedIds.Contains(level.LevelId))
			{
				failures.Add($"LevelList 第 {world} 个世界第 {index} 关是 spec 里没有的关卡（LevelId={level.LevelId}）");
			}
		});

		if (failures.Count == 0)
		{
			GD.Print($"[验证器] 通过：{checkedIds.Count} 个关卡全部与 LevelDataSpec 一致");
			return 0;
		}

		GD.PrintErr($"[验证器] 发现 {failures.Count} 处不一致：");
		foreach (string failure in failures)
		{
			GD.PrintErr($"  - {failure}");
		}
		return failures.Count;
	}

	/// <summary> 逐字段比对单个关卡 </summary>
	private static void CompareLevel(List<string> failures, LevelDataSpec.LevelExpectation expectation, LevelData level)
	{
		string id = expectation.Id;

		// 独立资源检查：被内嵌进 LevelList 时 ResourcePath 会是 "LevelList.tres::Resource_xxx"
		Check(failures, id, "资源路径（应为独立文件）", level.ResourcePath, expectation.ResourcePath);

		Check(failures, id, "LevelName", level.LevelName, expectation.Id);
		Check(failures, id, "LevelId", level.LevelId, expectation.Id);
		Check(failures, id, "SceneType", level.SceneType, expectation.Scene);
		Check(failures, id, "SunStart", level.SunStart, expectation.SunStart);
		Check(failures, id, "Waves", level.Waves, expectation.Waves);
		Check(failures, id, "FlagWaveInterval", level.FlagWaveInterval, expectation.FlagWaveInterval);
		Check(failures, id, "FirstWaveDelay", level.FirstWaveDelay, expectation.FirstWaveDelay);
		Check(failures, id, "WaveCapacityMultiplier", level.WaveCapacityMultiplier, expectation.WaveCapacityMultiplier);

		// 通用数值（全部关卡一致，来自 spec 常量；注意它们可能没被写进 .tres，值由 C# 字段初值提供）
		Check(failures, id, "WaveCapacityBase", level.WaveCapacityBase, LevelDataSpec.WaveCapacityBase);
		Check(failures, id, "BigWaveMultiplier", level.BigWaveMultiplier, LevelDataSpec.BigWaveMultiplier);
		Check(failures, id, "WaveIntervalMin", level.WaveIntervalMin, LevelDataSpec.WaveIntervalMin);
		Check(failures, id, "WaveIntervalMax", level.WaveIntervalMax, LevelDataSpec.WaveIntervalMax);
		Check(failures, id, "EarlyAdvanceHealthPercentMin", level.EarlyAdvanceHealthPercentMin, LevelDataSpec.EarlyAdvanceHealthPercentMin);
		Check(failures, id, "EarlyAdvanceHealthPercentMax", level.EarlyAdvanceHealthPercentMax, LevelDataSpec.EarlyAdvanceHealthPercentMax);
		Check(failures, id, "EarlyAdvanceSeconds", level.EarlyAdvanceSeconds, LevelDataSpec.EarlyAdvanceSeconds);

		// 介绍僵尸
		bool expectIntroduced = expectation.IntroducedZombie.HasValue;
		Check(failures, id, "SpawnIntroducedZombie", level.SpawnIntroducedZombie, expectIntroduced);
		if (expectIntroduced)
		{
			Check(failures, id, "IntroducedZombie", level.IntroducedZombie, expectation.IntroducedZombie.Value);
		}

		// 僵尸池
		if (level.WavePool.Count != expectation.Pool.Length)
		{
			failures.Add($"{id} 的僵尸池条目数：实际 {level.WavePool.Count}，期望 {expectation.Pool.Length}");
			return;
		}
		for (int i = 0; i < expectation.Pool.Length; i++)
		{
			ZombieWaveEntry entry = level.WavePool[i];
			LevelDataSpec.ZombieExpectation expect = expectation.Pool[i];
			string where = $"{id} 僵尸池第 {i + 1} 项";
			if (entry == null)
			{
				failures.Add($"{where}：为 null，期望 {expect.Type}");
				continue;
			}
			Check(failures, where, "Type", entry.Type, expect.Type);
			Check(failures, where, "Weight", entry.Weight, expect.Weight);
			Check(failures, where, "Grade", entry.Grade, expect.Grade);
			Check(failures, where, "FirstAllowedWave", entry.FirstAllowedWave, expect.FirstAllowedWave);
			Check(failures, where, "Allowed", entry.Allowed, true);
		}
	}

	/// <summary> 按世界号与关卡序号（均 1 起）从 LevelList 取关；取不到返回 null </summary>
	private static LevelData GetFromList(LevelList list, int world, int index)
	{
		if (world < 1 || world > list.Levels.Count)
		{
			return null;
		}
		Godot.Collections.Array<LevelData> levelsOfWorld = list.Levels[world - 1];
		if (levelsOfWorld == null || index < 1 || index > levelsOfWorld.Count)
		{
			return null;
		}
		return levelsOfWorld[index - 1];
	}

	/// <summary> 不一致就记一条，附上实际值与期望值 </summary>
	private static void Check<T>(List<string> failures, string where, string field, T actual, T expected)
	{
		if (!EqualityComparer<T>.Default.Equals(actual, expected))
		{
			failures.Add($"{where} 的 {field}：实际 {actual}，期望 {expected}");
		}
	}
}
