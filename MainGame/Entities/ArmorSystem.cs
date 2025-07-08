using Godot;
using System;
using System.Collections.Generic;

public partial class ArmorSystem
{
    private Dictionary<Armor.ArmorType, List<Armor>> _armors = new();

    public ArmorSystem()
    {
        // 使用 foreach 循环枚举 Armor.ArmorType 枚举值，并初始化 _armors 字典
        foreach (Armor.ArmorType type in Enum.GetValues(typeof(Armor.ArmorType)))
        {
            _armors[type] = new List<Armor>(); // 初始化空列表
        }
    }

    public void AddArmor(Armor armor)
    {
        _armors[armor.Type].Add(armor);
    }
    
    // 处理伤害
    public Hurt ProcessDamage (Hurt hurt)
    {
        // 如果能被二类防具防御
        if (((int)hurt.HurtType & 0b0010) != 0)
        {
            ProcessArmorType(Armor.ArmorType.Secondary, hurt);
        }
        // 如果能被一类防具防御
        if (((int)hurt.HurtType & 0b0001) != 0)
        {
            ProcessArmorType(Armor.ArmorType.Primary, hurt);
        }
        return hurt;
    }

    private Hurt ProcessArmorType(Armor.ArmorType type, Hurt hurt)
    {
        if (hurt.Damage <= 0)
        {
            return hurt;
        }

        // 遍历该类型的防具
        foreach (Armor armor in _armors[type])
        {
            hurt.HurtHealthEntity(armor);
            if (hurt.Damage <= 0)
            {
                break;
            }
        }
        return hurt;
    }
}