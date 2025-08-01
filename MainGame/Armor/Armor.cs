using System;
using Godot;
//using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public enum ArmorTypeEnum
{
	Primary, // 一类防具
	Secondary, // 二类防具
	Tertiary // 三类防具

}

public abstract partial class Armor : HealthEntity
{
	protected System.Collections.Generic.Dictionary<int, Texture2D> WearLevelTextures = new();
	protected int WearLevel = 0;
	public Sprite2D ArmorSprite;

	public List<Sprite2D> ShowParts;
	public List<Sprite2D> HideParts;
	public ArmorTypeEnum Type { get; set; }

	protected AudioStreamPlayer2D Sound = new();

	protected Armor(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts)
	{
		ArmorSprite = sprite;	
		ShowParts = showParts;
		HideParts = hideParts;
		ArmorSprite.Visible = true;
		ShowParts.ForEach(x => x.Visible = true);
		HideParts.ForEach(x => x.Visible = false);
		ArmorSprite.AddChild(Sound);
	}

	public override void Hurt(Hurt hurt)
	{
        if (HP <= 0)    {    return;    }
        int damage = Math.Min(hurt.Damage, HP);
        HP -= damage;
        hurt.Damage -= damage;

		if (HP <= 0)
		{
            ArmorSprite.SelfModulate = new Color(0, 0, 0, 0);
            ShowParts.ForEach(x => x.Visible = false);
            HideParts.ForEach(x => x.Visible = true);
            PlayParticles();
        }
		if (hurt.BEnableTargetHitSFX)
			PlaySound(hurt);
		SetWearLevel();
	}

	public virtual void PlaySound(Hurt hurt)
	{
		switch (hurt.HurtType)
		{
			case HurtType.Direct:
			case HurtType.Thrown:
				break;
			case HurtType.Explosion:
				return;
		}
	}

    public virtual void PlayParticles()
	{
		Array<Node> particles = ArmorSprite.FindChildren("*", "GPUParticles2D", recursive: true);
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
