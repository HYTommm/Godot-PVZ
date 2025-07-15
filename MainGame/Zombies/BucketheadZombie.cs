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
		Bucket Bucket = new Bucket(
			GetNode<Sprite2D>("Zombie/Anim_bucket"),
			new List<Sprite2D>(),
			new List<Sprite2D>() { Zombie_hair });
		ArmorSystem.AddArmor(Bucket);
	}
}
