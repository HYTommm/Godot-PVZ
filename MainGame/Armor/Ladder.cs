using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Ladder : Armor
{
	public Ladder(Sprite2D sprite, List<Sprite2D> hideParts) : base(sprite, hideParts)
	{
		HP = 500;
		MaxHP = 500;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder_1.png");
		WearLevelTextures.Add(333, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder_1_damage1.png"));
		WearLevelTextures.Add(166, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder_1_damage2.png"));
		
	}

	public override int Hurt(int damage)
	{
		return base.Hurt(damage);
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放啃食音效
		switch (random)
		{
			case 0:
				Sound.Stream = (AudioStream)GD.Load("res://sounds/shieldhit.ogg");
				break;
			case 1:
				Sound.Stream = (AudioStream)GD.Load("res://sounds/shieldhit2.ogg");
				break;
		}
		Sound.Play();
	}
}
