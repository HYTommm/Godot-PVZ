using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ResourceManager.Sounds;
using static ResourceManager.Images.Zombies.Armors;

public partial class Hardhat : Armor
{
	public Hardhat(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 100;
		MaxHP = 100;
		Type = ArmorTypeEnum.Primary;
		
		sprite.Texture =          ImageZombieArmor_DiggerHardhat1;
		WearLevelTextures.Add(66, ImageZombieArmor_DiggerHardhat2);
		WearLevelTextures.Add(33, ImageZombieArmor_DiggerHardhat3);
		
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放音效
        Sound.Stream = random switch
        {
            0 => Sound_PlasticHit,
            1 => Sound_PlasticHit2,
            _ => Sound.Stream
        };
        Sound.Play();
	}
}
