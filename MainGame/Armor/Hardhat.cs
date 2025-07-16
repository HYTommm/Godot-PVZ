using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Hardhat : Armor
{
	public Hardhat(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 100;
		MaxHP = 100;
		Type = ArmorTypeEnum.Primary;
		
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_digger_hardhat.png");
		WearLevelTextures.Add(66, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_digger_hardhat2.png"));
		WearLevelTextures.Add(33, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_digger_hardhat3.png"));
		
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
