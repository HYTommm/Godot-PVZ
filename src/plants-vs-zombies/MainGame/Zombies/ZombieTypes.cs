using Godot;
using System.Collections.Generic;

public enum ZombieTypeEnum
{
	Normal,
	Conehead,
	Buckethead,
	Screendoor,
	Polevaulter,
	Newspaper,
	Football,
	Flag,
	Tank,
}

/// <summary>
/// 僵尸注册表：僵尸种类 → 僵尸场景 + 显示名。
///
/// 与 PlantTypes 对称：枚举负责"有哪些"，这里负责"分别对应哪个场景"。
/// 调试图关的僵尸卡靠它取卡面场景与卡面文字。
/// </summary>
public class ZombieType
{
	private static ZombieType _instance;

	/// <summary>僵尸注册表，首次访问时加载</summary>
	public static ZombieType Instance => _instance ??= new ZombieType();

	/// <summary>
	/// 全部僵尸，顺序即选卡界面的展示顺序。
	/// 僵尸卡本身只在调试图关出现，所以这里也含测试专用的那几种。
	/// </summary>
	public static readonly ZombieTypeEnum[] All =
	{
		ZombieTypeEnum.Normal,
		ZombieTypeEnum.Conehead,
		ZombieTypeEnum.Buckethead,
		ZombieTypeEnum.Screendoor,
		ZombieTypeEnum.Polevaulter,
		ZombieTypeEnum.Newspaper,
		ZombieTypeEnum.Football,
		ZombieTypeEnum.Flag,
		ZombieTypeEnum.Tank,
	};

	private readonly Dictionary<ZombieTypeEnum, PackedScene> _zombieScenes = new();
	private readonly Dictionary<ZombieTypeEnum, string> _displayNames = new();

	public ZombieType()
	{
		Add(ZombieTypeEnum.Normal, "普通僵尸", "res://MainGame/Zombies/NormalZombie.tscn");
		Add(ZombieTypeEnum.Conehead, "路障僵尸", "res://MainGame/Zombies/ConeheadZombie.tscn");
		Add(ZombieTypeEnum.Buckethead, "铁桶僵尸", "res://MainGame/Zombies/BucketheadZombie.tscn");
		Add(ZombieTypeEnum.Screendoor, "铁栅门僵尸", "res://MainGame/Zombies/ScreendoorZombie.tscn");
		Add(ZombieTypeEnum.Polevaulter, "撑杆僵尸", "res://MainGame/Zombies/PolevaulterZombie/PolevaulterZombie.tscn");
		Add(ZombieTypeEnum.Newspaper, "读报僵尸", "res://MainGame/Zombies/NewspaperZombie/NewspaperZombie.tscn");
		Add(ZombieTypeEnum.Football, "橄榄球僵尸", "res://MainGame/Zombies/FootballZombie/Zombie_football.tscn");
		Add(ZombieTypeEnum.Flag, "旗帜僵尸", "res://MainGame/Zombies/FlagZombie.tscn");
		Add(ZombieTypeEnum.Tank, "肉盾僵尸", "res://MainGame/Zombies/TankZombie.tscn");
	}

	private void Add(ZombieTypeEnum type, string displayName, string scenePath)
	{
		PackedScene scene = GD.Load<PackedScene>(scenePath);
		if (scene == null)
		{
			GD.PrintErr($"[ZombieType] 僵尸场景加载失败：{scenePath}");
			return;
		}
		_zombieScenes[type] = scene;
		_displayNames[type] = displayName;
	}

	/// <summary>取僵尸的场景；未注册返回 null</summary>
	public PackedScene GetZombieScene(ZombieTypeEnum zombieType)
	{
		return _zombieScenes.TryGetValue(zombieType, out PackedScene scene) ? scene : null;
	}

	/// <summary>取僵尸的显示名；未注册时退化为枚举名</summary>
	public string GetDisplayName(ZombieTypeEnum type)
	{
		return _displayNames.TryGetValue(type, out string name) ? name : type.ToString();
	}
}
