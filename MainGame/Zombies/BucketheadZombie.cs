using Godot;
using System;
using System.Collections.Generic;

public partial class BucketheadZombie : Zombie
{
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
			GetNode<Sprite2D>("Zombie/Anim_bucket"),
            [],
            [Zombie_hair]);
		ArmorSystem.AddArmor(bucket);
	}
}
