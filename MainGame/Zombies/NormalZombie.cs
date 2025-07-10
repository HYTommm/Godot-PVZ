using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class NormalZombie : Zombie
{
	int test = 114514;
	public NormalZombie()
	{
		HP = 270;
		MaxHP = 270;
		test = 1919810;
		GD.Print("NormalZombie Constructor called");
	}

	public override void Init()
	{
		GD.Print("NormalZombie Init called", test);
	}

}
