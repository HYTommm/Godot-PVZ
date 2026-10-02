using Godot;

/// <summary>
/// 能上卡槽的一种"种子"：要么是植物，要么是僵尸。
///
/// 选卡面板与种子栏只认它，不关心底层是哪一类实体——正式关的可选池里只有植物，
/// 调试图关的池子里植物与僵尸混排，两边走的是同一套选卡与上槽逻辑。
///
/// 用 record struct 是为了拿到值相等：选卡状态里到处是 Contains / IndexOf。
/// </summary>
public readonly record struct SeedType
{
	/// <summary>植物种类；<see cref="IsZombie"/> 为 true 时无意义</summary>
	public PlantTypeEnum Plant { get; }

	/// <summary>僵尸种类；<see cref="IsZombie"/> 为 false 时无意义</summary>
	public ZombieTypeEnum Zombie { get; }

	/// <summary>是僵尸还是植物</summary>
	public bool IsZombie { get; }

	private SeedType(PlantTypeEnum plant)
	{
		Plant = plant;
		Zombie = default;
		IsZombie = false;
	}

	private SeedType(ZombieTypeEnum zombie)
	{
		Plant = default;
		Zombie = zombie;
		IsZombie = true;
	}

	public static SeedType Of(PlantTypeEnum type) => new(type);

	public static SeedType Of(ZombieTypeEnum type) => new(type);

	/// <summary>卡面要实例的实体场景</summary>
	public PackedScene GetScene() => IsZombie
		? ZombieType.Instance.GetZombieScene(Zombie)
		: PlantTypes.Instance.GetScene(Plant);

	/// <summary>卡面文字用的显示名</summary>
	public string GetDisplayName() => IsZombie
		? ZombieType.Instance.GetDisplayName(Zombie)
		: PlantTypes.Instance.GetDisplayName(Plant);
}
