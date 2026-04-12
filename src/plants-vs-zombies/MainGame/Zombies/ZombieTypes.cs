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
}

public class ZombieType
{
	readonly Dictionary<ZombieTypeEnum, PackedScene> _zombieScenes = new();

	public ZombieType()
	{
		_zombieScenes.Add(ZombieTypeEnum.Normal, GD.Load<PackedScene>("res://MainGame/Zombies/NormalZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Conehead, GD.Load<PackedScene>("res://MainGame/Zombies/ConeheadZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Buckethead, GD.Load<PackedScene>("res://MainGame/Zombies/BucketheadZombie.tscn"));
		_zombieScenes.Add(ZombieTypeEnum.Screendoor, GD.Load<PackedScene>("res://MainGame/Zombies/ScreendoorZombie.tscn"));
	}

	public PackedScene GetZombieScene(ZombieTypeEnum zombieType)
	{
		return _zombieScenes[zombieType];
	}
}
