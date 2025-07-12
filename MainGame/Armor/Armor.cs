using Godot;
//using Godot.Collections;
using System.Collections.Generic;

public enum ArmorTypeEnum
{
	Primary, // 一类防具
	Secondary, // 二类防具
	Tertiary // 三类防具

}

public abstract partial class Armor : HealthEntity
{
	protected Dictionary<int, Texture2D> WearLevelTextures = new Dictionary<int, Texture2D>();
	protected int WearLevel = 0;
	public Sprite2D ArmorSprite;
	public List<Sprite2D> HideParts = new List<Sprite2D>();
	public ArmorTypeEnum Type { get; set; }

	protected AudioStreamPlayer2D Sound = new AudioStreamPlayer2D();

	public Armor(Sprite2D sprite, List<Sprite2D> hideParts)
	{
		ArmorSprite = sprite;
		HideParts = hideParts;
		hideParts.ForEach(x => x.Visible = false);
		//Sound.VolumeDb -= 6;
		ArmorSprite.AddChild(Sound);
	}

	public override int Hurt(int damage)
	{
		int returnDamage = damage;
		if (damage >= HP)
		{
			if (HP > 0)
			{
				returnDamage = HP;
				ArmorSprite.Visible = false;
				HideParts.ForEach(x => x.Visible = true);
			}
			else
			{
				returnDamage = 0;
			}
			
		}
		if (returnDamage > 0)
		{
			PlaySound();
		}
		HP -= damage;
		SetWearLevel();
		return returnDamage;
	}

	public abstract void PlaySound();

	public void SetWearLevel()
	{
		int index = 0;
		foreach (KeyValuePair<int, Texture2D> level in WearLevelTextures)
		{
			index++;
			GD.Print("index: " + index , " level: " + level.Key, " HP: " + HP);
			if (HP < level.Key)
			{
				GD.Print("当前已进入: " + level.Key , "WearLevel: " + WearLevel, " index: " + index);
				if (WearLevel < index)
				{
					GD.Print("更换装备" + level.Value);
					WearLevel = index;
					ArmorSprite.Texture = level.Value;
				}
			}
		}
	}
}
