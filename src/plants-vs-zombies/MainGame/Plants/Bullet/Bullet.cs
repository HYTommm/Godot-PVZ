using Godot;
using static ResourceDB.Sounds;
using System;

public abstract partial class Bullet : Node2D
{
    [Export] public float ShadowPositionY = 0.0f; // 子弹的阴影位置

    private bool _bisDisappear = false; // 这个变量用来判断子弹是否消失

    [Export] protected Sprite2D BulletSprite2D; // 子弹的主体
    [Export] protected Sprite2D Shadow; // 子弹的阴影
    [Export] protected GpuParticles2D GpuParticles; // 子弹的粒子效果
    [Export] protected HurtType HurtType = HurtType.Direct; // 子弹伤害类型
    protected readonly AudioStreamPlayer BulletSplatsSound = new(); // 子弹的爆炸声音

    protected IHitBox HitBox; // 子弹的碰撞检测区域

    public int Damage = 20; // 子弹伤害

    /// <summary>
    /// 击中时是否打溅射粒子。子类关掉它就能单看子弹碰撞本身的开销，
    /// 量粒子在总开销里占多少。
    /// </summary>
    protected virtual bool BEnableSplats => true;

    public override void _Ready()
    {
        // Get Nodes
        HitBox = GetNode<IHitBox>("%HitBox");
        HitBox.AttachedNode = this;
        // Set Signals
        HitBox.HitBoxEntered += OnHitBoxEntered;

        if (MainGame.BCollisionOnlyTest)
        {
            // 纯碰撞测试：粒子节点与音效播放器都不建。它们是每颗子弹各一份，
            // 射速一高就是几百份挂在场上
            if (GpuParticles != null)
            {
                RemoveChild(GpuParticles);
                GpuParticles.QueueFree();
                GpuParticles = null;
            }
        }
        else
        {
            AddChild(BulletSplatsSound); // 添加子弹的爆炸声音节点
        }

        Shadow.GlobalPosition = new Vector2(GlobalPosition.X, ShadowPositionY); // 设置子弹的阴影位置
    }

    public override void _ExitTree()
    {
        HitBox.HitBoxEntered -= OnHitBoxEntered; // 断开信号连接，防止内存泄漏
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        if (!_bisDisappear)
        {
            //子弹向右移动
            Position += new Vector2(333 * (float)delta, 0);
            //判断子弹是否超出屏幕范围
            if (GlobalPosition.X > 1000)
            {
                QueueFree();
            }
        }
    }

    public void OnHitBoxEntered(IHitBox hitBox)
    {
        if (_bisDisappear)
        {
            if (MainGame.BEnableDebugPrint)
            {
                Log.Trace("Bullet has already disappeared!");
            }
            return;
        }

        if (MainGame.BEnableDebugPrint)
        {
            Log.Trace($"碰撞箱类型: {hitBox.GetType()}");
            Log.Trace($"AttachedNode: {hitBox.AttachedNode}");
            if (hitBox.AttachedNode != null)
            {
                Log.Trace($"AttachedNode Name: {hitBox.AttachedNode.Name}");
                Log.Trace($"AttachedNode IsValid: {IsInstanceValid(hitBox.AttachedNode)}");
            }
            else
            {
                Log.Trace("AttachedNode 为 null");
            }
        }

        if (!IsInstanceValid(hitBox.AttachedNode))
        {
            if (MainGame.BEnableDebugPrint)
            {
                Log.Trace("碰撞箱的 AttachedNode 已失效，忽略本次碰撞");
            }
            return;
        }

        //GD.Print("Bullet collided with " + hitBox.Name);
        _bisDisappear = true; // 子弹消失

        BulletSprite2D.Visible = false; // 子弹不可见
        Shadow.Visible = false; // 子弹阴影不可见
        //HitBox.SetDeferred("monitoring", false); // 停止检测子弹碰撞
        HitBox.Monitoring = false; // 停止检测子弹碰撞

        // 判断子弹是否击中僵尸
        if (MainGame.BEnableDebugPrint)
        {
            Log.Trace("Bullet collided with " + hitBox.AttachedNode.GetPath());
        }

        if (hitBox.AttachedNode is Zombie zombie)
        {
            AttackZombie(zombie);
        }
        else if (MainGame.BEnableDebugPrint)
        {
            Log.Trace("子弹没有击中僵尸！它可能击中了其他东西：" + hitBox.AttachedNode.Name);
        }
    }

    public virtual async void AttackZombie(Zombie zombie)
    {
        if (MainGame.BEnableDebugPrint)
        {
            Log.Trace("Bullet hit zombie");
        }
        //僵尸扣血
        zombie.Hurt(new Hurt(Damage, HurtType));
        //zombie.Die();
        //是sprite节点不可见

        if (MainGame.BCollisionOnlyTest)
        {
            // 纯碰撞测试：不播溅射音、不建等粒子放完的那个定时器，命中即销毁
            QueueFree();
            return;
        }

        if (BEnableSplats)
        {
            GpuParticles.SetDeferred("emitting", true);
        }
        PlaySplatSound();
        // 延迟0.4秒后销毁子弹
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }

    public abstract void PlaySplatSound();
}
