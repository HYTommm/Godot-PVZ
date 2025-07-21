using Godot;
using static ResourceManager.Sounds;
using static ResourceManager.Images.Zombies.Armors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Bucket : Armor
{
	public Bucket(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
	{
		HP = 1100;
		MaxHP = 1100;
		Type = ArmorTypeEnum.Primary;
		sprite.Texture =           ImageZombieArmor_Bucket1;
		WearLevelTextures.Add(733, ImageZombieArmor_Bucket2);
		WearLevelTextures.Add(366, ImageZombieArmor_Bucket3);
		
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
				Sound.Stream = Sound_ShieldHit;
				break;
			case 1:
				Sound.Stream = Sound_ShieldHit2;
				break;
		}
		Sound.Play();
	}
}
