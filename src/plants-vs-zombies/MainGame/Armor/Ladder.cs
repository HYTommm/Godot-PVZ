using Godot;
using System.Collections.Generic;
using static ResourceDB.Sounds;
using static ResourceDB.Images.Zombies.Armors;

public partial class Ladder : Armor
{
    public Ladder(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
    {
        //HP = 500;
        //MaxHP = 500;
        Type = ArmorTypeEnum.Secondary;
        sprite.Texture = ImageZombieArmor_Ladder1_Damage0;
        //WearLevelTextures.Add(333, ImageZombieArmor_Ladder1_Damage1);
        //WearLevelTextures.Add(166, ImageZombieArmor_Ladder1_Damage2);

        //HealthStageComponent.BindActionWithIndex(2, _ => ArmorSprite.Texture = ImageZombieArmor_Ladder1_Damage1);
        //HealthStageComponent.BindActionWithIndex(1, _ => ArmorSprite.Texture = ImageZombieArmor_Ladder1_Damage2);
        //HealthStageComponent.BindActionWithIndex(0, Die);
        HealthStageComponent.Defaults.StageHigh.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Ladder1_Damage1;
        HealthStageComponent.Defaults.StageLow.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Ladder1_Damage2;
        HealthStageComponent.Defaults.StageZero.Action += Die;
    }

    public override HealthStageComponent HealthStageComponent { get; set; } = new(500);

    public override void PlaySound(Hurt hurt)
    {
        base.PlaySound(hurt);
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