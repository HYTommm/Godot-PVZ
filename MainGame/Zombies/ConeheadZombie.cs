using Godot;
using System.Collections.Generic;
using System;

public partial class ConeheadZombie : Zombie
{
	public ConeheadZombie()
	{
		HP = 270;
		MaxHP = 270;
		GD.Print("ConeheadZombie Constructor");
	}

	public override void Init()
	{
		GD.Print("ConeheadZombie Init");
	}

	public override void _Ready()
	{
		base._Ready();
		Cone cone = new(
			GetNode<Sprite2D>("Zombie/Anim_cone"),
			[],
			[Zombie_hair]);
		ArmorSystem.AddArmor(cone);
	}
}
