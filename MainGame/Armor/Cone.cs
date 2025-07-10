using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Cone : Armor
{
	public Cone(Sprite2D sprite)
	{
		HP = 370;
		MaxHP = 370;
		Type = ArmorTypeEnum.Primary;
		Sprite = sprite;
		WearLevelTextures.Add(2 / 3f, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone2.png"));
		WearLevelTextures.Add(1 / 3f, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone3.png"));
	}

	public override int Hurt(int damage)
	{
		var r = base.Hurt(damage);
		if (HP <= 0)
		{
			Sprite.Visible = false;
		}
		return r;
	}
}
