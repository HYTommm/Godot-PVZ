using System;

namespace PlantsVsZombies.Collision;

/// <summary>
/// 判定运算。全部对齐原版的整数语义，**不用 Godot 的 Rect2**：后者的 Intersects
/// 对负 Size 给出的结果是错的，而负值在这里是语义、不是错误。
/// </summary>
public static class HitMath
{
	/// <summary>
	/// 两个矩形的横向交叠长度。**不重叠时返回负的间隔**（两个矩形之间的空隙取负），
	/// 接触（交叠 == 0）就算命中——调用方一律判 `>= 0`，不需要再单独判一次是否相交。
	///
	/// 一个数同时表达"交叠多深"和"还差多远"，这是跟着原版来的：负数是信息，
	/// 不是错误码。
	///
	/// **不对负宽度做任何过滤。** 区间反向时它照常返回一个数，语义变成"反向区间的
	/// 结果"——土豆雷的判定点被同行的撑杆推到远处，靠的正是这个行为：区间反向之后，
	/// 唯一还能返回非负值的位置是"僵尸右边缘恰好落在左界上"。加一句
	/// 加一句"宽度为负就直接返回"就把整个机制抹平了。
	///
	/// 分支结构与原版逐行对应，方便对照着审。
	/// </summary>
	public static int OverlapX(in HitShape a, in HitShape b)
	{
		// 先按左边缘定角色。原版用的是严格小于，两侧相等时算第一个参数在右——
		// 这里保持同样的选择，免得边界上与原版分叉
		bool aIsLeft = a.X < b.X;

		int leftRight = aIsLeft ? a.X + a.Width : b.X + b.Width;      // 左矩形的右边缘
		int rightLeft = aIsLeft ? b.X : a.X;                          // 右矩形的左边缘
		int rightRight = aIsLeft ? b.X + b.Width : a.X + a.Width;     // 右矩形的右边缘

		if (leftRight <= rightLeft)
		{
			// 不相交：负的间隔
			return leftRight - rightLeft;
		}

		if (leftRight <= rightRight)
		{
			// 左矩形的右边缘落在右矩形内，这就是真实的交叠长度
			return leftRight - rightLeft;
		}

		// 右矩形被整个盖住，交叠长度就是它的宽度
		return rightRight - rightLeft;
	}

	/// <summary>
	/// 圆与圆：Δx² + Δy² ≤ R²（两圆心距离的平方不超过半径和的平方），全整数运算。
	///
	/// **不能用外接矩形顶替**：平方和的边界跟矩形不一样，接近相切时结果会分叉。
	/// 原版的圆本来就不是绘图软件的圆，它是整数格点上的形状。
	/// </summary>
	public static bool CircleHits(in HitShape a, in HitShape b)
	{
		int dx = a.X - b.X;
		int dy = a.Y - b.Y;
		int radiusSum = a.Radius + b.Radius;
		return dx * dx + dy * dy <= radiusSum * radiusSum;
	}

	/// <summary>
	/// 两个箱子的交叠长度：形状列表**取 max** 归约。
	///
	/// 取 max 与"任一形状命中"等价（max >= 0 当且仅当存在一对 >= 0），所以并集
	/// 不需要单独的算子——一个列表就够了。要深度时拿到的是并集的最深交叠；全都
	/// 够不着时它是"最接近的那一对的负间隔"，语义仍然自洽。列表只有一个元素时
	/// 退化成单形状的行为。
	///
	/// 形状组合目前只实现 Rect × Rect——现在所有真实用例都是它，圆与圆走 CircleHits，
	/// 矩形对圆还没有真实用例。遇到没实现的组合**显式抛**：圆的 X 是圆心、Width 是 0，
	/// 拿它走 OverlapX 会算出一个看起来像模像样的垃圾值，那种错比抛异常难查得多。
	/// </summary>
	public static int OverlapX(IHitBox a, IHitBox b)
	{
		int best = int.MinValue;

		for (int i = 0; i < a.ShapeCount; i++)
		{
			HitShape left = a.GetShape(i);
			RequireRect(left);

			for (int j = 0; j < b.ShapeCount; j++)
			{
				HitShape right = b.GetShape(j);
				RequireRect(right);

				int overlap = OverlapX(left, right);
				if (overlap > best)
				{
					best = overlap;
				}
			}
		}

		return best;
	}

	private static void RequireRect(in HitShape shape)
	{
		if (shape.Kind != ShapeKind.Rect)
		{
			throw new NotSupportedException($"箱子对箱子的判定还没实现 {shape.Kind}，目前只有 Rect");
		}
	}
}
