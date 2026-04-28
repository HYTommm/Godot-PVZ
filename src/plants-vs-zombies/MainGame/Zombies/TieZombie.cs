using Godot;
using Godot.Collections;
using System.Linq;

public partial class TieZombie : RegularZombie
{
    // ── 自定义状态枚举 ──
    private enum TieState
    {
        Walk,
        Eat,
        Dying,
        Dead
    }

    // ── 状态机 ──
    private StateMachine<TieState> _stateMachine;

    // ── 死亡伤害类型暂存 ──
    private HurtType _pendingDeathHurtType = HurtType.Direct;

    protected TieZombie()
    {
        GD.Print("Base Zombie Constructor called");
    }

    public override void _Ready()
    {
        // 状态机必须在 base._Ready 之前初始化
        AddChild(_stateMachine = new StateMachine<TieState>(TieState.Walk));
        _stateMachine.StateChanged += OnTieStateChanged;

        base._Ready();

        // 触发初始状态（Walk），播放行走动画
        _stateMachine.ForceSetState(TieState.Walk);



        // 纹理设置
        Zombie_outerarm_upper.Texture = ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper;
        Zombie_outerarm_lower.Visible = true;
        Zombie_outerarm_hand.Visible = true;
    }

    // ══════════════════════════════════════════
    //  Physics Process
    // ══════════════════════════════════════════

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta); // StatusEffectManager.Tick + 地面补偿

        switch (_stateMachine.CurrentState)
        {
            case TieState.Walk:
                if (base.UpdateWalking(delta))
                    _stateMachine.ForceSetState(TieState.Eat);
                break;

            case TieState.Eat:
                if (!base.UpdateEating(delta))
                    _stateMachine.ForceSetState(TieState.Walk);
                break;

            case TieState.Dying:
                base.UpdateDying(delta);
                break;

            case TieState.Dead:
                break;
        }
    }

    // ══════════════════════════════════════════
    //  状态切换回调
    // ══════════════════════════════════════════

    private void OnTieStateChanged(TieState newState)
    {
        switch (newState)
        {
            case TieState.Walk:
                base.StartWalking();
                break;

            case TieState.Eat:
                base.StartEating();
                break;

            case TieState.Dying:
                base.StartDying();
                break;

            case TieState.Dead:
                base.StartDead(_pendingDeathHurtType);
                break;
        }
    }

    // ══════════════════════════════════════════
    //  死亡相关（重写：先同步状态机）
    // ══════════════════════════════════════════

    public override void StartDying()
    {
        if (BIsDying || BIsDead) return;

        if (_stateMachine.CurrentState != TieState.Dying)
        {
            _stateMachine.ForceSetState(TieState.Dying);
            return;
        }

        base.StartDying();
    }

    public override void StartDead(HurtType hurtType)
    {
        if (BIsDead) return;

        if (_stateMachine.CurrentState != TieState.Dead)
        {
            _pendingDeathHurtType = hurtType;
            _stateMachine.ForceSetState(TieState.Dead);
            return;
        }

        base.StartDead(hurtType);
    }

    // ── 血量阶段回调 ──

    protected override void OnHealthStageHigh()
    {
        base.OnHealthStageHigh();
        Zombie_outerarm_upper.Texture = ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper2;
        ZombieArmParticles.SetDeferred("emitting", true);
        ActiveEffectsCount++;
    }

    protected override void OnDyingStarted()
    {
        base.OnDyingStarted();
        ZombieHeadParticles.Emitting = true;
        ActiveEffectsCount++;
    }

    // ── 死亡处理 ──

    protected override void OnLawnMowerDeathAnimationFinished()
    {
        base.OnLawnMowerDeathAnimationFinished();
    }

    // ── 断臂 / 掉头 ──
}