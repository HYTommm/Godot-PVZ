using Godot;
using Godot.Collections;
//using System.Collections.Generic;

public enum ArmorTypeEnum
{
    Primary, // 一类防具
    Secondary, // 二类防具
    Tertiary // 三类防具

}

public partial class Armor : HealthEntity
{
    protected Dictionary<float, Texture2D> WearLevelTextures = new Dictionary<float, Texture2D>();
    public Sprite2D Sprite;
    public ArmorTypeEnum Type { get; set; }
    public override int Hurt(int damage)
    {
        int returnDamage = damage;
        if (damage > HP)
        {
            returnDamage = HP > 0 ? HP : 0;
        }
        HP -= damage;
        return returnDamage;
    }
}