using Godot;
using static ResourceManager.Images.Zombies.Armors;
using static ResourceManager.Sounds;
using System.Collections.Generic;

public partial class Screendoor : Armor
{
	public Screendoor(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 1100;
		MaxHP = 1100;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           ImageZombieArmor_ScreenDoor1;
		WearLevelTextures.Add(733, ImageZombieArmor_ScreenDoor2);
		WearLevelTextures.Add(366, ImageZombieArmor_ScreenDoor3);
		
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放音效
        Sound.Stream = random switch
        {
            0 => Sound_ShieldHit,
            1 => Sound_ShieldHit2,
            _ => Sound.Stream
        };
        Sound.Play();
	}
}
