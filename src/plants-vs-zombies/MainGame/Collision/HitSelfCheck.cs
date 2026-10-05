using System.Text;

namespace PlantsVsZombies.Collision;

/// <summary>
/// 判定层的最小自检。**纯函数，没有任何调用方**——要跑它得在外面加一行。
///
/// 验的是土豆雷那条链子：触发区间被同行的撑杆压成负宽度之后，唯一还能返回非负
/// 交叠的位置是"僵尸右边缘恰好落在左界上"。这个行为是整套机制的核心，也是判定
/// 必须走整数域、必须容许负宽度的理由——换个实现方式，它就不成立了。
///
/// 参数用"僵尸右边缘"而不是左边缘：僵尸从右往左走，这个量随时间递减，而判定要看
/// 的正是它扫过左界的那一刻。
/// </summary>
public static class HitSelfCheck
{
	private const int MineLeft0 = 400;      // 左界初值（测试用的位置）
	private const int MineOffset0 = 55;     // 偏移初值
	private const int PoleShift = 40;       // 同行每有一只撑杆：左界 +40、偏移 -40
	private const int ZombieWidth = 42;

	public static string Run()
	{
		StringBuilder sb = new();

		sb.AppendLine("土豆雷的触发区间：左界 = 400 + 40n，偏移 = 55 - 40n，右界恒为 455");
		sb.AppendLine();

		// n = 0：区间 [400, 455]，一切正常
		Check(sb, "n=0  僵尸右边缘 400（刚接触）", 0, OverlapAt(0, 400));
		Check(sb, "n=0  僵尸右边缘 420（深入 20）", 20, OverlapAt(0, 420));
		Check(sb, "n=0  僵尸右边缘 380（还差 20）", -20, OverlapAt(0, 380));

		// n = 1：区间 [440, 455]，窄到 15，但仍是正常区间
		Check(sb, "n=1  僵尸右边缘 440（刚接触）", 0, OverlapAt(1, 440));
		Check(sb, "n=1  僵尸右边缘 455（盖满 15 宽的区间）", 15, OverlapAt(1, 455));
		Check(sb, "n=1  僵尸右边缘 439（还差 1）", -1, OverlapAt(1, 439));

		// n = 2：偏移 -25，区间反向。只剩"僵尸右边缘 == 左界 480"这一个位置非负
		Check(sb, "n=2  僵尸右边缘 480（恰好等于左界）", 0, OverlapAt(2, 480));
		Check(sb, "n=2  僵尸右边缘 479（差 1）", -1, OverlapAt(2, 479));
		Check(sb, "n=2  僵尸右边缘 470（整个在左界左边）", -10, OverlapAt(2, 470));
		Check(sb, "n=2  僵尸右边缘 500（跨在左界上）", -25, OverlapAt(2, 500));

		// 核对过的三组：宽度为负（-25），僵尸从右边靠近，相邻位置只差一像素
		// 119 → -1、120 → 0、121 → -25。注意第三下不是渐变，是直接掉到"那个负宽度"
		Check(sb, "核对值 僵尸 x=77（右边缘 119，差 1）", -1, RawOverlap(120, -25, 77, 42));
		Check(sb, "核对值 僵尸 x=78（右边缘 120，恰好压上）", 0, RawOverlap(120, -25, 78, 42));
		Check(sb, "核对值 僵尸 x=79（右边缘 121，刚过一像素）", -25, RawOverlap(120, -25, 79, 42));

		return sb.ToString();
	}

	/// <summary>同行有 poles 只撑杆、僵尸右边缘停在 zombieRight 时的交叠长度</summary>
	private static int OverlapAt(int poles, int zombieRight)
	{
		int left = MineLeft0 + PoleShift * poles;
		int offset = MineOffset0 - PoleShift * poles;
		return RawOverlap(left, offset, zombieRight - ZombieWidth, ZombieWidth);
	}

	/// <summary>直接按两个矩形的原始参数算，供核对值那几组用</summary>
	private static int RawOverlap(int mineX, int mineWidth, int zombieX, int zombieWidth)
	{
		return HitMath.OverlapX(
			HitShape.Rect(mineX, 0, mineWidth, 100),
			HitShape.Rect(zombieX, 0, zombieWidth, 100));
	}

	private static void Check(StringBuilder sb, string what, int expected, int actual)
	{
		string verdict = expected == actual ? "ok  " : "FAIL";
		sb.AppendLine($"[{verdict}] {what}：预期 {expected}，实得 {actual}");
	}
}
