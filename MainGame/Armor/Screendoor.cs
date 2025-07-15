using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Screendoor : Armor
{
	public Screendoor(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 1100;
		MaxHP = 1100;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor1.png");
		WearLevelTextures.Add(733, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor2.png"));
		WearLevelTextures.Add(366, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor3.png"));
		
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
