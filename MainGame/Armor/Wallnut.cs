using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Wallnuthead : Armor
{
	public Wallnuthead(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 1100;
		MaxHP = 1100;
		Type = ArmorTypeEnum.Primary;
		sprite.Texture =           GD.Load<Texture2D>("res://art/MainGame/Plants/Wallnut/Wallnut_body.png");
		WearLevelTextures.Add(733, GD.Load<Texture2D>("res://art/MainGame/Plants/Wallnut/Wallnut_cracked1.png"));
		WearLevelTextures.Add(366, GD.Load<Texture2D>("res://art/MainGame/Plants/Wallnut/Wallnut_cracked2.png"));
		

	}
	public override void PlaySound()
	{
		
	}
}
