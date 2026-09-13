using Godot;
using Godot.Collections;

/// <summary>
/// 全部关卡的有序索引，供选关界面和"取某一关"使用。
///
/// 结构：外层下标 + 1 = 世界号，内层下标 + 1 = 关卡号，合起来就是 "1-1" 编码。
/// 也就是说 Levels[0][0] 是 1-1、Levels[0][9] 是 1-10、Levels[1][0] 是 2-1。
///
/// 注意容器类型：写成 System.Collections.Generic.List&lt;List&lt;T&gt;&gt; 会被 Godot
/// 的导出分析器直接拒掉（GD0102，编译期报错），所以这里用
/// Godot.Collections.Array&lt;Array&lt;T&gt;&gt;——嵌套语义一致，且内层带类型。
/// </summary>
[GlobalClass]
public partial class LevelList : Resource
{
	/// <summary> 外层=世界，内层=该世界的关卡，均按顺序 </summary>
	[Export] public Array<Array<LevelData>> Levels = new();

	/// <summary> 世界数量 </summary>
	public int WorldCount => Levels?.Count ?? 0;

	/// <summary> 第 world 个世界（1-based）的关卡数量 </summary>
	public int GetLevelCountOfWorld(int world)
	{
		if (world < 1 || world > WorldCount)
		{
			return 0;
		}
		return Levels[world - 1]?.Count ?? 0;
	}

	/// <summary> 取某关，world / index 均为 1-based；取不到返回 null </summary>
	public LevelData GetLevel(int world, int index)
	{
		if (world < 1 || world > WorldCount)
		{
			return null;
		}
		Array<LevelData> levelsOfWorld = Levels[world - 1];
		if (levelsOfWorld == null || index < 1 || index > levelsOfWorld.Count)
		{
			return null;
		}
		return levelsOfWorld[index - 1];
	}

	/// <summary> 按 LevelId 取关，先命中先返回；取不到返回 null </summary>
	public LevelData GetLevelById(string levelId)
	{
		if (string.IsNullOrEmpty(levelId))
		{
			return null;
		}
		foreach (Array<LevelData> levelsOfWorld in Levels)
		{
			if (levelsOfWorld == null)
			{
				continue;
			}
			foreach (LevelData level in levelsOfWorld)
			{
				if (level != null && level.LevelId == levelId)
				{
					return level;
				}
			}
		}
		return null;
	}

	/// <summary> 遍历所有关卡，回调 (world, index, level)，world / index 均 1-based </summary>
	public void ForEachLevel(System.Action<int, int, LevelData> action)
	{
		for (int world = 1; world <= WorldCount; world++)
		{
			Array<LevelData> levelsOfWorld = Levels[world - 1];
			if (levelsOfWorld == null)
			{
				continue;
			}
			for (int index = 1; index <= levelsOfWorld.Count; index++)
			{
				LevelData level = levelsOfWorld[index - 1];
				if (level != null)
				{
					action(world, index, level);
				}
			}
		}
	}

	/// <summary> 关卡总数 </summary>
	public int TotalLevelCount()
	{
		int total = 0;
		foreach (Array<LevelData> levelsOfWorld in Levels)
		{
			total += levelsOfWorld?.Count ?? 0;
		}
		return total;
	}
}
