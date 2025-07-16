using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Tallnuthead : Armor
{
	public Tallnuthead(Sprite2D sprite, List<Sprite2D> hideParts) : base(sprite, hideParts)
	{
		HP = 2700;
		MaxHP = 2700;
		Type = ArmorTypeEnum.Primary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Plants/Tallnut/Tallnut_body.png");
		WearLevelTextures.Add(1800, GD.Load<Texture2D>("res://art/MainGame/Plants/Tallnut/Tallnut_cracked1.png"));
		WearLevelTextures.Add(900, GD.Load<Texture2D>("res://art/MainGame/Plants/Tallnut/Tallnut_cracked2.png"));
		

	}
	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放啃食音效
		switch (random)
		{
			case 0:
				break;
			case 1:
				break;
		}
		Sound.Play();
	}
}
