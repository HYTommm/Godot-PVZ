using Godot;
//using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

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

	public List<Sprite2D> ShowParts = new List<Sprite2D>();
	public List<Sprite2D> HideParts = new List<Sprite2D>();
	public ArmorTypeEnum Type { get; set; }

	protected AudioStreamPlayer2D Sound = new AudioStreamPlayer2D();

	public Armor(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts)
	{
		ArmorSprite = sprite;	
		ShowParts = showParts;
		HideParts = hideParts;
		ArmorSprite.Visible = true;
		ShowParts.ForEach(x => x.Visible = true);
		HideParts.ForEach(x => x.Visible = false);
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
				ArmorSprite.SelfModulate = new Color(0, 0, 0, 0);
				ShowParts.ForEach(x => x.Visible = false);
				HideParts.ForEach(x => x.Visible = true);
				PlayParticles();
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

	public virtual void PlayParticles()
	{
		var particles = ArmorSprite.FindChildren("*", "GPUParticles2D", recursive: true);
		GD.Print("particles count: " + particles.Count);
		if (particles.Count > 0)
		{
			particles.Cast<GpuParticles2D>().ToList().ForEach(x => x.Emitting = true);
		}
	}

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
