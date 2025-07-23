using Godot;

public partial class ScreendoorZombie : Zombie
{
	[Export] public Sprite2D Zombie_innerarm_screendoor;
	[Export] public Sprite2D Zombie_innerarm_screendoor_hand;
	[Export] public Sprite2D Zombie_outerarm_screendoor;

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
		Screendoor screendoor = new(
			GetNode<Sprite2D>("Zombie/Anim_screendoor"),
			[
				Zombie_innerarm_screendoor,
				Zombie_innerarm_screendoor_hand,
				Zombie_outerarm_screendoor
			],
			[
				Zombie_outerarm_upper,
				Zombie_outerarm_lower,
				Zombie_outerarm_hand,
				Anim_innerArm1,
				Anim_innerArm2,
				Anim_innerArm3
			]);
		ArmorSystem.AddArmor(screendoor);
	}
}
