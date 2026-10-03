using Godot;
using System.Collections.Generic;
using System;

public partial class ConeheadZombie : TieZombie
{
    [Export] public Sprite2D Zombie_cone;

    public ConeheadZombie()
    {
        //HP = 270;
        //MaxHP = 270;
        Log.Debug("ConeheadZombie Constructor");
    }

    public override void Init()
    {
        Log.Debug("ConeheadZombie Init");
    }

    public override void _Ready()
    {
        base._Ready();
        Cone cone = new(
            Zombie_cone,
            [],
            [Zombie_hair]);
        ArmorManager.AddArmor(cone);
    }
}