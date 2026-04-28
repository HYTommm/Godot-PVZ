using Godot;
using Godot.Collections;
using System.Collections.Generic;

public abstract partial class RegularZombie : Zombie
{
    // ── 动画 ──
    [Export] protected AnimationPlayer Animation;

    // 焦炭动画
    [Export] protected AnimationPlayer AnimCharred;

    public virtual string CharredAnimationName => "ALL_ANIMS";
    protected Node2D ZombieCharredNode2D;

    public virtual string WalkAnimationName => "Zombie_walk";
    public virtual string EatAnimationName => "Zombie_eat";
    public virtual string DeathAnimationName => "Zombie_death";
    public virtual string LawnMowerDeathAnimationName => "LawnMoweredZombie";

    // ── 移动 ──
    public float WalkSpeed = 1.0f;

    // ── 战斗 ──
    public int Attack = 100;

    protected double AttackTemp = 0;
    private float _baseEatAnimSpeed = 3.0f;

    // ── 地面/影子引用 ──
    [Export] protected Sprite2D Ground { get; set; }

    [Export] protected Sprite2D Shadow { get; set; }
    protected Vector2 LastGroundPos;
    protected static readonly Vector2 ConstGroundPos = new(-9.8f, 40);

    // ── 啃食音效 ──
    private AudioStreamPlayer _eatSound = new();

    private bool _isPlayingEatSound = false;

    // ── 护甲 ──
    protected ArmorManager ArmorManager { get; } = new();

    // ── 血量临界值（用于草坪机死亡粒子判定） ──
    public int CriticalHP1 = 180;

    public int CriticalHPLast = 90;

    // ── 调试 ──
    [Export] private TextEdit _textEdit;

    // ── 可配置的隐藏部件（在编辑器中为每个僵尸单独设置） ──
    [ExportGroup("Body Parts (Hidden on Stage)")]
    [Export]
    private Array<Node2D> _bodyPartsHalfHealth = [];

    [Export]
    private Array<Node2D> _bodyPartsDying = new();

    [ExportGroup("Node References")]
    [Export] protected Node2D Body;           // 身体

    // 节点引用 - 身体部件
    [Export] protected Sprite2D Anim_innerArm1;        // 内臂上部

    [Export] protected Sprite2D Anim_innerArm2;        // 内臂下部
    [Export] protected Sprite2D Anim_innerArm3;        // 内臂手
    [Export] protected Sprite2D Zombie_outerarm_upper; // 外臂上部
    [Export] protected Sprite2D Zombie_outerarm_lower; // 外臂下部
    [Export] protected Sprite2D Zombie_outerarm_hand;  // 外臂手
    [Export] protected Sprite2D Zombie_head;           // 头
    [Export] protected Sprite2D Zombie_jaw;            // 下巴
    [Export] protected Sprite2D Zombie_hair;           // 头发
    [Export] protected Sprite2D Zombie_tongue;         // 舌头

    // 粒子节点
    [ExportGroup("Zombie Particles")]
    [Export] protected GpuParticles2D ZombieArmParticles;  // 外臂粒子动画

    [Export] protected GpuParticles2D ZombieHeadParticles; // 头部粒子动画

    protected RegularZombie()
    {
        Index = -1;
    }

    // ══════════════════════════════════════════
    //  Ready
    // ══════════════════════════════════════════

    public override void _Ready()
    {
        base._Ready();

        // 血量阶段回调
        HealthStageComponent.Defaults.StageHigh.Action += _ => OnHealthStageHigh();
        HealthStageComponent.Defaults.StageLow.Action += _ => Dying();
        HealthStageComponent.Defaults.StageZero.Action += hurt => Die(hurt.HurtType);

        // 碰撞区域
        DefenseHitBox = GetNode<IHitBox>("./DefenseHitBox");
        AttackHitBox = GetNode<IHitBox>("./AttackHitBox");
        DefenseHitBox.AttachedNode = this;
        AttackHitBox.AttachedNode = this;

        // 地面
        LastGroundPos = Ground.Position;

        // 音效
        _eatSound.Finished += () => _isPlayingEatSound = false;
        AddChild(_eatSound);

        // 随机速度
        WalkSpeed = MainGame.Instance.RNG.RandfRange(
            640.0f / 99 / 735 * 100,
            640.0f / 99 / 459 * 100);

        // 信号
        Animation.AnimationFinished += OnAnimationFinished;
        StatusEffectManager.EffectsChanged += OnEffectsChanged;

        // 粒子完成信号
        ZombieArmParticles.Finished += OnEffectsFinished;
        ZombieHeadParticles.Finished += OnEffectsFinished;
        if (AnimCharred != null)
            AnimCharred.AnimationFinished += OnCharredAnimationFinished;
    }

    // ══════════════════════════════════════════
    //  Physics Process
    //  基类仅处理通用逻辑（状态效果、地面补偿）。
    //  子类自行管理状态机并调用对应的行为方法。
    // ══════════════════════════════════════════

    public override void _PhysicsProcess(double delta)
    {
        StatusEffectManager.Tick(delta);

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
    //  公开行为方法 —— 供子类状态机调用
    //  子类在状态切换回调中调用 Start* 方法，
    //  在状态更新中调用 Update* 方法。
    // ══════════════════════════════════════════

    /// <summary>进入行走状态</summary>
    public virtual void StartWalking()
    {
        BIsMoving = true;
        BIsDying = false;
        BIsDead = false;
        Move(0.2f);
    }

    /// <summary>行走状态更新 —— 检测前方是否有植物，返回 true 表示需要切换到 Eat</summary>
    public virtual bool UpdateWalking(double delta)
    {
        if (AttackHitBox == null || BIsDying || BIsDead)
            return false;

        return IsPlantInAttackRange();
    }

    /// <summary>进入啃食状态</summary>
    public virtual void StartEating()
    {
        BIsMoving = false;
        BIsDying = false;
        BIsDead = false;
        float eatSpeed = _baseEatAnimSpeed * StatusEffectManager.MovementMultiplier;
        Animation.Play(EatAnimationName, 0.2f, eatSpeed);
    }

    /// <summary>啃食状态更新 —— 攻击前方植物，返回 true 表示仍有植物可啃</summary>
    public virtual bool UpdateEating(double delta)
    {
        if (AttackHitBox == null || BIsDying || BIsDead)
            return false;

        return ProcessAttackOnPlant(delta);
    }

    /// <summary>进入濒死状态</summary>
    public virtual void StartDying()
    {
        if (BIsDying || BIsDead) return;
        BIsDying = true;
        BIsDead = false;
        EmitSignal("ZombieDying");
        OnDyingStarted();
    }

    /// <summary>濒死状态更新</summary>
    public virtual void UpdateDying(double delta)
    {
        if (Alive)
            Hurt(new Hurt(1, HurtType.Dying));
    }

    /// <summary>进入死亡状态，根据伤害类型播放对应效果</summary>
    public virtual void StartDead(HurtType hurtType = HurtType.Direct)
    {
        if (BIsDead) return;
        BIsMoving = false;
        BIsDying = false;
        BIsDead = true;
        DefenseHitBox.Monitorable = false;
        if (Index >= 0)
            MainGame.Instance.RemoveZombieFromStack(this);

        OnDeathStarted(hurtType);

        Animation.Stop();
        switch (hurtType)
        {
            case HurtType.LawnMower:

                Animation.Play(LawnMowerDeathAnimationName, 1.0 / 6.0);
                IsAnimationPlaying = true;
                OnLawnMowerDeath();
                break;

            case HurtType.AshExplosion:
            case HurtType.Explosion:
            case HurtType.Squash:
                Shadow.Visible = false;
                OnExplosiveDeath(hurtType);
                break;

            default:
                Animation.Play(DeathAnimationName, 1.0 / 6.0);
                IsAnimationPlaying = true;
                break;
        }
    }

    // ══════════════════════════════════════════
    //  子类可重写的扩展点
    // ══════════════════════════════════════════

    /// <summary>血量降至 2/3 以下时调用（隐藏 BodyPartsHalfHealth 中的部件）</summary>
    protected virtual void OnHealthStageHigh()
    {
        foreach (Node2D part in _bodyPartsHalfHealth)
            part.Visible = false;
    }

    /// <summary>进入濒死状态时调用（隐藏 BodyPartsDying 中的部件）</summary>
    protected virtual void OnDyingStarted()
    {
        foreach (Node2D part in _bodyPartsDying)
            part.Visible = false;
    }

    /// <summary>进入死亡状态时调用（TieZombie → 隐藏身体）</summary>
    protected virtual void OnDeathStarted(HurtType hurtType)
    { }

    /// <summary>被小推车杀死时的额外逻辑</summary>
    protected virtual void OnLawnMowerDeath()
    { }

    /// <summary>
    /// 被爆炸/碾压类伤害杀死时的处理。
    /// 子类应隐藏身体视觉并处理焦炭动画。
    /// 若播放了动画，设置 IsAnimationPlaying = true。
    /// </summary>
    protected virtual void OnExplosiveDeath(HurtType hurtType)
    {
        Body.Visible = false;
        if (hurtType == HurtType.AshExplosion && ZombieCharredNode2D != null)
        {
            ZombieCharredNode2D.Visible = true;
            AnimCharred.Play(CharredAnimationName, 1.0 / 6.0);
            IsAnimationPlaying = true;
        }
        else
        {
            IsAnimationPlaying = false;
        }
    }

    /// <summary>普通死亡动画播放完毕时调用</summary>
    protected virtual void OnDeathAnimationFinished()
    {
        Body.Visible = false;
    }

    /// <summary>小推车死亡动画播放完毕时调用</summary>
    protected virtual void OnLawnMowerDeathAnimationFinished()
    {
        foreach (Node child in Body.GetChildren())
            if (child is Sprite2D sp) sp.Visible = false;

        if (HealthStageComponent.HP > CriticalHP1)
        {
            ActiveEffectsCount++;
            ZombieArmParticles.Emitting = true;
        }
        if (HealthStageComponent.HP >= CriticalHPLast)
        {
            ActiveEffectsCount++;
            ZombieHeadParticles.Emitting = true;
        }
    }

    /// <summary>被施加减速效果时调用（TieZombie → 着色器变蓝）</summary>
    protected virtual void OnSlowEffectApplied() => (Body.Material as ShaderMaterial)?.SetShaderParameter("modulate_color", Color.Color8(75, 75, 255, 255));

    /// <summary>减速效果移除时调用</summary>
    protected virtual void OnSlowEffectRemoved() => (Body.Material as ShaderMaterial)?.SetShaderParameter("modulate_color", Color.Color8(128, 128, 128, 128));

    // ══════════════════════════════════════════
    //  攻击检测（内部辅助方法）
    // ══════════════════════════════════════════

    /// <summary>检测攻击范围内是否有植物</summary>
    protected bool IsPlantInAttackRange()
    {
        IReadOnlyList<IHitBox> overlappingAreas = AttackHitBox.GetOverlappingHitBox();
        foreach (IHitBox area in overlappingAreas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>对攻击范围内最靠后的植物造成啃食伤害，返回 true 表示攻击成功</summary>
    protected bool ProcessAttackOnPlant(double delta)
    {
        IReadOnlyList<IHitBox> overlappingAreas = AttackHitBox.GetOverlappingHitBox();
        if (overlappingAreas.Count == 0)
        {
            AttackTemp = 0;
            return false;
        }

        int maxStack = -1;
        IHitBox attackPlantArea = null;
        foreach (IHitBox area in overlappingAreas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive
                && plant.Index >= maxStack)
            {
                maxStack = plant.Index;
                attackPlantArea = area;
            }
        }

        if (attackPlantArea?.AttachedNode is Plants attackPlant
            && attackPlant.Alive
            && attackPlant.BIsPlanted)
        {
            AttackTemp += Attack * StatusEffectManager.AttackMultiplier * delta;
            int attackInt = (int)AttackTemp;
            AttackTemp -= attackInt;
            attackPlant.Hurt(new Hurt(attackInt, HurtType.Eating));

            if (!_isPlayingEatSound) PlayEatSound();
            return true;
        }

        AttackTemp = 0;
        return false;
    }

    // ══════════════════════════════════════════
    //  移动
    // ══════════════════════════════════════════

    public void ContinueMove(float customBlend = 0) => Move(customBlend);

    public void Move(float customBlend = 0)
    {
        if (Animation.CurrentAnimation != WalkAnimationName)
        {
            float effectiveSpeed = WalkSpeed * StatusEffectManager.MovementMultiplier;
            if (effectiveSpeed <= 0f)
            {
                BIsMoving = false;
                return;
            }
            Animation.Play(WalkAnimationName, customBlend: customBlend, customSpeed: effectiveSpeed);
            BIsMoving = true;
        }
    }

    // ══════════════════════════════════════════
    //  音效
    // ══════════════════════════════════════════

    public virtual void PlayEatSound()
    {
        _isPlayingEatSound = true;
        uint random = GD.Randi() % 3;
        _eatSound.Stream = random switch
        {
            0 => ResourceDB.Sounds.Sound_Chomp,
            1 => ResourceDB.Sounds.Sound_Chomp2,
            2 => ResourceDB.Sounds.Sound_ChompSoft,
            _ => _eatSound.Stream
        };
        _eatSound.Play();
    }

    // ══════════════════════════════════════════
    //  受伤 / 死亡
    // ══════════════════════════════════════════

    public override void Hurt(Hurt hurt)
    {
        if (BIsDead) return;

        ArmorManager.ProcessDamage(hurt);

        if (hurt.HurtType == HurtType.LawnMower)
        {
            Die(HurtType.LawnMower);
            return;
        }

        HealthStageComponent?.TakeDamage(hurt);
        MainGame.Instance.UpdateZombieHP();
    }

    /// <summary>外部/血量回调触发濒死</summary>
    public void Dying()
    {
        if (BIsDying || BIsDead) return;
        StartDying();
    }

    /// <summary>外部/血量回调触发死亡</summary>
    public void Die(HurtType hurtType = HurtType.Direct)
    {
        if (BIsDead) return;
        StartDead(hurtType);
    }

    // ══════════════════════════════════════════
    //  动画 / 粒子结束
    // ══════════════════════════════════════════

    protected virtual void OnAnimationFinished(StringName animName)
    {
        IsAnimationPlaying = false;
        Shadow.Visible = false;

        if (animName == DeathAnimationName)
        {
            OnDeathAnimationFinished();
        }
        else if (animName == LawnMowerDeathAnimationName)
        {
            OnLawnMowerDeathAnimationFinished();
        }
        if (ActiveEffectsCount == 0 && !IsAnimationPlaying && IsReleaseRequested)
            QueueFree();
    }

    public void OnEffectsFinished()
    {
        ActiveEffectsCount--;
        if (ActiveEffectsCount == 0 && !IsAnimationPlaying && IsReleaseRequested)
            QueueFree();
    }

    // ══════════════════════════════════════════
    //  状态效果
    // ══════════════════════════════════════════

    private void OnEffectsChanged()
    {
        if (StatusEffectManager.HasEffect(StatusEffectTypeEnum.Slow))
            OnSlowEffectApplied();
        else
            OnSlowEffectRemoved();
        UpdateAnimationSpeeds();
    }

    protected virtual void UpdateAnimationSpeeds()
    {
        if (Animation == null) return;

        string current = Animation.CurrentAnimation;
        float movementMul = StatusEffectManager.MovementMultiplier;

        if (current == WalkAnimationName)
        {
            float effectiveSpeed = WalkSpeed * movementMul;
            if (effectiveSpeed <= 0f)
            {
                try { Animation.Pause(); } catch { }
                return;
            }
            try { Animation.Play(WalkAnimationName, customBlend: 0f, customSpeed: effectiveSpeed); } catch { }
        }
        else if (current == EatAnimationName)
        {
            float newSpeed = _baseEatAnimSpeed * movementMul;
            if (newSpeed <= 0f)
            {
                try { Animation.Pause(); } catch { }
                return;
            }
            try { Animation.Play(EatAnimationName, customBlend: 0f, customSpeed: newSpeed); } catch { }
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        try { StatusEffectManager.EffectsChanged -= UpdateAnimationSpeeds; } catch { }
    }

    // ── 焦炭动画结束 ──
    protected void OnCharredAnimationFinished(StringName animName)
    {
        ZombieCharredNode2D.Visible = false;
        IsAnimationPlaying = false;
        if (ActiveEffectsCount == 0 && !IsAnimationPlaying && IsReleaseRequested)
            QueueFree();
    }
}