using Godot;
using System;
using System.Collections.Generic;

public partial class Chomper : Plants
{
    [Export] public AnimationPlayer AnimationPlayer;

    private IHitBox _detectionHitBox;
    private IHitBox _defenseHitBox;

    private Zombie _targetZombie;

    public bool CanEat
    {
        get;
        private set
        { if (field = value) CheckZombieInDetectionHitBox(); }
    } = true;

    private Timer _chompTimer;
    private const float CHOMP_INTERVAL = 1.07f; // 啃咬间隔
    private const int CHOMP_DAMAGE = 40; // 每次啃咬伤害

    private enum ChomperState
    {
        Idle,
        Biting,
        Chewing,
        Swallowing,
        Digesting,
        Chomping  // 持续啃咬无法吞食的僵尸
    }

    private StateMachine<ChomperState> _stateMachine;
    //private bool _canEat = true;

    public Chomper()
    {
        SunCost = 150;
        CDtime = CDTime.FAST;
    }

    public override void _Ready()
    {
        base._Ready();

        AddChild(_stateMachine = new StateMachine<ChomperState>(ChomperState.Idle));
        _stateMachine.StateChanged += OnStateChanged;

        // 获取碰撞区域
        _detectionHitBox = GetNode<IHitBox>("%DetectionHitBox");
        _defenseHitBox = GetNode<IHitBox>("%DefenseHitBox");

        _detectionHitBox.Monitoring = false;
        _detectionHitBox.HitBoxEntered += OnDetectionHitBoxEntered;

        // 初始化啃咬计时器
        _chompTimer = new Timer();
        _chompTimer.WaitTime = CHOMP_INTERVAL;
        _chompTimer.OneShot = true;
        _chompTimer.Timeout += OnChompTimeout;
        AddChild(_chompTimer);
    }

    public override void _Plant(int col, int row, int index)
    {
        base._Plant(col, row, index);
        _detectionHitBox.Monitoring = true;
    }

    public override void _Idle()
    {
        AnimationPlayer.Play("Chomper/idle", 20);
    }

    private void CheckZombieInDetectionHitBox()
    {
        GD.Print("Chomper: Checking detection hit box");
        if (_stateMachine.CurrentState != ChomperState.Idle)
        {
            GD.Print("_stateMachine.CurrentState != ChomperState.Idle, _stateMachine.CurrentState = ", _stateMachine.CurrentState);
            return;
        }

        if (!CanEat)
        {
            GD.Print("CanEat == false");
            return;
        }

        GD.Print("Chomper: Zombie detected in detection hit box");

        Zombie target = GetTargetFromDetectionHitBox();
        if (target == null) return;
        _targetZombie = target;

        GD.Print("Chomper enters idle state");
        if (CanSwallowZombie(target))
        {
            _stateMachine.ForceSetState(ChomperState.Biting);
        }
        else
        {
            _stateMachine.ForceSetState(ChomperState.Chomping);
        }
    }

    private void OnDetectionHitBoxEntered(IHitBox hitBox)
    {
        //GD.Print("Detection hitbox entered by: " + (hitBox.AttachedNode != null ? hitBox.AttachedNode.Name : "Unknown"));
        CheckZombieInDetectionHitBox();
    }

    //public override void Hurt(Hurt hurt)
    //{
    //    // 在咀嚼/消化期间，大嘴花可以被僵尸啃食
    //    if (hurt.HurtType == HurtType.Eating && !_canEat)
    //        base.Hurt(hurt);
    //    else if (hurt.HurtType != HurtType.Eating)
    //        base.Hurt(hurt);
    //}

    private void OnStateChanged(ChomperState newState)
    {
        GD.Print($"Chomper state changed: {newState}");
        TextEdit.Text = $"State: {newState}";
        switch (newState)
        {
            case ChomperState.Idle:
                CanEat = true;
                _targetZombie = null;

                if (_stateMachine.CurrentState == ChomperState.Idle)
                {
                    AnimationPlayer.Play("Chomper/idle");
                }
                break;

            case ChomperState.Biting:
                GD.Print("Chomper starts biting");
                CanEat = false;
                AnimationPlayer.Play("Chomper/bite", 0.2, 2f);
                _stateMachine.SetNextState(ChomperState.Chewing, 1f);
                break;

            case ChomperState.Chewing:
                AnimationPlayer.Play("Chomper/chew", 0, 1.25f);
                _stateMachine.SetNextState(ChomperState.Swallowing, 10f);
                break;

            case ChomperState.Swallowing:
                GD.Print("Chomper starts swallowing");
                AnimationPlayer.Play("Chomper/swallow", 0.2, 1f);
                _stateMachine.SetNextState(ChomperState.Idle, 2.25);
                break;

            case ChomperState.Chomping:
                CanEat = false;
                if (_targetZombie is { BIsDead: false })
                {
                    // 立即造成一次伤害
                    _targetZombie.Hurt(new Hurt(CHOMP_DAMAGE, HurtType.Eating));
                    // 启动啃咬计时器
                    _chompTimer.Start();
                }
                else
                {
                    // 没有目标，返回空闲
                    _stateMachine.ForceSetState(ChomperState.Idle);
                }
                break;
        }
    }

    private void DamageZombie()
    {
        if (_targetZombie is { BIsDead: false })
        {
            _targetZombie.Hurt(new Hurt(1800, HurtType.Squash));
        }
    }

    public override void FreePlant()
    {
        base.FreePlant();
        if (_detectionHitBox != null)
            _detectionHitBox.Monitoring = false;
    }

    private bool CanSwallowZombie(Zombie zombie)
    {
        // 暂时假设所有僵尸都可吞食（巨人僵尸和僵王博士未实现）
        return true;
    }

    private Zombie GetTargetFromDetectionHitBox()
    {
        if (_detectionHitBox == null) return null;

        IReadOnlyList<IHitBox> overlapping = _detectionHitBox.GetOverlappingHitBox();
        Zombie theZombie = null;

        int maxStack = -1;

        foreach (IHitBox hitBox in overlapping)
        {
            if (hitBox.AttachedNode is not Zombie zombie || zombie.Row != Row || zombie.BIsDead || zombie.Index <= maxStack)
                continue;
            maxStack = zombie.Index;
            theZombie = zombie;
        }
        //if (theZombie == null)
        //    GD.PrintErr("Chomper: GetTargetFromDetectionHitBox - Zombie is null");
        return theZombie;
    }

    private void OnChompTimeout()
    {
        if (_stateMachine.CurrentState != ChomperState.Chomping) return;
        if (_targetZombie == null || _targetZombie.BIsDead)
        {
            _stateMachine.ForceSetState(ChomperState.Idle);
            return;
        }

        // 造成啃咬伤害
        _targetZombie.Hurt(new Hurt(CHOMP_DAMAGE, HurtType.Eating));

        // 每次撕咬后回到Idle状态，重新检测僵尸
        //_chompTimer.Stop();
        _stateMachine.ForceSetState(ChomperState.Idle);
    }
}