using Godot;
using static ResourceDB;
using static ResourceDB.Images.Zombies.Armors;
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

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hurt"></param>
	public override void PlaySound(Hurt hurt)
	{
		base.PlaySound(hurt);
		if (!hurt.BEnableTargetHitSFX)
			return;
		uint random = GD.Randi() % 2; // 随机播放音效
		Sound.Stream = random switch
		{
			0 => Sounds.Sound_PlasticHit,
			1 => Sounds.Sound_PlasticHit2,
			_ => Sound.Stream
		};
		Sound.Play();
	}

}
