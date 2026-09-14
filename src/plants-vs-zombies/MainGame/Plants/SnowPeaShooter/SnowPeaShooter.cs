using Godot;
using System;

public partial class SnowPeaShooter : PeaShooterSingle
{
    public SnowPeaShooter()
    {
        SunCost = 175; // 比普通豌豆射手贵，冷却时间沿用父类设置
    }

    public override void _Idle()
    {
        AnimIdle.CallDeferred("play", "SnowPea_Idle", -1, SpeedScaleOfIdle);
        AnimTree.CallDeferred("set", "active", true);
    }
}
