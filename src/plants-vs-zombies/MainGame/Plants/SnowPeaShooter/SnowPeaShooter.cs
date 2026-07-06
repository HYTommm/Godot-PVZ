using Godot;
using System;

public partial class SnowPeaShooter : PeaShooterSingle
{
    public override void _Idle()
    {
        AnimIdle.CallDeferred("play", "SnowPea_Idle", -1, SpeedScaleOfIdle);
        AnimTree.CallDeferred("set", "active", true);
    }
}
