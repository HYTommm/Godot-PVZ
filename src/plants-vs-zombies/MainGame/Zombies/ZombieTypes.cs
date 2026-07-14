using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ZombieTypeEnum
{
	Normal,
	Conehead,
	Buckethead,
	Screendoor,
	Polevaulter,
	Newspaper,
	Football,
}

public class ZombieType
{
	private readonly Dictionary<ZombieTypeEnum, PackedScene> _zombieScenes = new();

	public ZombieType()
	{
		_zombieScenes.Add(ZombieTypeEnum.Normal, GD.Load<PackedScene>("res://MainGame/Zombies/NormalZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Conehead, GD.Load<PackedScene>("res://MainGame/Zombies/ConeheadZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Buckethead, GD.Load<PackedScene>("res://MainGame/Zombies/BucketheadZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Screendoor, GD.Load<PackedScene>("res://MainGame/Zombies/ScreendoorZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Polevaulter, GD.Load<PackedScene>("res://MainGame/Zombies/PolevaulterZombie/PolevaulterZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Newspaper, GD.Load<PackedScene>("res://MainGame/Zombies/NewspaperZombie/NewspaperZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Football, GD.Load<PackedScene>("res://MainGame/Zombies/FootballZombie/Zombie_football.tscn"));
	}

	public PackedScene GetZombieScene(ZombieTypeEnum zombieType)
	{
		return _zombieScenes[zombieType];
	}
}
