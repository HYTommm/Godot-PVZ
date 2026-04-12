using Godot;

public partial class NormalZombie : Zombie
{
	public NormalZombie()
	{
		HP = 270;
		MaxHP = 270;
	}

	public override void Init()
	{
		GD.Print("NormalZombie Init called");
	}

}
