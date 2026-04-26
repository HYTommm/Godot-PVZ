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

public abstract partial class Armor : Entity, IHealthStage
{
    //protected SortedList<int, Texture2D> WearLevelTextures = new();

    public abstract HealthStageComponent HealthStageComponent { get; set; }
    public bool Alive => HealthStageComponent.HP > 0;

    //protected int WearLevel = 0;
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

    protected void Die(Hurt _)
    {
        ArmorSprite.SelfModulate = new Color(0, 0, 0, 0);
        ShowParts.ForEach(x => x.Visible = false);
        HideParts.ForEach(x => x.Visible = true);
        PlayParticles();
    }

    public virtual void Hurt(Hurt hurt)
    {
        ((IHealthStage)this).Hurt(hurt);
        if (hurt.BEnableTargetHitSFX)
            PlaySound(hurt);
        //SetWearLevel();
    }

    public virtual void PlaySound(Hurt hurt)
    {
        switch (hurt.HurtType)
        {
            case HurtType.Direct:
            case HurtType.Thrown:
                GD.Print("Play sound");
                break;

            case HurtType.AshExplosion:
            case HurtType.Explosion:
                GD.Print("AshExplosion, no sound");
                hurt.BEnableTargetHitSFX = false;
                break;
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

    //public void SetWearLevel()
    //{
    //    int index = 0;
    //    foreach (KeyValuePair<int, Texture2D> level in WearLevelTextures)
    //    {
    //        index++;
    //        GD.Print("index: " + index, " level: " + level.Key, " HP: " + HP);
    //        if (HP < level.Key)
    //        {
    //            SetArmorTexture(level.Key, index);
    //        }
    //    }
    //}

    //private void SetArmorTexture(int levelKey, int index)
    //{
    //    GD.Print("当前已进入: " + levelKey, "WearLevel: " + WearLevel, " index: " + index);
    //    if (WearLevel < index)
    //    {
    //        GD.Print("更换装备" + WearLevelTextures[levelKey]);
    //        WearLevel = index;
    //        ArmorSprite.Texture = WearLevelTextures[levelKey];
    //    }
    //}
}
