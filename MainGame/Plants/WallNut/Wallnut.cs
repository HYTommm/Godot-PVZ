using Godot;
using System;
using static ResourceDB.Images.Plants;
public partial class Wallnut : Plants
{
    // Called when the node enters the scene tree for the first time.
    [Export] public AnimationPlayer Anim_idle;
    [Export] public Sprite2D Body;

    // 动画暂停倒计时
    public double AnimPauseTime = 0.0;

    public Wallnut()
    {
        MaxHP = 4000;
        HP = MaxHP;
        SunCost = 50; // 设置花费的阳光数量
        CDtime = CDTime.VERY_SLOW; // 设置冷却时间
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Idle()
    {
        Anim_idle.Play("Wallnut_idle");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (AnimPauseTime > 0)
        {
            AnimPauseTime -= delta;
            if (AnimPauseTime <= 0)
            {
                Anim_idle.Play("Wallnut_idle");
            }
        }
    }

    public override void Hurt(Hurt hurt)
    {
        if (AnimPauseTime <= 0)
        {
            Anim_idle.Pause();
        }
        int damage = Math.Min(hurt.Damage, HP);
        base.Hurt(hurt);
        if (HP is < 2666 and >= 1333 && HP + damage >= 2666)
        {
            Body.Texture = ImagePlant_WallNut_Cracked1;
        }
        else if (HP is < 1333 and > 0 && HP + damage >= 1333)
        {
            Body.Texture = ImagePlant_WallNut_Cracked2;
        }
        TextEdit.Text = "HP: " + HP;
        AnimPauseTime = 0.3;
    }
}
