using Godot;
using static ResourceManager;
using static ResourceManager.Images.Zombies.Armors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Cone : Armor
{
	public Cone(Sprite2D sprite,List<Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 370;
		MaxHP = 370;
		Type = ArmorTypeEnum.Primary;
		
		sprite.Texture =           ImageZombieArmor_Cone1;
		WearLevelTextures.Add(226, ImageZombieArmor_Cone2);
		WearLevelTextures.Add(113, ImageZombieArmor_Cone3);
		
	}

	public override int Hurt(int damage)
	{
		return base.Hurt(damage);
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放音效
		switch (random)
		{
			case 0:
				Sound.Stream = Sounds.Sound_PlasticHit;
				break;
			case 1:
				Sound.Stream = Sounds.Sound_PlasticHit2;
				break;
		}
		Sound.Play();
	}

}
