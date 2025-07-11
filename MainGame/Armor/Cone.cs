using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Cone : Armor
{
	public Cone(Sprite2D sprite) : base(sprite)
	{
		HP = 370;
		MaxHP = 370;
		Type = ArmorTypeEnum.Primary;
		
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
		if (r > 0)
		{
			PlaySound();
		}
		return r;
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
