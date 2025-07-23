using Godot;
using System;
using System.Collections.Generic;

public class ArmorSystem
{
    private readonly Dictionary<ArmorTypeEnum, List<Armor>> _armors = new();

    public ArmorSystem()
    {
        // 使用 foreach 循环枚举 Armor.ArmorType 枚举值，并初始化 _armors 字典
        foreach (ArmorTypeEnum type in Enum.GetValues(typeof(ArmorTypeEnum)))
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
            ProcessArmorType(ArmorTypeEnum.Secondary, hurt);
        }
        // 如果能被一类防具防御
        if (((int)hurt.HurtType & 0b0001) != 0)
        {
            ProcessArmorType(ArmorTypeEnum.Primary, hurt);
        }
        return hurt;
    }

    private Hurt ProcessArmorType(ArmorTypeEnum type, Hurt hurt)
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