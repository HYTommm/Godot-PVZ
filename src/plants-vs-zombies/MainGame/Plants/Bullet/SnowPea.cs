using Godot;
using System;

public partial class SnowPea : Pea
{
    /// <summary>寒冰豌豆走自己的 AttackZombie，粒子不跟着豌豆一起关</summary>
    protected override bool BEnableSplats => true;

    public override async void AttackZombie(Zombie zombie)
    {
        if (MainGame.BEnableDebugPrint)
        {
            Log.Trace("Bullet hit zombie");
        }
        //僵尸扣血
        zombie.Hurt(new Hurt(Damage, HurtType));
        // 施加减速效果
        zombie.AddStatusEffect(new SlowEffect());
        //zombie.Die();
        //是sprite节点不可见

        if (MainGame.BCollisionOnlyTest)
        {
            // 纯碰撞测试：粒子节点在 Bullet._Ready 里已经被清掉了，这里必须提前走，
            // 不然下面 SetDeferred 会碰到空引用
            QueueFree();
            return;
        }

        GpuParticles.SetDeferred("emitting", true);
        PlaySplatSound();
        // 延迟0.4秒后销毁子弹
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
