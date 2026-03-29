using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using static Godot.GD;

//using PlantsVsZombies;

public partial class Zombie : HealthEntity
{
    /// <summary>僵尸死亡事件</summary>
    [Signal]
    public delegate void ZombieDyingEventHandler();

    // 状态标志
    /// <summary>是否正在移动</summary>
    [Export] public bool BIsMoving = true;

    /// <summary>是否濒死</summary>
    [Export] public bool BIsDying = false;

    /// <summary>是否死亡</summary>
    [Export] public bool BIsDead = false;

    // 节点引用 - 动画与显示
    /// <summary>动画播放器</summary>
    [Export] private AnimationPlayer _animation;

    [Export] private AnimationPlayer _animCharred;

    /// <summary>地面节点</summary>
    [Export] public Sprite2D Ground;

    // 身体部件（导出，供编辑器设置）
    [ExportGroup("Zombie Body Parts")]
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

    // 命中盒/区域
    /// <summary>防御区域节点</summary>
    public IHitBox DefenseHitBox;

    /// <summary>攻击区域节点</summary>
    public IHitBox AttackHitBox;

    // 音频
    /// <summary>啃食音效</summary>
    public AudioStreamPlayer EatSound = new();

    /// <summary>是否正在播放啃食音效</summary>
    public bool IsPlayingEatSound = false;

    // 数值与游戏相关字段
    /// <summary>所处波数</summary>
    public int Wave;

    /// <summary>所处行</summary>
    public int Row = -1;

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
    protected ArmorSystem ArmorSystem { get; } = new();

    // 常量与位置相关
    /// <summary>地面位置</summary>
    public Vector2 ConstGroundPos = new((float)-9.8, 40);

    public Vector2 LastGroundPos;

    // 内部计数与控制
    private int _activeEffectsCount = 0; // 当前活跃的效果（包括动画和粒子）数量

    private bool _isReleaseRequested;
    private bool _isAnimationPlaying;

    public Zombie()
    {
        HP = 270;
        MaxHP = 270;
        Index = -1;
        CriticalHP1 = 180;
        CriticalHPLast = 90;
        GD.Print("Base Zombie Constructor called");
    }

    public virtual void Init()
    {
        GD.Print("Zombie Constructor called");
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

        DefenseHitBox = GetNode<IHitBox>("./DefenseArea");
        AttackHitBox = GetNode<IHitBox>("./AttackArea");

        DefenseHitBox.AttachedNode = this;
        AttackHitBox.AttachedNode = this;

        ZombieArmParticles.Finished += OnEffectsFinished;
        ZombieHeadParticles.Finished += OnEffectsFinished;

        // 获取主游戏节点
        _zombieNode2D = GetNode<Node2D>("./Zombie");
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
        _animation.Play("Zombie_walk", customBlend: 1 / 6.0f, customSpeed: WalkSpeed);
        Print("Zombie speed: " + WalkSpeed);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        if (BIsMoving)
        {
            Vector2 temp = LastGroundPos - Ground.Position;
            if (temp < Vector2.Zero)
                Position += temp;
            LastGroundPos = Ground.Position;
        }
        if (BIsDying && HP > 0)
        {
            //HP -= 1;
            Hurt(new Hurt(damage: 1, hurtType: HurtType.Dying));
        }
        //if (HP <= 0 && !BIsDead)
        //{
        //    BIsDead = true;
        //    Die();
        //}

        //textEdit.Text = "波数：" + Wave.ToString() + "，血量：" + HP.ToString() + "栈：" + Index.ToString();
        //GD.Print(Position);
        // 处理攻击区域
        // 如果植物在攻击区域内，则攻击
        if (BIsDead)
            return;
        IReadOnlyList<IHitBox> overlappingAreas = AttackHitBox.GetOverlappingHitBox();
        //Print(overlappingAreas.Count);
        if (AttackHitBox != null && overlappingAreas.Count > 0 && !BIsDying)
        {
            int maxStack = -1;
            IHitBox attackPlantArea = null;
            Plants attackPlant = null;
            foreach (IHitBox area in overlappingAreas)
            {
                // 在这里处理每个重叠的区域
                if (area?.AttachedNode is Plants plant
                    && plant.Row == Row
                    && plant.BIsPlanted
                    && plant.HP > 0)
                {
                    // 处理植物
                    if (plant.Index >= maxStack)
                    {
                        maxStack = plant.Index;
                        //current_max_stack_array_index = overlappingAreas.IndexOf(area);
                        attackPlantArea = area;
                    }
                }
            }

            //Print("Plant HP: ")
            if (attackPlantArea != null &&
                (attackPlant = attackPlantArea.AttachedNode as Plants) != null &&
                attackPlant.HP > 0 &&
                attackPlant.BIsPlanted)
            {
                //Print("Eat Plant");
                AttackTemp += Attack * delta; // 攻击暂存
                int attackInt = (int)AttackTemp; // 攻击整数
                AttackTemp -= attackInt; // 攻击余数
                Eat(attackPlant, attackInt);
            }
            else
            {
                AttackTemp = 0;

                if (_animation.CurrentAnimation == "Zombie_eat")
                {
                    Print(_animation.CurrentAnimation);
                    ContinueMove(customBlend: 1 / 6.0f);
                }
            }
        }
        else
        {
            //Print("Attack :" + AttackHitBox + "BIsDying: " + BIsDying.ToString());
            AttackTemp = 0;
            if (_animation.CurrentAnimation == "Zombie_eat")
            {
                Print(_animation.CurrentAnimation);
                ContinueMove(customBlend: 1 / 6.0f);
            }
        }
    }

    public void ContinueMove(float customBlend = 0) => Move(customBlend);

    public void Move(float customBlend = 0)
    {
        Print("Current Animation: " + _animation.CurrentAnimation);

        // 如果当前动画不是行走动画，则播放行走动画
        if (_animation.CurrentAnimation != "Zombie_walk")
        {
            // 播放速度为WalkSpeed
            _animation.Play("Zombie_walk", customBlend: customBlend, customSpeed: WalkSpeed);

            // 继续移动
            BIsMoving = true;
        }
    }

    /// <summary>
    /// 啃食植物
    /// </summary>
    /// <param name="attackPlant">目标植物</param>
    /// <param name="attackInt">伤害</param>
    public void Eat(Plants attackPlant, int attackInt)
    {
        attackPlant.Hurt(new Hurt(damage: attackInt, hurtType: HurtType.Eating));
        if (!IsPlayingEatSound) PlayEatSound(); // 播放啃食音效

        if (_animation.CurrentAnimation != "Zombie_eat" && _animation.CurrentAnimation != "Zombie_death")
        {
            //播放吃植物动画
            BIsMoving = false;
            _animation.Play("Zombie_eat", 1.0 / 6.0, 3.0f);
            Ground.Position = ConstGroundPos;
        }
    }

    public virtual void PlayEatSound()
    {
        IsPlayingEatSound = true;
        uint random = Randi() % 3; // 随机播放啃食音效
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
        if (BIsDead) return;

        ArmorSystem.ProcessDamage(hurt);
        Print("Damage: " + hurt.Damage);

        if (hurt.HurtType == HurtType.LawnMower)
        {
            Die(HurtType.LawnMower);
            return;
        }

        int damage = Math.Min(hurt.Damage, HP);
        HP -= damage;
        hurt.Damage -= damage;

        if (HP <= CriticalHP1 && HP + damage > CriticalHP1) DropArm(); // 断臂
        if (HP < CriticalHPLast && HP + damage >= CriticalHPLast) Dying(); // 开始濒死
        if (HP <= 0) Die(hurt.HurtType); // 死亡

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
        _activeEffectsCount++;
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void Dying()
    {
        // 设置为濒死状态
        BIsDying = true;
        // 发出死亡信号
        EmitSignal("ZombieDying");
        DropHead(); // 掉头
    }

    /// <summary>
    /// 掉头
    /// </summary>
    public void DropHead()
    {
        Zombie_head.Visible = false; // 隐藏头部
        Zombie_jaw.Visible = false; // 隐藏下巴
        Zombie_hair.Visible = false; // 隐藏头发
        Zombie_tongue.Visible = false; //隐藏舌头
        // 播放掉头粒子
        ZombieHeadParticles.Emitting = true;
        _activeEffectsCount++;
    }

    public void Die(HurtType hurtType = HurtType.Direct)
    {
        BIsDead = true;
        BIsMoving = false;
        DefenseHitBox.Monitorable = false;
        // 从主游戏的僵尸栈中移除自己
        if (Index >= 0)
            MainGame.Instance.RemoveZombieFromStack(this);
        Print("Zombie Die");
        if (hurtType == HurtType.LawnMower)
        {
            Print("LawnMower");
            _animation.Stop();
            _animation.Play("LawnMoweredZombie", 1.0 / 6.0);
        }
        else if (hurtType is HurtType.AshExplosion or HurtType.Explosion)
        {
            _zombieNode2D.Visible = false;
            if (hurtType == HurtType.AshExplosion && _zombieCharredNode2D != null)
            {
                _zombieCharredNode2D.Visible = true;
                _animCharred.Play("ALL_ANIMS", 1.0 / 6.0);
            }
            else
            {
                _isAnimationPlaying = false;
                return;
            }
        }
        else
        {
            _animation.Play("Zombie_death", 1.0 / 6.0);
        }

        _isAnimationPlaying = true;
        _animation.AnimationFinished += OnAnimationFinished;
    }

    public void OnEffectsFinished()
    {
        _activeEffectsCount--;
        if (_activeEffectsCount == 0 && !_isAnimationPlaying && _isReleaseRequested)
            QueueFree();
    }

    /// <summary>
    /// 由外部管理器调用，授权怪物释放自己。
    /// </summary>
    public void RequestRelease()
    {
        if (_isReleaseRequested) return;   // 已请求过，防止重复
        _isReleaseRequested = true;

        if (_activeEffectsCount == 0 && !_isAnimationPlaying) // 没有活跃的效果和动画，可以直接释放
            QueueFree();
        // 否则等待粒子结束时（在 OnParticleFinished 中处理）
    }

    private void OnAnimationFinished(StringName animName)
    {
        _isAnimationPlaying = false;

        // 根据动画名称执行结束后的逻辑
        if (animName == "Zombie_death")
        {
            _zombieNode2D.Visible = false;
        }
        else if (animName == "LawnMoweredZombie")
        {
            foreach (Node child in _zombieNode2D.GetChildren())
                if (child is Sprite2D sp) sp.Visible = false;

            // 动画结束后触发粒子（根据血量）
            if (HP > CriticalHP1)
            {
                _activeEffectsCount++;
                ZombieArmParticles.Emitting = true;
            }
            if (HP >= CriticalHPLast)
            {
                _activeEffectsCount++;
                ZombieHeadParticles.Emitting = true;
            }
        }
        else if (animName == "ALL_ANIMS" && _animCharred != null)
        {
            _zombieCharredNode2D.Visible = false;
        }

        if (_activeEffectsCount == 0 && !_isAnimationPlaying && _isReleaseRequested)
            QueueFree();
    }

    /// <summary>
    /// 刷新僵尸
    /// </summary>
    /// <param name="index"></param>
    /// <param name="scene"></param>
    /// <param name="wave"></param>
    /// <param name="row"></param>
    public void Refresh(int index, Scene scene, int wave, int row)
    {
        HP = MaxHP;
        //Index = index;
        Wave = wave;
        // 随机行

        Row = row;
        // 设置调试信息
        GetNode<TextEdit>("./TextEdit").Text = Index.ToString();
        Position = new Vector2(1050, scene.LawnLeftTopPos.Y + Row * scene.LawnUnitWidth - 35);
    }

    public override void SetZIndex()
    {
        GetParent().MoveChild(this, Index);
        ZIndex = (Row + 1) * 10 + (int)ZIndexEnum.Zombies;
    }
}
