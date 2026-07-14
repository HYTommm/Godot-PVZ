using Godot;
using System;
using System.Collections.Generic;

public partial class FootballZombie : RegularZombie
{
	[Export] public Sprite2D Zombie_Football;

	public override string WalkAnimationName => "Zombie_football_walk";

	public FootballZombie()
	{
		//HP = 270;
		//MaxHP = 270;
		GD.Print("FootballZombie Constructor");
	}

	public override void Init()
	{
		GD.Print("FootballZombie Init");
	}

	public override void _Ready()
	{
		base._Ready();
		FootballHelmet footballhelmet = new(
			Zombie_Football,
			[],
			[Zombie_hair]);
		ArmorManager.AddArmor(footballhelmet);
	}
}
