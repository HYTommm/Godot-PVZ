using Godot;
using System.Collections.Generic;

/// <summary>
/// 可种植的植物种类。
///
/// 与 ZombieTypeEnum 对称：枚举负责"有哪些"，PlantTypes 负责"分别对应哪个场景"。
/// 在此之前"哪些植物能上卡槽"完全由 seed_bank.tscn 的场景树节点摆放决定，
/// 代码侧零控制；有了这个枚举之后，选卡、将来的解锁进度、卡槽生成都以它为准。
/// </summary>
public enum PlantTypeEnum
{
	PeaShooterSingle,
	PeaShooter,
	SnowPeaShooter,
	SunFlower,
	Wallnut,
	CherryBomb,
	PotatoMine,
	Squash,
	Chomper,
}

/// <summary>
/// 植物注册表：植物种类 → 植物场景 + 显示名。
///
/// 卡面上的阳光花费与冷却时间**不在这里存**，而是从植物场景实例上读
/// （Plants.SunCost / Plants.CDtime）。数值只在植物类里留一份，避免两处真相。
/// </summary>
public class PlantTypes
{
	private static PlantTypes _instance;

	/// <summary>植物注册表，首次访问时加载</summary>
	public static PlantTypes Instance => _instance ??= new PlantTypes();

	/// <summary>
	/// 全部植物，顺序即选卡界面的展示顺序。
	/// 当前是"全部可选"（尚未做解锁进度）；将来接存档时，可选池改由存档决定，
	/// 这个数组退化成"注册表里一共有哪些植物"。
	/// </summary>
	public static readonly PlantTypeEnum[] All =
	{
		PlantTypeEnum.PeaShooterSingle,
		PlantTypeEnum.PeaShooter,
		PlantTypeEnum.SnowPeaShooter,
		PlantTypeEnum.SunFlower,
		PlantTypeEnum.Wallnut,
		PlantTypeEnum.CherryBomb,
		PlantTypeEnum.PotatoMine,
		PlantTypeEnum.Squash,
		PlantTypeEnum.Chomper,
	};

	private readonly Dictionary<PlantTypeEnum, PackedScene> _scenes = new();
	private readonly Dictionary<PlantTypeEnum, string> _displayNames = new();

	private PlantTypes()
	{
		Add(PlantTypeEnum.PeaShooterSingle, "豌豆射手", "res://MainGame/Plants/PeaShooterSingle/PeaShooterSingle.tscn");
		Add(PlantTypeEnum.PeaShooter, "双发射手", "res://MainGame/Plants/PeaShooter/PeaShooter.tscn");
		Add(PlantTypeEnum.SnowPeaShooter, "寒冰射手", "res://MainGame/Plants/SnowPeaShooter/SnowPeaShooter.tscn");
		Add(PlantTypeEnum.SunFlower, "向日葵", "res://MainGame/Plants/Sunflower/SunFlower.tscn");
		Add(PlantTypeEnum.Wallnut, "坚果墙", "res://MainGame/Plants/WallNut/Wallnut.tscn");
		Add(PlantTypeEnum.CherryBomb, "樱桃炸弹", "res://MainGame/Plants/CherryBomb/CherryBomb.tscn");
		Add(PlantTypeEnum.PotatoMine, "土豆雷", "res://MainGame/Plants/PotatoMine/PotatoMine.tscn");
		Add(PlantTypeEnum.Squash, "窝瓜", "res://MainGame/Plants/Squash/Squash.tscn");
		Add(PlantTypeEnum.Chomper, "大嘴花", "res://MainGame/Plants/Chomper/Chomper.tscn");
	}

	private void Add(PlantTypeEnum type, string displayName, string scenePath)
	{
		PackedScene scene = GD.Load<PackedScene>(scenePath);
		if (scene == null)
		{
			GD.PrintErr($"[PlantTypes] 植物场景加载失败：{scenePath}");
			return;
		}
		_scenes[type] = scene;
		_displayNames[type] = displayName;
	}

	/// <summary>取植物的场景；未注册返回 null</summary>
	public PackedScene GetScene(PlantTypeEnum type)
	{
		return _scenes.TryGetValue(type, out PackedScene scene) ? scene : null;
	}

	/// <summary>取植物的显示名；未注册时退化为枚举名</summary>
	public string GetDisplayName(PlantTypeEnum type)
	{
		return _displayNames.TryGetValue(type, out string name) ? name : type.ToString();
	}
}
