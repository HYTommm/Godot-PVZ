using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 伤害类型，使用二进制位来表示是否可被对应类型防具防御
public enum HurtType
{
    // 平射子弹伤害
    Direct = 0b0000_0011, // 可以被一类和二类防具防御
    // 投掷物伤害
    Thrown = 0b0001_0001, // 可以被一类防具防御
    // 爆炸伤害
    Explosion = 0b0010_0011, // 可以被一类和二类防具防御
    // 地刺类伤害
    Bomber = 0b0011_0001, // 可以被一类防具防御
    // 其他伤害
    Other
}
public partial class Hurt : Node2D
{
    public int Damage { get; set; }
    public HurtType HurtType { get; set; }

    public Hurt(int damage, HurtType hurtType)
    {
        Damage = damage;
        HurtType = hurtType;
    }

    public Hurt HurtHealthEntity(HealthEntity entity)
    {
        Damage -= entity.Hurt(Damage);
        return this;
    }
}
