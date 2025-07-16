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
