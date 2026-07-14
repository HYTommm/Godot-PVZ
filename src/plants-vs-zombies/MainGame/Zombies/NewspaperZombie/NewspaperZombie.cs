using Godot;

public partial class NewspaperZombie : RegularZombie
{
    private enum PaperState
    {
        Walk,
        Eat,
        Gasp,
        WalkAngry,
        EatAngry,
        Dying,
        Dead
    }

    private StateMachine<PaperState> _stateMachine;
    private HurtType _pendingDeathHurtType = HurtType.Direct;

    // ── 动画名称 ──
    private string WalkAnim => "Zombie_paper/walk";

    private string EatAnim => "Zombie_paper/eat";
    private string WalkAngryAnim => "Zombie_paper/walk_nopaper";
    private string EatAngryAnim => "Zombie_paper/eat_nopaper";
    private string DeathAnim => "Zombie_paper/death";
    private string GaspAnim => "Zombie_paper/gasp";

    // ── 节点引用 ──
    private Sprite2D _newspaperSprite;      // Zombie_paper_paper

    // ── 数值 ──
    private const float AngrySpeedMultiplier = 1.5f;

    private double _attackTemp;
    private const float BaseEatAnimSpeed = 3.0f;
    private bool _hasNoPaper;

    // ── 音效 ──
    private AudioStreamPlayer _eatSound = new();

    private bool _isPlayingEatSound;

    public override void _Ready()
    {
        // 状态机
        AddChild(_stateMachine = new StateMachine<PaperState>(PaperState.Walk));
        _stateMachine.StateChanged += OnStateChanged;

        // 查找节点
        DefenseHitBox = GetNode<IHitBox>("./DefenseHitBox");
        AttackHitBox = GetNode<IHitBox>("./AttackHitBox");
        DefenseHitBox.AttachedNode = this;
        AttackHitBox.AttachedNode = this;

        _newspaperSprite = GetNodeOrNull<Sprite2D>("./Zombie/Zombie_paper_paper");

        // 地面 (来自 Export)
        LastGroundPos = Ground.Position;

        // 血量阶段回调
        HealthStageComponent.Defaults.StageHigh.Action += _ => OnHealthStageHigh();
        HealthStageComponent.Defaults.StageLow.Action += _ => StartDying();
        HealthStageComponent.Defaults.StageZero.Action += hurt => StartDead(hurt.HurtType);

        // 音效
        _eatSound.Finished += () => _isPlayingEatSound = false;
        AddChild(_eatSound);

        // 随机速度
        WalkSpeed = MainGame.Instance.RNG.RandfRange(
            640.0f / 99 / 735 * 100,
            640.0f / 99 / 459 * 100);

        // 信号
        Animation.AnimationFinished += OnAnimationFinished;

        // 添加报纸护甲
        SetupNewspaperArmor();

        // 进入初始状态
        _stateMachine.ForceSetState(PaperState.Walk);
    }

    private void SetupNewspaperArmor()
    {
        if (_newspaperSprite == null) return;

        Newspaper armor = new(
            _newspaperSprite,
            new System.Collections.Generic.List<Sprite2D>(),
            new System.Collections.Generic.List<Sprite2D>());

        // 当报纸被摧毁 → 进入愤怒模式
        armor.HealthStageComponent.Defaults.StageZero.Action += _ => StartAngryMode();

        ArmorManager.AddArmor(armor);
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (_stateMachine.CurrentState)
        {
            case PaperState.Walk:
                UpdateWalk(delta);
                break;

            case PaperState.Eat:
                UpdateEat(delta);
                break;

            case PaperState.Gasp:
                break;

            case PaperState.WalkAngry:
                UpdateWalkAngry(delta);
                break;

            case PaperState.EatAngry:
                UpdateEatAngry(delta);
                break;

            case PaperState.Dying:
                UpdateDying(delta);
                break;

            case PaperState.Dead:
                return;
        }

        // 基类处理: StatusEffect tick + 地面动画位移补偿
        base._PhysicsProcess(delta);
    }

    // ══════════════════════════════════════════
    //  状态切换
    // ══════════════════════════════════════════

    private void OnStateChanged(PaperState newState)
    {
        switch (newState)
        {
            case PaperState.Walk:
                BIsMoving = true; BIsDying = false; BIsDead = false;
                Animation.Play(WalkAnim, 0.2, WalkSpeed);
                break;

            case PaperState.Eat:
                BIsMoving = false; BIsDying = false; BIsDead = false;
                Animation.Play(EatAnim, 0.2, BaseEatAnimSpeed);
                break;

            case PaperState.Gasp:
                BIsMoving = false;
                Animation.Play(GaspAnim, 0.1, 2/3f);
                break;

            case PaperState.WalkAngry:
                BIsMoving = true; BIsDying = false; BIsDead = false;
                Animation.Play(WalkAngryAnim, 0.2, WalkSpeed * AngrySpeedMultiplier);
                break;

            case PaperState.EatAngry:
                BIsMoving = false; BIsDying = false; BIsDead = false;
                Animation.Play(EatAngryAnim, 0.2, BaseEatAnimSpeed * AngrySpeedMultiplier);
                break;

            case PaperState.Dying:
                if (!BIsDying && !BIsDead) { BIsDying = true; EmitSignal("ZombieDying"); }
                break;

            case PaperState.Dead:
                StartDeadState();
                break;
        }
    }

    // ══════════════════════════════════════════
    //  状态更新
    // ══════════════════════════════════════════

    private void UpdateWalk(double delta)
    {
        if (AttackHitBox == null || BIsDying || BIsDead) return;
        if (IsPlantInRange())
            _stateMachine.ForceSetState(PaperState.Eat);
    }

    private void UpdateEat(double delta)
    {
        if (BIsDying || BIsDead) return;
        if (!ProcessAttack(delta))
            _stateMachine.ForceSetState(PaperState.Walk);
    }

    private void UpdateWalkAngry(double delta)
    {
        if (AttackHitBox == null || BIsDying || BIsDead) return;
        if (IsPlantInRange())
            _stateMachine.ForceSetState(PaperState.EatAngry);
    }

    private void UpdateEatAngry(double delta)
    {
        if (BIsDying || BIsDead) return;
        if (!ProcessAttack(delta))
            _stateMachine.ForceSetState(PaperState.WalkAngry);
    }

    public override void UpdateDying(double delta)
    {
        if (Alive)
            Hurt(new Hurt(1, HurtType.Dying));
    }

    // ══════════════════════════════════════════
    //  愤怒模式
    // ══════════════════════════════════════════

    private void StartAngryMode()
    {
        if (_hasNoPaper) return;
        _hasNoPaper = true;

        // 隐藏报纸
        if (_newspaperSprite != null)
            _newspaperSprite.Visible = false;

        _stateMachine.ForceSetState(PaperState.Gasp);
    }

    // ══════════════════════════════════════════
    //  濒死 / 死亡 (覆盖 RegularZombie)
    // ══════════════════════════════════════════

    public override void StartDying()
    {
        if (BIsDying || BIsDead) return;
        if (_stateMachine.CurrentState != PaperState.Dying)
        {
            _stateMachine.ForceSetState(PaperState.Dying);
            return;
        }
        if (BIsDying || BIsDead) return;
        BIsDying = true;
        BIsDead = false;
        EmitSignal("ZombieDying");
    }

    public override void StartDead(HurtType hurtType = HurtType.Direct)
    {
        if (BIsDead) return;
        if (_stateMachine.CurrentState != PaperState.Dead)
        {
            _pendingDeathHurtType = hurtType;
            _stateMachine.ForceSetState(PaperState.Dead);
            return;
        }
        StartDeadState();
    }

    private void StartDeadState()
    {
        if (BIsDead) return;
        BIsMoving = false;
        BIsDying = false;
        BIsDead = true;
        DefenseHitBox.Monitorable = false;
        if (Index >= 0)
            MainGame.Instance.RemoveZombieFromStack(this);

        Animation.Stop();

        HurtType hurtType = _pendingDeathHurtType;
        if (hurtType == HurtType.LawnMower)
        {
            // 没有草坪机专用动画，使用普通死亡
            Animation.Play(DeathAnim, 1.0f / 6.0f);
            IsAnimationPlaying = true;
        }
        else if (hurtType is HurtType.AshExplosion or HurtType.Explosion or HurtType.Squash)
        {
            if (Shadow != null) Shadow.Visible = false;
            IsAnimationPlaying = false;
            QueueFree();
        }
        else
        {
            Animation.Play(DeathAnim, 1.0f / 6.0f);
            IsAnimationPlaying = true;
        }

        RequestRelease();
    }

    // ══════════════════════════════════════════
    //  受伤
    // ══════════════════════════════════════════

    public override void Hurt(Hurt hurt)
    {
        if (BIsDead) return;
        ArmorManager.ProcessDamage(hurt);

        if (hurt.HurtType == HurtType.LawnMower)
        {
            StartDead(HurtType.LawnMower);
            return;
        }

        HealthStageComponent?.TakeDamage(hurt);
        MainGame.Instance.UpdateZombieHP();
    }

    // ══════════════════════════════════════════
    //  攻击检测
    // ══════════════════════════════════════════

    private bool IsPlantInRange()
    {
        var areas = AttackHitBox.GetOverlappingHitBox();
        foreach (var area in areas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive)
                return true;
        }
        return false;
    }

    private bool ProcessAttack(double delta)
    {
        var areas = AttackHitBox.GetOverlappingHitBox();
        if (areas.Count == 0)
        {
            _attackTemp = 0;
            return false;
        }

        int maxStack = -1;
        IHitBox target = null;
        foreach (var area in areas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive
                && plant.Index >= maxStack)
            {
                maxStack = plant.Index;
                target = area;
            }
        }

        if (target?.AttachedNode is Plants attackPlant
            && attackPlant.Alive
            && attackPlant.BIsPlanted)
        {
            _attackTemp += Attack * delta;
            int dmg = (int)_attackTemp;
            _attackTemp -= dmg;
            attackPlant.Hurt(new Hurt(dmg, HurtType.Eating));

            if (!_isPlayingEatSound) PlayEatSound();
            return true;
        }

        _attackTemp = 0;
        return false;
    }

    // ══════════════════════════════════════════
    //  音效
    // ══════════════════════════════════════════

    public override void PlayEatSound()
    {
        _isPlayingEatSound = true;
        uint r = GD.Randi() % 3;
        _eatSound.Stream = r switch
        {
            0 => ResourceDB.Sounds.Sound_Chomp,
            1 => ResourceDB.Sounds.Sound_Chomp2,
            2 => ResourceDB.Sounds.Sound_ChompSoft,
            _ => _eatSound.Stream
        };
        _eatSound.Play();
    }

    // ══════════════════════════════════════════
    //  动画完成
    // ══════════════════════════════════════════

    protected override void OnAnimationFinished(StringName animName)
    {
        if (animName == GaspAnim)
        {
            _stateMachine.ForceSetState(PaperState.WalkAngry);
            return;
        }

        if (animName == DeathAnim)
        {
            IsAnimationPlaying = false;
            if (Shadow != null) Shadow.Visible = false;
            if (IsReleaseRequested)
                QueueFree();
        }
    }

    protected override void OnHealthStageHigh()
    {
        // 可扩展：半血视觉效果
    }

    public override void Init()
    {
        GD.Print("NewspaperZombie Init");
    }
}