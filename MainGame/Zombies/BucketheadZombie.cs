using Godot;
using System;
using System.Collections.Generic;

public partial class BucketheadZombie : Zombie
{
	[Export] public Sprite2D Zombie_bucket;
	public BucketheadZombie()
	{
		HP = 270;
		MaxHP = 270;
		GD.Print("BucketheadZombie Constructor");
	}

	public override void Init()
	{
		GD.Print("BucketheadZombie Init");
	}

	public override void _Ready()
	{
		base._Ready();
		Bucket bucket = new(
			Zombie_bucket,
			[],
			[Zombie_hair]);
		ArmorSystem.AddArmor(bucket);
	}
}
