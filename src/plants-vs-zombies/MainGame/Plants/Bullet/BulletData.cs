/// <summary>
/// 弹种。决定贴图，以及命中时附带什么效果。
/// </summary>
public enum BulletKind
{
	/// <summary>普通豌豆：只扣血</summary>
	Pea,

	/// <summary>寒冰豌豆：扣血并附加减速</summary>
	SnowPea,
}

/// <summary>
/// 一条子弹的全部数值。
///
/// 子弹不再是一个节点：位置、速度、伤害都在这个 struct 里，自己算。
/// 画面由 BulletSystem 每帧向 RenderingServer 直渲，判定由 BulletSystem 自己算矩形。
/// 这样能预分配一个大数组，遍历时一条子弹的字段在内存里挨着——节点那套是指针跳转，
/// 上千条的时候缓存全落空。
///
/// 节点引用（精灵、粒子、音效）一律不进这里，那些归表现层管。
/// </summary>
public struct BulletData
{
	/// <summary>世界坐标（像素）。不是节点坐标——"自己算矩形判定"的前提就是位置在自己手里</summary>
	public float X, Y;

	/// <summary>速度（像素/秒）</summary>
	public float VelX, VelY;

	/// <summary>纵向加速度（像素/秒²）。抛物线的弹种用它，直线弹填 0</summary>
	public float AccY;

	/// <summary>这颗子弹在地上的影子所在的 Y。发射时就定死，飞行中不变</summary>
	public float ShadowY;

	public BulletKind Kind;

	public int Damage;
	public HurtType Type;

	/// <summary>还能穿透几个目标。不穿透的填 1，命中一次就没了</summary>
	public int PenetrateLeft;

	/// <summary>属于哪一行。决定它打谁，也决定画在哪一层</summary>
	public int Row;

	/// <summary>本帧是否还活着。压实那一趟会把 false 的挤出去</summary>
	public bool Alive;
}
