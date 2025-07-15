using Godot;
using System;
using System.Collections.Generic;

public partial class ScreendoorZombie : Zombie
{
	public ScreendoorZombie()
	{
		HP = 270;
		MaxHP = 270;
		GD.Print("ScreendoorZombie Constructor");
	}

	public override void Init()
	{
		GD.Print("ScreendoorZombie Init");
	}

	public override void _Ready()
	{
		base._Ready();
		Screendoor Screendoor = new Screendoor(
			GetNode<Sprite2D>("Zombie/Anim_screendoor"), 
			new List<Sprite2D>() {
				GetNode<Sprite2D>("Zombie/Zombie_innerarm_screendoor"),
				GetNode<Sprite2D>("Zombie/Zombie_innerarm_screendoor_hand"),
				GetNode<Sprite2D>("Zombie/Zombie_outerarm_screendoor")
			},
			new List<Sprite2D>(){
				Zombie_outerarm_upper,
				Zombie_outerarm_lower,
				Zombie_outerarm_hand,
				Anim_innerarm1,
				Anim_innerarm2,
				Anim_innerarm3
			} );
		ArmorSystem.AddArmor(Screendoor);
	}
}
