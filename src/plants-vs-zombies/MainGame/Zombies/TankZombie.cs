/// <summary>
/// 肉盾僵尸：血极厚、走得极慢的测试用僵尸。
///
/// 只改数值，动画与形象全部照普通僵尸，用来给碰撞与伤害判定制造压力
/// ——它短时间打不死，会一直站在场上挨打，方便持续观察。
/// </summary>
public partial class TankZombie : NormalZombie
{
	/// <summary>
	/// 血量。厚到几乎打不死，免得观察中途僵尸没了。
	///
	/// 超频豌豆 10 株是 1000 发/秒 × 20 伤害 = 2 万/秒，一亿能扛约 83 分钟。
	/// 不能取到接近 int.MaxValue：血量阶段的阈值是按 MaxHP 的 2/3 算的，
	/// MaxHP * 2 会先溢出（int 上限 21.4 亿）
	/// </summary>
	private const int TestMaxHP = 100_000_000;

	/// <summary>
	/// 行走动画速率。普通僵尸抽的是 0.88~1.41，这里压到约 1/6。
	/// 世界位移由走路动画的地面轨道驱动，速率小了移速自然就慢
	/// </summary>
	private const float TestWalkSpeed = 0.2f;

	public override void _Ready()
	{
		if (!BIsDisplayOnly)
		{
			// 血量必须在 base._Ready() 注册血量阶段回调之前改好：
			// 默认阶段的阈值是按 MaxHP 的 1/3、2/3 算的，改完要让它重算一遍
			HealthStageComponent.MaxHP = TestMaxHP;
			HealthStageComponent.HP = TestMaxHP;
			HealthStageComponent.Refresh();
		}

		base._Ready();
	}

	protected override void PickRandomSpeed()
	{
		WalkSpeed = TestWalkSpeed; // 不抽随机，固定极慢
	}
}
