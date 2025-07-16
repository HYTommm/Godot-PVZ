using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Newspaper : Armor
{
	public Newspaper(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 150;
		MaxHP = 150;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper1.png");
		WearLevelTextures.Add(100, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper2.png"));
		WearLevelTextures.Add(50, GD.Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper3.png"));
		
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
				break;
			case 1:
				break;
		}
		Sound.Play();
	}
}
