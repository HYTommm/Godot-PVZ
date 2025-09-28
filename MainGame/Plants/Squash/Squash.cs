using Godot;
using System;

public partial class Squash : Plants
{
    [Export] public AnimationPlayer Anim_idle;
    public override void _Idle()
    {
        Anim_idle.Play("Squash_idle");
    }
}
