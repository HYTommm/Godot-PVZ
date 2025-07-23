using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Balloon : Armor
{
	public Balloon(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 20;
		MaxHP = 20;
		Type = ArmorTypeEnum.Primary;
	}

	
	public override void PlaySound()
	{
	}
}
