using Godot;
using System.Collections.Generic;
using static ResourceManager.Sounds;
using static ResourceManager.Images.Zombies.Armors;

public partial class Ladder : Armor
{
	public Ladder(Sprite2D sprite,List <Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 500;
		MaxHP = 500;
		Type = ArmorTypeEnum.Secondary;
		sprite.Texture =           ImageZombieArmor_Ladder1_Damage0;
		WearLevelTextures.Add(333, ImageZombieArmor_Ladder1_Damage1);
		WearLevelTextures.Add(166, ImageZombieArmor_Ladder1_Damage2);
		
	}

	public override void PlaySound()
	{
		uint random = GD.Randi() % 2; // 随机播放音效
		switch (random)
		{
			case 0:
				Sound.Stream = Sound_ShieldHit;
				break;
			case 1:
				Sound.Stream = Sound_ShieldHit2;
				break;
		}
		Sound.Play();
	}
}
