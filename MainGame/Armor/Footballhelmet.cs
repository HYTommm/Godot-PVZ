using Godot;
using static ResourceManager.Sounds;
using static ResourceManager.Images.Zombies.Armors;
using System.Collections.Generic;

public partial class FootballHelmet : Armor
{
	public FootballHelmet(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) 
		: base(sprite, showParts, hideParts)
	{
		HP = 1400;
		MaxHP = 1400;
		Type = ArmorTypeEnum.Primary;
		sprite.Texture =           ImageZombieArmor_FootballHelmet1;
		WearLevelTextures.Add(933, ImageZombieArmor_FootballHelmet2);
		WearLevelTextures.Add(466, ImageZombieArmor_FootballHelmet3);
		
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放啃食音效
		switch (random)
		{
			case 0:
				Sound.Stream = Sound_PlasticHit;
				break;
			case 1:
				Sound.Stream = Sound_PlasticHit2;
				break;
		}
		Sound.Play();
	}
}
