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
	public Sprite2D Sprite;
	public ArmorTypeEnum Type { get; set; }

	protected AudioStreamPlayer2D Sound = new AudioStreamPlayer2D();

	public Armor(Sprite2D sprite)
	{
		Sprite = sprite;
		//Sound.VolumeDb -= 6;
		Sprite.AddChild(Sound);
	}

	public override int Hurt(int damage)
	{
		int returnDamage = damage;
		if (damage > HP)
		{
			returnDamage = HP > 0 ? HP : 0;
			Sprite.Visible = false;
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
					Sprite.Texture = level.Value;
				}
			}
		}
	}
}
