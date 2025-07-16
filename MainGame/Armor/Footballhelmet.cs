using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Footballhelmet : Armor
{
	public Footballhelmet(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 1400;
		MaxHP = 1400;
		Type = ArmorTypeEnum.Primary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_football_helmet.png");
		WearLevelTextures.Add(933, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_football_helmet2.png"));
		WearLevelTextures.Add(466, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_football_helmet3.png"));
		
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
				Sound.Stream = (AudioStream)GD.Load("res://sounds/plastichit.ogg");
				break;
			case 1:
				Sound.Stream = (AudioStream)GD.Load("res://sounds/plastichit2.ogg");
				break;
		}
		Sound.Play();
	}
}
