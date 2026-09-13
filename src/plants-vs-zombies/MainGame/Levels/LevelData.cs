using Godot;

/// <summary>
/// 一个关卡的全部可配置数据。一关一个 .tres，放在 res://MainGame/Levels/ 下。
///
/// 覆盖范围（刻意划定的边界）：
///   - 波次与僵尸池：本文件里的 Waves / WavePool / 各种系数
///   - 场景类型：SceneType，只表达"哪一种场景"，草坪尺寸坐标等仍由 Scene 子类管理
///   - 初始阳光：SunStart
///
/// 不覆盖：草坪行列数/单位尺寸/原点坐标/背景贴图/BGM（仍属 Scene 子类）、
/// 种子包可用植物列表（目前在 seed_bank.tscn 的场景树里）、存档与解锁进度。
/// </summary>
[GlobalClass]
public partial class LevelData : Resource
{
	/// <summary> 显示用名称，如 "1-1" </summary>
	[Export] public string LevelName = "1-1";

	/// <summary> 查表/日志用编号，如 "1-1"；调试图关为 "DEBUG" </summary>
	[Export] public string LevelId = "1-1";

	/// <summary> 场景类型 </summary>
	[Export] public SceneKind SceneType = SceneKind.Day;

	/// <summary> 初始阳光 </summary>
	[Export] public int SunStart = 50;

	// ---- 波次 ----

	/// <summary> 本关总波数（打到最后这个波次的僵尸死亡即通关） </summary>
	[Export] public int Waves = 10;

	/// <summary>
	/// 每波容量上限公式里的基础系数。
	/// 原式：zombieMaxGrade = int(int(波索引 * WaveCapacityBase) / 2) + 1
	///
	/// 原版的每波僵尸点数是 floor(波索引 / 3) + 1（波索引 **0 起算**，
	/// 其编号从 0 起算）。项目的公式形状按整除恒等式
	/// floor(floor(a)/2) = floor(a/2) 化简后是 floor(波索引 * 系数 / 2)，
	/// 所以系数取 **2/3** 时与 1/3 的斜率完全一致，逐波精确吻合（不是拟合）。
	///
	/// 前提是 ZombieCurrentWave 也 0 起算；若改回 1 起算，这个 2/3 就不再成立。
	/// </summary>
	[Export] public float WaveCapacityBase = 2f / 3f;

	/// <summary> 大波（旗帜波）的容量倍数 </summary>
	[Export] public float BigWaveMultiplier = 2.5f;

	/// <summary>
	/// 关卡级点数倍率，在旗帜波的 BigWaveMultiplier 之后相乘。
	///
	/// 原版按"游戏模式 + 关卡号"判定，与是否通关无关：
	/// 坚果保龄球关（冒险第 5 关）= 4、小 Boss 关（冒险第 10 / 20 / 30 关）= 3、其余 = 1。
	/// 世界 1 里只有 1-5（×4）与 1-10（×3）命中。
	/// 注意两处都是整数相乘后截断，所以与旗帜 ×2.5 的顺序不可交换。
	/// </summary>
	[Export] public float WaveCapacityMultiplier = 1.0f;

	/// <summary> 每隔几波算一次大波，当前硬编码是 10 </summary>
	[Export] public int FlagWaveInterval = 10;

	/// <summary> 首波延迟（秒） </summary>
	[Export] public double FirstWaveDelay = 19.0;

	/// <summary> 波次间隔下限（秒） </summary>
	[Export] public double WaveIntervalMin = 25.0;

	/// <summary> 波次间隔上限（秒） </summary>
	[Export] public double WaveIntervalMax = 31.0;

	/// <summary>
	/// 提前推进的血量阈值下限（百分比）。
	///
	/// 原版这个阈值是**每波重新抽一次**的随机值，区间 50% ~ 65%，
	/// 所以这里存的是区间而不是定值。全场僵尸总血量降到阈值以下时，
	/// 把下一波倒计时压到 EarlyAdvanceSeconds。
	/// </summary>
	[Export] public int EarlyAdvanceHealthPercentMin = 50;

	/// <summary> 提前推进的血量阈值上限（百分比），原版 65 </summary>
	[Export] public int EarlyAdvanceHealthPercentMax = 65;

	/// <summary> 提前推进时下一波的剩余倒计时（秒） </summary>
	[Export] public double EarlyAdvanceSeconds = 2.0;

	// ---- 僵尸池 ----

	/// <summary> 本关的僵尸池：能出哪些僵尸、各自权重与等级 </summary>
	[Export] public Godot.Collections.Array<ZombieWaveEntry> WavePool = new();

	/// <summary>
	/// 本关"首次登场"的僵尸类型。原版在每种僵尸首次出现的关卡里，
	/// 会在第 (Waves / 2) 波和最后一波各放一只，作为亮相。
	/// </summary>
	[Export] public ZombieTypeEnum IntroducedZombie = ZombieTypeEnum.Normal;

	/// <summary> 是否启用 IntroducedZombie 的定点亮相；没有新僵尸首现的关卡保持 false </summary>
	[Export] public bool SpawnIntroducedZombie = false;
}
