/// <summary>
/// 草坪格子。只是个数据槽——记这一格是什么地形、上面站着几株植物，
/// 由 Scene 建 9×5 个放进 LawnArray。
///
/// 它原本是 Node2D，但从来没挂进过场景树，于是每个格子都是一个孤儿节点、
/// 白占一个 CanvasItem RID（45 个格子，正好是退出时那 45 个 RID 泄漏）。
/// 这里用不到任何节点能力，改成普通类，孤儿和 RID 一起消失。
/// </summary>
public class Lawn
{
	/// <summary>内置类：类型</summary>
	public class LawnType
	{
		/// <summary>草地</summary>
		public const int Grass = 0;
		/// <summary>水池</summary>
		public const int Pool = 1;
		/// <summary>屋顶</summary>
		public const int Roof = 2;
	}

	/// <summary>草地类型</summary>
	public int Type = LawnType.Grass;

	/// <summary>植物数量</summary>
	public int PlantCount = 0;
}
