using System.Collections.Generic;

/// <summary>
/// 关卡数值表 —— <see cref="LevelDataGenerator"/> 与 <see cref="LevelDataVerifier"/> 共用的唯一真相。
///
/// 数值取自 PVZ 原版游戏数据。
///
/// ## 这个表和 .tres 的关系
///
/// - 生成器按本表**创建/覆盖** res://MainGame/Levels/ 下的 .tres
/// - 验证器按本表**逐字段比对**已有 .tres，报告不一致
/// - 在 Godot 编辑器里手工调整某个 .tres 是允许的，但那样它就会与本表不一致，
///   验证器会报出来 —— 这是有意的：让"手工改过"这件事始终可见
///
/// ## 注意：等于 LevelData.cs 字段初值的属性不会被写进 .tres
///
/// Godot 保存资源时会省略等于脚本默认值的属性，所以 .tres 里存的是"与默认值的差异"。
/// 后果是 <see cref="LevelData"/> 的**字段初值有实际效力**：改它会静默改变所有依赖默认值的关卡，
/// 而 .tres 一个字节不变。验证器比对的是**运行时生效值**，所以这种漂移也能被发现。
/// </summary>
public static class LevelDataSpec
{
	public const string LevelsDir = "res://MainGame/Levels/";
	public const string ListFileName = "LevelList";

	// ---- 全关卡通用（取自原版）----

	/// <summary>
	/// 每波容量上限公式的基础系数。
	/// 原版一周目每波僵尸点数 = floor(波索引 / 3) + 1（波索引 **0 起算**）；
	/// 项目公式化简后是 floor(波索引 * 系数 / 2)，故取 2/3 时斜率完全一致、逐波精确吻合。
	/// 二周目对应 4/5 = 0.8（原版是 floor(波索引 * 2 / 5) + 1）。
	/// </summary>
	public const float WaveCapacityBase = 2f / 3f;

	/// <summary> 旗帜波容量倍数（原版 ×2.5） </summary>
	public const float BigWaveMultiplier = 2.5f;

	/// <summary> 波次间隔下限（秒），原版 2500 tick </summary>
	public const double WaveIntervalMin = 25.0;

	/// <summary> 波次间隔上限（秒），原版 2500 + 600 tick </summary>
	public const double WaveIntervalMax = 31.0;

	/// <summary> 提前推进的血量阈值下限（%），原版每波重抽 0.5~0.65 </summary>
	public const int EarlyAdvanceHealthPercentMin = 50;

	/// <summary> 提前推进的血量阈值上限（%） </summary>
	public const int EarlyAdvanceHealthPercentMax = 65;

	/// <summary> 提前推进后把下一波倒计时压到的秒数，原版 200 tick </summary>
	public const double EarlyAdvanceSeconds = 2.0;

	// ---- 僵尸自身数值（取自原版）----

	/// <summary> 权重 / 等级 / 首个允许出场波 </summary>
	public static readonly (ZombieTypeEnum Type, int Weight, int Grade, int FirstAllowedWave)[] ZombieStats =
	[
		(ZombieTypeEnum.Normal, 4000, 1, 1),
		(ZombieTypeEnum.Conehead, 4000, 2, 1),
		(ZombieTypeEnum.Buckethead, 3000, 4, 1),
		(ZombieTypeEnum.Screendoor, 3500, 4, 5),
		(ZombieTypeEnum.Polevaulter, 2000, 2, 5),
		(ZombieTypeEnum.Newspaper, 1000, 2, 1),
		(ZombieTypeEnum.Football, 2000, 7, 5),
	];

	/// <summary> 一种僵尸在某个关卡里的期望配置 </summary>
	public readonly struct ZombieExpectation(ZombieTypeEnum type, int weight, int grade, int firstAllowedWave)
	{
		public readonly ZombieTypeEnum Type = type;
		public readonly int Weight = weight;
		public readonly int Grade = grade;
		public readonly int FirstAllowedWave = firstAllowedWave;

		/// <summary> 按类型从 <see cref="ZombieStats"/> 取默认数值 </summary>
		public static ZombieExpectation Of(ZombieTypeEnum type)
		{
			foreach ((ZombieTypeEnum Type, int Weight, int Grade, int FirstAllowedWave) stat in ZombieStats)
			{
				if (stat.Type == type)
				{
					return new ZombieExpectation(stat.Type, stat.Weight, stat.Grade, stat.FirstAllowedWave);
				}
			}
			return new ZombieExpectation(type, 0, 1, 1);
		}
	}

	/// <summary> 一个关卡的期望值 </summary>
	public readonly struct LevelExpectation
	{
		/// <summary> LevelId，也是与 .tres 对齐的键（"1-1" / "DEBUG"） </summary>
		public readonly string Id;

		/// <summary> 所在世界号（1 起），用于在 LevelList 里定位 </summary>
		public readonly int World;

		/// <summary> 该世界内的关卡序号（1 起） </summary>
		public readonly int Index;

		public readonly SceneKind Scene;
		public readonly int SunStart;
		public readonly int Waves;
		public readonly int FlagWaveInterval;
		public readonly double FirstWaveDelay;
		public readonly float WaveCapacityMultiplier;
		public readonly ZombieTypeEnum? IntroducedZombie;
		public readonly ZombieExpectation[] Pool;

		public LevelExpectation(string id, int world, int index, SceneKind scene, int sunStart,
			int waves, int flagWaveInterval, double firstWaveDelay, float waveCapacityMultiplier,
			ZombieTypeEnum? introducedZombie, ZombieTypeEnum[] pool)
		{
			Id = id;
			World = world;
			Index = index;
			Scene = scene;
			SunStart = sunStart;
			Waves = waves;
			FlagWaveInterval = flagWaveInterval;
			FirstWaveDelay = firstWaveDelay;
			WaveCapacityMultiplier = waveCapacityMultiplier;
			IntroducedZombie = introducedZombie;
			Pool = new ZombieExpectation[pool.Length];
			for (int i = 0; i < pool.Length; i++)
			{
				Pool[i] = ZombieExpectation.Of(pool[i]);
			}
		}

		/// <summary> 该关的 .tres 文件名（不含扩展名）："1-1" → "Level_1_1"，"DEBUG" → "Level_Debug" </summary>
		public string FileName => Id == "DEBUG" ? "Level_Debug" : "Level_" + Id.Replace('-', '_');

		/// <summary> 该关 .tres 的完整 res:// 路径 </summary>
		public string ResourcePath => $"{LevelsDir}{FileName}.tres";
	}

	/// <summary>
	/// 世界 1（1-1 ~ 1-10）逐关期望值。各字段的取值规则：
	///   Waves            ← 原版各关的总波数
	///   Pool             ← 原版各关允许出场的僵尸
	///   FlagWaveInterval ← 常规 10；首次游玩且本关总波数 &lt; 10 时取本关总波数；
	///                       1-1 无旗帜波，用 0 表示
	///   FirstWaveDelay   ← 常规 18 秒；1-2 是特例 50 秒
	///   IntroducedZombie ← 该僵尸首次出现的关卡，原版会在第 (Waves/2) 波与末波各投放一只
	///   GradeMultiplier  ← 坚果保龄球关（第 5 关）×4、小 Boss 关（第 10 关）×3
	/// </summary>
	public static readonly LevelExpectation[] World1 =
	[
		// id      world index scene           sun waves flag first  mult  introduced                             pool
		new("1-1",  1, 1, SceneKind.Day, 50, 4,  0,  18.0, 1f,  null,                            [ZombieTypeEnum.Normal]),
		new("1-2",  1, 2, SceneKind.Day, 50, 6,  6,  50.0, 1f,  null,                            [ZombieTypeEnum.Normal]),
		new("1-3",  1, 3, SceneKind.Day, 50, 8,  8,  18.0, 1f,  ZombieTypeEnum.Conehead,         [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead]),
		new("1-4",  1, 4, SceneKind.Day, 50, 10, 10, 18.0, 1f,  null,                            [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead]),
		new("1-5",  1, 5, SceneKind.Day, 50, 8,  8,  18.0, 4f,  null,                            [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead]),
		new("1-6",  1, 6, SceneKind.Day, 50, 10, 10, 18.0, 1f,  ZombieTypeEnum.Polevaulter,      [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Polevaulter]),
		new("1-7",  1, 7, SceneKind.Day, 50, 20, 10, 18.0, 1f,  null,                            [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Polevaulter]),
		new("1-8",  1, 8, SceneKind.Day, 50, 10, 10, 18.0, 1f,  ZombieTypeEnum.Buckethead,       [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Buckethead]),
		new("1-9",  1, 9, SceneKind.Day, 50, 20, 10, 18.0, 1f,  null,                            [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Polevaulter, ZombieTypeEnum.Buckethead]),
		new("1-10", 1, 10, SceneKind.Day, 50, 20, 10, 18.0, 3f, null,                            [ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Polevaulter, ZombieTypeEnum.Buckethead]),
	];

	/// <summary>
	/// 调试图关。**这一关的数值是项目自己的调试配置，不是 PVZ 原版数值。**
	/// 满阳光 + 全僵尸池 + 波数短 + 首波快，便于反复试。
	/// </summary>
	public static readonly LevelExpectation[] DebugWorld =
	[
		new("DEBUG", 2, 1, SceneKind.Day, 50000, 3, 10, 1.0, 1f, null,
			[ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Buckethead,
			 ZombieTypeEnum.Screendoor, ZombieTypeEnum.Polevaulter, ZombieTypeEnum.Newspaper,
			 ZombieTypeEnum.Football]),
	];

	/// <summary> 全部关卡的期望值（世界 1 + 调试图关） </summary>
	public static IEnumerable<LevelExpectation> All()
	{
		foreach (LevelExpectation level in World1)
		{
			yield return level;
		}
		foreach (LevelExpectation level in DebugWorld)
		{
			yield return level;
		}
	}
}
