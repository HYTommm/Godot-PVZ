using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class TieZombie : Zombie
{
    /// <summary>
    /// 僵尸状态枚举
    /// </summary>
    public enum ZombieState
    {
        /// <summary>行走状态</summary>
        Walk,

        /// <summary>啃食状态</summary>
        Eat,

        /// <summary>濒死状态（掉头，持续掉血）</summary>
        Dying,

        /// <summary>死亡状态（播放死亡动画）</summary>
        Dead
    }

    [Export] public Abc Abc;

    /// <summary>状态机</summary>
    private StateMachine<ZombieState> _stateMachine;

    /// <summary>上一次的血量（用于检测血量变化）</summary>
    //private int _previousHP;

    // 状态辅助属性
    public ZombieState CurrentState => _stateMachine?.CurrentState ?? ZombieState.Walk;

    // 节点引用 - 动画与显示
    /// <summary>动画播放器</summary>
    [Export] private AnimationPlayer _animation;

    [Export] private AnimationPlayer _animCharred;

    /// <summary>地面节点</summary>
    [Export] public Sprite2D Ground;

    // 身体部件（导出，供编辑器设置）
    [ExportGroup("Zombie Body Parts")]
    [Export] protected Node2D Body;           // 身体

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

    // 粒子节点（导出）
    [ExportGroup("Zombie Particles")]
    [Export] protected GpuParticles2D ZombieArmParticles; // 外臂粒子动画

    [Export] protected GpuParticles2D ZombieHeadParticles; // 头部粒子动画

    // 缓存的子节点，避免重复查找
    // 缓存TextEdit节点，避免多次查找
    private TextEdit _textEdit;

    // 缓存Zombie Node2D节点
    private Node2D _zombieNode2D;

    // 缓存ZombieCharred Node2D节点
    private Node2D _zombieCharredNode2D;

    // 音频
    /// <summary>啃食音效</summary>
    public AudioStreamPlayer EatSound = new();

    /// <summary>是否正在播放啃食音效</summary>
    public bool IsPlayingEatSound = false;

    /// <summary>攻击力/秒</summary>
    public int Attack = 100;

    /// <summary>暂存攻击</summary>
    public double AttackTemp = 0;

    /// <summary>行走速度，默认1.0f，在(640/99)/735到(640/99)/459之间</summary>
    public float WalkSpeed = 1.0f;

    /// <summary>断臂血量线</summary>
    public int CriticalHP1;

    /// <summary>濒死血量线</summary>
    public int CriticalHPLast;

    /// <summary>护甲系统</summary>
    protected ArmorManager ArmorManager { get; } = new();

    // 常量与位置相关
    /// <summary>地面位置</summary>
    public Vector2 ConstGroundPos = new((float)-9.8, 40);

    public Vector2 LastGroundPos;

    // 动画名称属性（子类可重写）
    public virtual string WalkAnimationName => "Zombie_walk";

    public virtual string EatAnimationName => "Zombie_eat";
    public virtual string DeathAnimationName => "Zombie_death";
    public virtual string LawnMowerDeathAnimationName => "LawnMoweredZombie";
    public virtual string CharredAnimationName => "ALL_ANIMS";
    //public virtual string IdleAnimationName => "";
    //public virtual string JumpAnimationName => "";
    //public virtual string RunAnimationName => "";

    private float _baseEatAnimSpeed = 3.0f;

    protected TieZombie()
    {
        //HP = 270;
        //MaxHP = 270;
        Index = -1;
        CriticalHP1 = 180;
        CriticalHPLast = 90;
        GD.Print("Base Zombie Constructor called");
    }

    //public void Init(ZombieTypeEnum zombieTypeEnum)
    //{
    //    switch (zombieTypeEnum)
    //    {
    //        case ZombieTypeEnum.Normal:
    //            ((NormalZombie)this).Init();
    //            break;
    //        case ZombieTypeEnum.Conehead:
    //            ((ConeheadZombie)this).Init();
    //            break;
    //        case ZombieTypeEnum.Buckethead:
    //            ((BucketheadZombie)this).Init();
    //            break;
    //        case ZombieTypeEnum.Screendoor:
    //            ((ScreendoorZombie)this).Init();
    //            break;
    //    }
    //}

    public override void _Ready()
    {
        base._Ready();

        // 初始化血量阶段组件回调

        //HealthStageComponent.BindActionWithIndex(2, _ => DropArm());
        //HealthStageComponent.BindActionWithIndex(1, _ => Dying());
        //HealthStageComponent.BindActionWithIndex(0, hurt => Die(hurt.HurtType));
        HealthStageComponent.Defaults.StageHigh.Action += _ => DropArm();
        HealthStageComponent.Defaults.StageLow.Action += _ => Dying();
        HealthStageComponent.Defaults.StageZero.Action += hurt => Die(hurt.HurtType);
        // 初始化状态机
        _stateMachine = new StateMachine<ZombieState>(ZombieState.Walk);
        _stateMachine.StateChanged += OnZombieStateChanged;

        DefenseHitBox = GetNode<IHitBox>("./DefenseArea");
        AttackHitBox = GetNode<IHitBox>("./AttackArea");

        DefenseHitBox.AttachedNode = this;
        AttackHitBox.AttachedNode = this;

        ZombieArmParticles.Finished += OnEffectsFinished;
        ZombieHeadParticles.Finished += OnEffectsFinished;

        // 获取主游戏节点
        //_zombieNode2D = GetNode<Node2D>("./Zombie");
        _zombieCharredNode2D = GetNodeOrNull<Node2D>("./ZombieCharred");

        LastGroundPos = Ground.Position;
        // 设置僵尸外臂上部的纹理
        Zombie_outerarm_upper.Texture = ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper;

        // 设置僵尸外臂下部可见
        Zombie_outerarm_lower.Visible = true;

        // 设置僵尸外臂手部可见
        Zombie_outerarm_hand.Visible = true;

        // 设置啃食音效
        EatSound.Finished += () => IsPlayingEatSound = false;

        // 将啃食的声音节点添加为子节点
        AddChild(EatSound);
        // 随机设置僵尸的行走速度，并打印出速度
        WalkSpeed = MainGame.Instance.RNG.RandfRange(640.0f / 99 / 735 * 100, 640.0f / 99 / 459 * 100);
        // play walk with effective speed considering status effects
        _animation.Play(WalkAnimationName, customBlend: 1 / 6.0f, customSpeed: WalkSpeed * StatusEffectManager.MovementMultiplier);
        GD.Print("Zombie speed: " + WalkSpeed);

        _animation.AnimationFinished += OnAnimationFinished;
        StatusEffectManager.EffectsChanged += OnEffectsChanged;
        _animCharred.AnimationFinished += OnAnimationFinished;
    }

    /// <summary>
    /// 僵尸状态改变事件处理
    /// </summary>
    private void OnZombieStateChanged(ZombieState newState)
    {
        GD.Print($"Zombie state changed to: {newState}");

        // 更新基类状态字段以反映当前状态
        // 注意：这里直接设置基类的公共字段
        switch (newState)
        {
            case ZombieState.Walk:
                BIsMoving = true;
                BIsDying = false;
                BIsDead = false;
                Move();
                break;

            case ZombieState.Eat:
                BIsMoving = false;
                BIsDying = false;
                BIsDead = false;
                float eatSpeed = _baseEatAnimSpeed * StatusEffectManager.MovementMultiplier;
                _animation.Play(EatAnimationName, 0.2f, eatSpeed);
                break;

            case ZombieState.Dying:
                //this.BIsMoving = false;
                BIsDying = true;
                BIsDead = false;
                EmitSignal("ZombieDying");
                DropHead();
                break;

            case ZombieState.Dead:
                BIsMoving = false;
                BIsDying = false;
                BIsDead = true;
                DefenseHitBox.Monitorable = false;
                if (Index >= 0)
                    MainGame.Instance.RemoveZombieFromStack(this);
                // 注意：具体的死亡动画在 Die() 方法中根据伤害类型播放
                break;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        // update status effects each frame
        StatusEffectManager.Tick(delta);

        // 更新状态机
        _stateMachine?._Process(delta);

        // 根据当前状态处理逻辑
        switch (CurrentState)
        {
            case ZombieState.Walk:
                // 处理移动

                // 检查攻击区域，决定是否切换到啃食状态
                CheckAttackArea(delta);
                break;

            case ZombieState.Eat:
                // 处理啃食攻击
                ProcessEating(delta);
                break;

            case ZombieState.Dying:
                // 濒死状态：持续掉血
                if (Alive)
                {
                    Hurt(new Hurt(damage: 1, hurtType: HurtType.Dying));
                }
                break;

            case ZombieState.Dead:
                // 死亡状态：不处理任何逻辑
                return;
        }

        if (BIsMoving) // 注意：BIsMoving 现在是基于状态的属性
        {
            Vector2 temp = LastGroundPos - Ground.Position;
            if (temp < Vector2.Zero)
                Position += temp;
            LastGroundPos = Ground.Position;
        }
    }

    /// <summary>
    /// 检查攻击区域，决定是否切换到啃食状态
    /// </summary>
    private void CheckAttackArea(double delta)
    {
        if (AttackHitBox == null || BIsDying || BIsDead) return;

        IReadOnlyList<IHitBox> overlappingAreas = AttackHitBox.GetOverlappingHitBox();
        //GD.Print("================Overlapping areas count: " + overlappingAreas.Count);
        if (overlappingAreas.Count == 0)
        {
            // 没有可攻击的植物，切换回行走状态
            if (CurrentState == ZombieState.Eat)
            {
                _stateMachine.ForceSetState(ZombieState.Walk);
            }
            AttackTemp = 0;
            return;
        }

        // 寻找可以攻击的植物
        int maxStack = -1;
        IHitBox attackPlantArea = null;
        Plants attackPlant = null;
        foreach (IHitBox area in overlappingAreas)
        {
            if (area?.AttachedNode is Plants plant
                && plant.Row == Row
                && plant.BIsPlanted
                && plant.Alive)
            {
                if (plant.Index >= maxStack)
                {
                    maxStack = plant.Index;
                    attackPlantArea = area;
                }
            }
        }

        if (attackPlantArea != null &&
            (attackPlant = attackPlantArea.AttachedNode as Plants) != null &&
            attackPlant.Alive &&
            attackPlant.BIsPlanted)
        {
            // 切换到啃食状态
            if (CurrentState != ZombieState.Eat)
            {
                _stateMachine.ForceSetState(ZombieState.Eat);
            }

            // 处理攻击
            AttackTemp += Attack * StatusEffectManager.AttackMultiplier * delta;
            int attackInt = (int)AttackTemp;
            AttackTemp -= attackInt;

            // 对植物造成伤害
            attackPlant.Hurt(new Hurt(damage: attackInt, hurtType: HurtType.Eating));

            // 播放啃食音效
            if (!IsPlayingEatSound) PlayEatSound();
        }
        else if (CurrentState == ZombieState.Eat)
        {
            // 没有可攻击的植物，切回行走状态
            _stateMachine.ForceSetState(ZombieState.Walk);
            AttackTemp = 0;
        }
    }

    /// <summary>
    /// 处理啃食状态逻辑
    /// </summary>
    private void ProcessEating(double delta)
    {
        // 在啃食状态下，持续检查攻击区域
        CheckAttackArea(delta);
    }

    public void ContinueMove(float customBlend = 0) => Move(customBlend);

    public void Move(float customBlend = 0)
    {
        GD.Print("Current Animation: " + _animation.CurrentAnimation);

        // 如果当前动画不是行走动画，则播放行走动画
        if (_animation.CurrentAnimation != WalkAnimationName)
        {
            // 计算有效速度（考虑状态效果），为0时停止移动
            float effectiveSpeed = WalkSpeed * StatusEffectManager.MovementMultiplier;
            if (effectiveSpeed <= 0f)
            {
                this.BIsMoving = false;
                return;
            }

            // 播放速度为有效速度
            _animation.Play(WalkAnimationName, customBlend: customBlend, customSpeed: effectiveSpeed);

            // 继续移动
            this.BIsMoving = true;
        }
    }

    private void OnEffectsChanged()
    {
        GD.Print("Status effects changed. Current effects: " + string.Join(", ", StatusEffectManager.ActiveEffects.Select(e => e.ToString())));
        if (StatusEffectManager.HasEffect(StatusEffectTypeEnum.Slow))
        {
            (Body.Material as ShaderMaterial)?.SetShaderParameter("modulate_color", Color.Color8(75, 75, 255, 255));
        }
        else
        {
            (Body.Material as ShaderMaterial)?.SetShaderParameter("modulate_color", Color.Color8(128, 128, 128, 128));
        }
        UpdateAnimationSpeeds();
    }

    private void UpdateAnimationSpeeds()
    {
        // Immediately update current animation playback speed according to status effects
        if (_animation == null) return;

        string current = _animation.CurrentAnimation;
        float movementMul = StatusEffectManager.MovementMultiplier;

        if (current == WalkAnimationName)
        {
            float effectiveSpeed = WalkSpeed * movementMul;
            if (effectiveSpeed <= 0f)
            {
                // pause to keep current position
                try { _animation.Pause(); } catch { }
                // 注意：不设置 BIsMoving 字段，状态仍然是 Walk
                return;
            }

            // resume or update play speed without resetting animation position
            try { _animation.Play(WalkAnimationName, customBlend: 0f, customSpeed: effectiveSpeed); } catch { }
        }
        else if (current == EatAnimationName)
        {
            float newSpeed = _baseEatAnimSpeed * movementMul;
            if (newSpeed <= 0f)
            {
                try { _animation.Pause(); } catch { }
                return;
            }
            try { _animation.Play(EatAnimationName, customBlend: 0f, customSpeed: newSpeed); } catch { }
        }
    }

    /// <summary>
    /// 啃食植物（兼容旧代码）
    /// </summary>
    /// <param name="attackPlant">目标植物</param>
    /// <param name="attackInt">伤害</param>
    public void Eat(Plants attackPlant, int attackInt)
    {
        // 切换到啃食状态
        if (CurrentState != ZombieState.Eat && CurrentState != ZombieState.Dead)
        {
            _stateMachine.ForceSetState(ZombieState.Eat);
        }

        attackPlant.Hurt(new Hurt(damage: attackInt, hurtType: HurtType.Eating));
        if (!IsPlayingEatSound) PlayEatSound(); // 播放啃食音效
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        try { StatusEffectManager.EffectsChanged -= UpdateAnimationSpeeds; } catch { }
    }

    public virtual void PlayEatSound()
    {
        IsPlayingEatSound = true;
        uint random = GD.Randi() % 3; // 随机播放啃食音效
        EatSound.Stream = random switch
        {
            0 => ResourceDB.Sounds.Sound_Chomp,
            1 => ResourceDB.Sounds.Sound_Chomp2,
            2 => ResourceDB.Sounds.Sound_ChompSoft,
            _ => EatSound.Stream
        };

        EatSound.Play();
    }

    /// <summary>
    /// 受伤
    /// </summary>
    /// <param name="hurt"></param>
    public override void Hurt(Hurt hurt)
    {
        if (CurrentState == ZombieState.Dead) return;

        //(this as IHealthStage).Hurt(hurt);
        // 保存伤害前的血量
        //int previousHP = HP;

        ArmorManager.ProcessDamage(hurt);
        GD.Print("Damage: " + hurt.Damage);

        if (hurt.HurtType == HurtType.LawnMower)
        {
            Die(HurtType.LawnMower);
            return;
        }

        //int damage = Math.Min(hurt.Damage, HP);
        //HP -= damage;
        //hurt.Damage -= damage;

        // 调用血量阶段组件
        HealthStageComponent?.TakeDamage(hurt);

        // 更新记录的血量
        //_previousHP = HP;

        // 状态转换：检查是否死亡
        //if (HP <= 0)
        //{
        //    Die(hurt.HurtType); // 死亡
        //}

        MainGame.Instance.UpdateZombieHP();
    }

    /// <summary>
    /// 断臂
    /// </summary>
    public void DropArm()
    {
        //Zombie_outerarm_upper
        Zombie_outerarm_upper.Texture = ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper2;
        //Zombie_outerarm_lower
        Zombie_outerarm_lower.Visible = false;
        //Zombie_outerarm_hand
        Zombie_outerarm_hand.Visible = false;
        // 播放断臂粒子
        ZombieArmParticles.SetDeferred("emitting", true);
        ActiveEffectsCount++;
    }

    /// <summary>
    /// 濒死（兼容旧代码）
    /// </summary>
    public void Dying()
    {
        // 切换到濒死状态
        if (CurrentState != ZombieState.Dying && CurrentState != ZombieState.Dead)
        {
            _stateMachine.ForceSetState(ZombieState.Dying);
        }
    }

    /// <summary>
    /// 掉头
    /// </summary>
    public void DropHead()
    {
        Zombie_head.Visible = false; // 隐藏头部
        Zombie_jaw.Visible = false; // 隐藏下巴
        Zombie_hair.Visible = false; // 隐藏头发
        Zombie_tongue?.Visible = false; //隐藏舌头
        // 播放掉头粒子
        ZombieHeadParticles.Emitting = true;
        ActiveEffectsCount++;
    }

    public void Die(HurtType hurtType = HurtType.Direct)
    {
        // 切换到死亡状态
        if (CurrentState != ZombieState.Dead)
        {
            _stateMachine.ForceSetState(ZombieState.Dead);
        }

        // 根据伤害类型播放死亡动画
        GD.Print("Zombie Die");
        if (hurtType == HurtType.LawnMower)
        {
            GD.Print("LawnMower");
            _animation.Stop();
            _animation.Play(LawnMowerDeathAnimationName, 1.0 / 6.0);
            IsAnimationPlaying = true;
        }
        else if (hurtType is HurtType.AshExplosion or HurtType.Explosion or HurtType.Squash)
        {
            GetNode<Sprite2D>("./Shadow").Visible = false;
            Body.Visible = false;
            if (hurtType == HurtType.AshExplosion && _zombieCharredNode2D != null)
            {
                _zombieCharredNode2D.Visible = true;
                _animCharred.Play(CharredAnimationName, 1.0 / 6.0);
                IsAnimationPlaying = true;
            }
            else
            {
                IsAnimationPlaying = false;
            }
        }
        else
        {
            _animation.Play(DeathAnimationName, 1.0 / 6.0);
            IsAnimationPlaying = true;
        }
    }

    public void OnEffectsFinished()
    {
        ActiveEffectsCount--;
        if (ActiveEffectsCount == 0 && !IsAnimationPlaying && IsReleaseRequested)
            QueueFree();
    }

    private void OnAnimationFinished(StringName animName)
    {
        IsAnimationPlaying = false;
        GetNode<Sprite2D>("./Shadow").Visible = false;
        // 根据动画名称执行结束后的逻辑
        if (animName == DeathAnimationName)
        {
            Body.Visible = false;
        }
        else if (animName == LawnMowerDeathAnimationName)
        {
            foreach (Node child in Body.GetChildren())
                if (child is Sprite2D sp) sp.Visible = false;

            // 动画结束后触发粒子（根据血量）
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
        else if (animName == CharredAnimationName && _animCharred != null)
        {
            _zombieCharredNode2D.Visible = false;
        }

        if (ActiveEffectsCount == 0 && !IsAnimationPlaying && IsReleaseRequested)
            QueueFree();
    }
}