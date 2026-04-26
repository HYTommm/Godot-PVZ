using Godot;
using static ResourceDB.Sounds;
using static ResourceDB.Images.Zombies.Armors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Bucket : Armor
{
    public Bucket(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
    {
        //HP = 1100;
        //MaxHP = 1100;
        Type = ArmorTypeEnum.Primary;
        sprite.Texture = ImageZombieArmor_Bucket1;
        //WearLevelTextures.Add(733, ImageZombieArmor_Bucket2);
        //WearLevelTextures.Add(366, ImageZombieArmor_Bucket3);

        //HealthStageComponent.BindActionWithIndex(2, _ => ArmorSprite.Texture = ImageZombieArmor_Bucket2);
        //HealthStageComponent.BindActionWithIndex(1, _ => ArmorSprite.Texture = ImageZombieArmor_Bucket3);
        //HealthStageComponent.BindActionWithIndex(0, Die);
        HealthStageComponent.Defaults.StageHigh.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Bucket2;
        HealthStageComponent.Defaults.StageLow.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Bucket3;
        HealthStageComponent.Defaults.StageZero.Action += Die;
    }

    public override sealed HealthStageComponent HealthStageComponent { get; set; } = new(1100);

    public override void PlaySound(Hurt hurt)
    {
        base.PlaySound(hurt);
        if (!hurt.BEnableTargetHitSFX)
            return;
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