using Godot;
using System;

public partial class SnowPeaShooter : PeaShooterSingle
{
    public override void _Idle()
    {
        AnimIdle.CallDeferred("play", "SnowPea_Idle", -1, SpeedScaleOfIdle);
        AnimHead.CallDeferred("play", "Head_Idle", -1, SpeedScaleOfIdle);
    }
}
