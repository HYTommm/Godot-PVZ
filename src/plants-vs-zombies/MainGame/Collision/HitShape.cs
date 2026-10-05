namespace PlantsVsZombies.Collision;

/// <summary>判定形状的种类</summary>
public enum ShapeKind
{
	Rect,
	Circle,
}

/// <summary>
/// 一个二维判定形状。
///
/// **坐标与尺寸都是整数。** 原版的坐标在内存里存的是 16 位浮点，但所有判定计算都会
/// 向零取整（抹去小数部分）——判定层看到的从来不是浮点。这里跟着对齐，不是洁癖：
/// 土豆雷的区间被同行的撑杆压到只剩一个判定点，那个点之所以是个"点"而不是"一段"，
/// 靠的就是整数运算。在浮点域里它会被摊成一个很窄的区间，行为就不是原版了。
///
/// Rect 用左上角 + 宽高，Circle 用圆心 + 半径。**Width 允许为负**，它表示"区间反向"
/// （左界越过了右界）——那是撑杆把土豆雷判定点推走的机制本身，不是非法输入，
/// 所以这里不设任何防线，判定函数那边也不过滤。
/// </summary>
public struct HitShape
{
	public ShapeKind Kind;

	/// <summary>Rect 是左上角 x；Circle 是圆心 x</summary>
	public int X;

	/// <summary>Rect 是左上角 y；Circle 是圆心 y</summary>
	public int Y;

	/// <summary>Rect 的宽。可为负，表示区间反向</summary>
	public int Width;

	/// <summary>Rect 的高</summary>
	public int Height;

	/// <summary>Circle 的半径</summary>
	public int Radius;

	public static HitShape Rect(int x, int y, int width, int height) => new()
	{
		Kind = ShapeKind.Rect,
		X = x,
		Y = y,
		Width = width,
		Height = height,
	};

	public static HitShape Circle(int centerX, int centerY, int radius) => new()
	{
		Kind = ShapeKind.Circle,
		X = centerX,
		Y = centerY,
		Radius = radius,
	};

	/// <summary>
	/// 从浮点世界坐标构造矩形，**向零取整**。
	///
	/// C# 的 (int) 转换与原版的取整方向一致，都是朝零截断——注意它不是四舍五入，
	/// 也不是向下取整：-1.5 取到 -1 而不是 -2。
	/// </summary>
	public static HitShape RectFromWorld(float x, float y, float width, float height) =>
		Rect((int)x, (int)y, (int)width, (int)height);
}
