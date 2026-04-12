using Godot;
using System;

public partial class SnowPea : Pea
{
    public override async void AttackZombie(Zombie zombie)
    {
        GD.Print("Bullet hit zombie");
        //僵尸扣血
        zombie.Hurt(new Hurt(Damage, HurtType));
        // 施加减速效果
        zombie.AddStatusEffect(new SlowEffect());
        //zombie.Die();
        //是sprite节点不可见

        GpuParticles.SetDeferred("emitting", true);
        PlaySplatSound();
        // 延迟0.4秒后销毁子弹
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
