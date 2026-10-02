/// <summary>
/// 超级豌豆射手：攻速拉到极限的测试用植物。
///
/// 只改数值，动画与形象全部照 PeaShooterSingle，用来给子弹与粒子制造压力。
/// 不进正式选卡池（见 PlantTypes.DebugOnly），只在调试图关出现。
/// </summary>
public partial class SuperPeaShooter : PeaShooterSingle
{
	public SuperPeaShooter()
	{
		SunCost = 0; // 调试用，不要钱
		CDtime = 0f; // 也不冷却，能连着摆一排
	}

	/// <summary>
	/// 超频：不挂计时器，每个物理帧都能开火。
	///
	/// 基类靠 CanShootTimer 控间隔，而 CanShoot 又是在 _PhysicsProcess 里查的，
	/// 所以间隔再小也压不过一个物理帧（本项目 100 物理帧/秒）。
	/// 这里直接把"能开火"钉成恒真，就是每物理帧一发 = 100 发/秒/株，
	/// 这是当前架构的上限——要更快就得改成一次发多发。
	///
	/// 想要一个达不到上限的射速，删掉这个覆写、改 ShootMinInterval / ShootMaxInterval 即可。
	/// </summary>
	public override void RandomShootTime()
	{
		CanShoot = true;
	}
}
