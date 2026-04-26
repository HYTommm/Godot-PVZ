using Godot;
using static ResourceDB.Images.Zombies.Armors;
using System.Collections.Generic;

public partial class Newspaper : Armor
{
    public Newspaper(Sprite2D sprite, List<Sprite2D> showParts, List<Sprite2D> hideParts) : base(sprite, showParts, hideParts)
    {
        //HP = 150;
        //MaxHP = 150;
        Type = ArmorTypeEnum.Secondary;
        sprite.Texture = ImageZombieArmor_Paper1;
        //WearLevelTextures.Add(100, ImageZombieArmor_Paper2);
        //WearLevelTextures.Add(50, ImageZombieArmor_Paper3);

        //HealthStageComponent.BindActionWithIndex(2, _ => ArmorSprite.Texture = ImageZombieArmor_Paper2);
        //HealthStageComponent.BindActionWithIndex(1, _ => ArmorSprite.Texture = ImageZombieArmor_Paper3);
        //HealthStageComponent.BindActionWithIndex(0, Die);

        HealthStageComponent.Defaults.StageHigh.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Paper2;
        HealthStageComponent.Defaults.StageLow.Action += _ => ArmorSprite.Texture = ImageZombieArmor_Paper3;
        HealthStageComponent.Defaults.StageZero.Action += Die;
    }

    public override HealthStageComponent HealthStageComponent { get; set; } = new(150);

    public override void PlaySound(Hurt hurt)
    {
    }
}