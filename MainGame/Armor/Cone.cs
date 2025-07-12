using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Cone : Armor
{
	public Cone(Sprite2D sprite, List<Sprite2D> hideParts) : base(sprite, hideParts)
	{
		HP = 370;
		MaxHP = 370;
		Type = ArmorTypeEnum.Primary;
		
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone1.png");
		WearLevelTextures.Add(226, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone2.png"));
		WearLevelTextures.Add(113, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone3.png"));
		
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
