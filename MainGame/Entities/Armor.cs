using Godot;

public partial class Armor : HealthEntity
{
    public enum ArmorType
    {
        Primary, // 一类防具
        Secondary, // 二类防具
        Tertiary // 三类防具
    }
    public ArmorType Type { get; set; }
    public override int Hurt(int damage)
    {
        int returnDamage = 0;
        if (damage > HP)
        {
            returnDamage = damage - HP;
        }
        HP -= damage;
        return returnDamage;
    }
}