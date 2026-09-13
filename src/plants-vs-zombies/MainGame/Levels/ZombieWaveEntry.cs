using Godot;

/// <summary>
/// 关卡里一种僵尸的出场配置。
///
/// 之所以要单独做一层 Resource：原来权重和等级是
/// Dictionary&lt;ZombieTypeEnum, int&gt;（见 ZombieWeightsAndGrades），
/// 而 Dictionary 无法被 [Export]，所以关卡数据里改用可序列化的条目列表表达。
/// 运行时仍会先转回 Dictionary，波次生成逻辑不受影响。
/// </summary>
[GlobalClass]
public partial class ZombieWaveEntry : Resource
{
	/// <summary> 僵尸类型 </summary>
	[Export] public ZombieTypeEnum Type = ZombieTypeEnum.Normal;

	/// <summary> 抽取权重，越大越容易被抽到 </summary>
	[Export] public int Weight = 1000;

	/// <summary> 等级（占用波容量的大小） </summary>
	[Export] public int Grade = 1;

	/// <summary> 本关是否允许该僵尸出场 </summary>
	[Export] public bool Allowed = true;

	/// <summary>
	/// 首个允许出场的波数（1 起）。
	///
	/// 这是原版独立于"本关是否允许"之外的第二道门槛：
	/// 撑杆跳 / 铁栅门 / 橄榄球 = 5，其余僵尸 = 1。
	/// 未到该波数时，即使已在本关僵尸池里也不会被抽到。
	/// </summary>
	[Export] public int FirstAllowedWave = 1;
}
