using Godot;
using static ResourceManager.Images.Zombies.Armors;
using System.Collections.Generic;

public partial class Newspaper : Armor
{
	public Newspaper(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 150;
		MaxHP = 150;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           ImageZombieArmor_Paper1;
		WearLevelTextures.Add(100, ImageZombieArmor_Paper2);
		WearLevelTextures.Add(50,  ImageZombieArmor_Paper3);
		
	}

	public override void PlaySound()
	{
		//uint random = GD.Randi() % 2; // 随机播放啃食音效
		//switch (random)
		//{
		//	case 0:
		//		break;
		//	case 1:
		//		break;
		//}
		//Sound.Play();
	}
}
