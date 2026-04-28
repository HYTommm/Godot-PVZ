using Godot;
using System.Collections.Generic;

public partial class PolevaulterZombie : RegularZombie
{
    // ── 自定义状态枚举 ──
    private enum PoleState
    {
        Run,
        InVault,
        PostVault,
        Walk,
        Eat,
        Dying,
        Dead
    }

    // ── 状态机 ──
    private StateMachine<PoleState> _stateMachine;

    /// <summary>是否已经跳过</summary>
    public bool IsJumped => _stateMachine.CurrentState != PoleState.Run;

    // ── 跳跃检测碰撞箱 ──
    private IHitBox _detectionHitBox;

    // ── 跳跃参数 ──
    private Plants _vaultTargetPlant;

    private float _vaultStartX;
    private float _vaultEndX;
    private Vector2 _shadowStartPos;

    // ── 死亡伤害类型暂存 ──
    // （StartDead 经状态机回调转发时需要传递 hurtType）
    private HurtType _pendingDeathHurtType = HurtType.Direct;

    // ── 动画名称 ──
    public override string WalkAnimationName => "PolevaulterZombie/walk";

    public override string EatAnimationName => "PolevaulterZombie/eat";
    public override string DeathAnimationName => "PolevaulterZombie/death";

    private string RunAnimationName => "PolevaulterZombie/run";
    public virtual string IdleAnimationName => "PolevaulterZombie/idle";
    public virtual string JumpAnimationName => "PolevaulterZombie/jump";

    public PolevaulterZombie()
    {
        GD.Print("PolevaulterZombie Constructor");
    }

    public override void Init()
    {
        GD.Print("PolevaulterZombie Init");
    }

    // ══════════════════════════════════════════
    //  Ready
    // ══════════════════════════════════════════

    public override void _Ready()
    {
        //_vaultPhase = VaultPhase.PreVault;

        // 状态机必须在 base._Ready 之前初始化，
        // 因为血量回调可能触发 Dying/Die → StartDying/StartDead，
        // 它们需要访问 _stateMachine。
        AddChild(_stateMachine = new StateMachine<PoleState>(PoleState.Run));
        _stateMachine.StateChanged += OnPoleStateChanged;
        ZombieCharredNode2D = GetNodeOrNull<Node2D>("./ZombieCharred");
        base._Ready();

        // 触发初始状态（Run），播放奔跑动画
        _stateMachine.ForceSetState(PoleState.Run);

        // 初始化跳跃检测碰撞箱
        _detectionHitBox = GetNode<IHitBox>("./DetectionHitBox");
        _detectionHitBox.AttachedNode = this;

        // 记录影子初始偏移，跳跃时影子沿地面滑动
        _shadowStartPos = Shadow.Position;
    }

    /// <summary>状态切换回调</summary>
    /// <param name="newState"></param>
    private void OnPoleStateChanged(PoleState newState)
    {
        switch (newState)
        {
            case PoleState.Run:
                BIsMoving = true;
                BIsDying = false;
                BIsDead = false;
                float runSpeed = RunSpeed * StatusEffectManager.MovementMultiplier;
                Animation.Play(RunAnimationName, customBlend: 1 / 6.0f, customSpeed: runSpeed);
                break;

            case PoleState.Walk:
                base.StartWalking();
                break;

            case PoleState.Eat:
                base.StartEating();
                break;

            case PoleState.Dying:
                base.StartDying();
                break;

            case PoleState.Dead:
                base.StartDead(_pendingDeathHurtType);
                break;
        }
    }

    public float RunSpeed { get; set; } = MainGame.Instance.RNG.RandfRange(3.188167f, 3.284778f);

    public override void _PhysicsProcess(double delta)
    {
        StatusEffectManager.Tick(delta);

        switch (_stateMachine.CurrentState)
        {
            case PoleState.Run:
                TryStartVault();
                break;

            case PoleState.InVault:
                UpdateVault(delta);
                break;

            case PoleState.PostVault:
                EndVault();
                break;

            case PoleState.Walk:
                if (base.UpdateWalking(delta))
                    _stateMachine.ForceSetState(PoleState.Eat);
                break;

            case PoleState.Eat:
                if (!base.UpdateEating(delta))
                    _stateMachine.ForceSetState(PoleState.Walk);
                break;

            case PoleState.Dying:
                base.UpdateDying(delta);
                break;

            case PoleState.Dead:
                return; // 死亡状态不再执行地面补偿
        }

        // 地面动画位移补偿
        if (BIsMoving)
        {
            Vector2 temp = LastGroundPos - Ground.Position;
            if (temp < Vector2.Zero)
                Position += temp;
            LastGroundPos = Ground.Position;
        }
    }

    // ══════════════════════════════════════════
    //  攻击检测（重写 CheckAttackArea 的逻辑）
    //  PreVault → TryStartVault；PostVault → 基类逻辑
    // ══════════════════════════════════════════

    /// <summary>
    /// Run 阶段（PreVault）：通过 DetectionHitBox 检测前方是否有植物可以跳过。
    /// </summary>
    private void TryStartVault()
    {
        if (_detectionHitBox == null || BIsDying || BIsDead) return;

        IReadOnlyList<IHitBox> overlappingAreas = _detectionHitBox.GetOverlappingHitBox();
        if (overlappingAreas.Count == 0) return;

        foreach (IHitBox area in overlappingAreas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive)
            {
                _stateMachine.ForceSetState(PoleState.InVault);
                StartVault(plant);

                return;
            }
        }
    }

    // ══════════════════════════════════════════
    //  跳跃逻辑
    // ══════════════════════════════════════════

    /// <summary>
    /// 开始跳跃：播放跳跃动画，计算落点，进入 InVault 阶段。
    /// 跳跃过程中真实坐标不移动，动画子部件负责视觉位移。
    /// </summary>
    private void StartVault(Plants targetPlant)
    {
        //_vaultPhase = VaultPhase.InVault;
        DefenseHitBox.Monitorable = false; // 跳跃过程中不检测攻击
        _vaultTargetPlant = targetPlant;
        _vaultStartX = Position.X;
        _vaultEndX = targetPlant.Position.X - 80; // 落在植物左侧（越过植物）
        BIsMoving = false;

        Animation.Play(JumpAnimationName, customSpeed: 2.0f);
    }

    /// <summary>
    /// 跳跃中更新：影子沿地面滑动到落点。
    /// </summary>
    private void UpdateVault(double delta)    // PERF：UpdateVault(delta)
    {
        double animLength = Animation?.CurrentAnimationLength ?? 0;
        if (animLength <= 0) return;

        double animPos = Animation.CurrentAnimationPosition;
        double progress = Mathf.Clamp(animPos / animLength, 0, 1);

        // 影子沿地面从起始位置插值到落点
        float shadowOffsetX = Mathf.Lerp(0, _vaultEndX - _vaultStartX, (float)progress);
        Shadow.Position = new Vector2(_shadowStartPos.X + shadowOffsetX, _shadowStartPos.Y);

        double moveDelta = (_vaultStartX - _vaultTargetPlant.Position.X - 80) * progress;

        Position = new Vector2(_vaultStartX - (float)moveDelta, Position.Y);
    }

    /// <summary>
    /// 跳跃结束：瞬移坐标至落点，切换到 Walk 状态，重置影子。
    /// </summary>
    private void EndVault()
    {
        //_vaultPhase = VaultPhase.PostVault;
        LastGroundPos = Ground.Position;

        // 重置影子偏移（节点移动后影子作为子节点跟随）
        Shadow.Position = _shadowStartPos;

        Animation.Stop(); // 确保动画停止，防止跳跃动画残留

        Position -= new Vector2(150, 0);
        // 跳跃结束 → 切换到 Walk 状态
        _stateMachine.ForceSetState(PoleState.Walk);

        // 强制动画系统在本帧立即处理 walk 第一帧，保证与位置切换同步
        Animation.Seek(0, true);

        DefenseHitBox.Monitorable = true; // 恢复攻击检测
    }

    // ══════════════════════════════════════════
    //  动画完成
    // ══════════════════════════════════════════

    protected override void OnAnimationFinished(StringName animName)
    {
        if (animName == JumpAnimationName)
        {
            //EndVault();
            _stateMachine.ForceSetState(PoleState.PostVault);
            return; // 跳跃动画不需要走基类的清理逻辑
        }
        base.OnAnimationFinished(animName);
    }

    // ══════════════════════════════════════════
    //  死亡相关（重写：先同步状态机再执行基类逻辑）
    // ══════════════════════════════════════════

    public override void StartDying()
    {
        if (BIsDying || BIsDead) return;

        // 同步状态机到 Dying 状态
        if (_stateMachine.CurrentState != PoleState.Dying)
        {
            _stateMachine.ForceSetState(PoleState.Dying);
            return; // ForceSetState 触发 OnPoleStateChanged(Dying) → base.StartDying()
        }

        base.StartDying();
    }

    private bool IsJimping;

    public override void StartDead(HurtType hurtType)
    {
        if (BIsDead) return;

        if (_stateMachine.CurrentState != PoleState.Dead)
        {
            IsJimping = _stateMachine.CurrentState == PoleState.InVault; // 如果正在跳跃，记录状态以调整死亡表现
            _pendingDeathHurtType = hurtType;
            _stateMachine.ForceSetState(PoleState.Dead);
            return; // ForceSetState 触发 OnPoleStateChanged(Dead) → base.StartDead(hurtType)
        }

        base.StartDead(hurtType);
    }

    protected override void OnHealthStageHigh()
    {
        base.OnHealthStageHigh();
        Zombie_outerarm_upper.Texture = ResourceDB.Images.Zombies.ImageZombie_PolevaulterOuterarmUpper2;
        ZombieArmParticles.SetDeferred("emitting", true);
        ActiveEffectsCount++;
    }

    protected override void OnDyingStarted()
    {
        base.OnDyingStarted();
        ZombieHeadParticles.Emitting = true;
        ActiveEffectsCount++;
    }

    protected override void OnExplosiveDeath(HurtType hurtType)
    {
        if (IsJimping)
        {
            Body.Visible = false;
            IsAnimationPlaying = false;
        }
        else
        {
            base.OnExplosiveDeath(hurtType);
        }
    }

    // ══════════════════════════════════════════
    //  状态效果（重写：处理奔跑动画速度）
    // ══════════════════════════════════════════

    protected override void UpdateAnimationSpeeds()
    {
        if (Animation == null) return;

        string current = Animation.CurrentAnimation;
        float movementMul = StatusEffectManager.MovementMultiplier;

        if (current == RunAnimationName)
        {
            float effectiveSpeed = WalkSpeed * movementMul;
            if (effectiveSpeed <= 0f)
            {
                try { Animation.Pause(); } catch { }
                return;
            }
            try { Animation.Play(RunAnimationName, customBlend: 0f, customSpeed: effectiveSpeed); } catch { }
        }
        else
        {
            // Walk/Eat/Death 等动画由基类处理
            base.UpdateAnimationSpeeds();
        }
    }
}
