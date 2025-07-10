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
}

public class ZombieType
{
	Dictionary<ZombieTypeEnum, PackedScene> zombieScenes = new Dictionary<ZombieTypeEnum, PackedScene>();

	public ZombieType()
	{
		zombieScenes.Add(ZombieTypeEnum.Normal, GD.Load<PackedScene>("res://MainGame/Zombies/NormalZombie.tscn"));
		zombieScenes.Add(ZombieTypeEnum.Conehead, GD.Load<PackedScene>("res://MainGame/Zombies/ConeheadZombie.tscn"));
		zombieScenes.Add(ZombieTypeEnum.Buckethead, zombieScenes[ZombieTypeEnum.Normal]);
	}

	public PackedScene GetZombieScene(ZombieTypeEnum zombieType)
	{
		return zombieScenes[zombieType];
	}
}
