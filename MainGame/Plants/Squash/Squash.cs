using Godot;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Squash : Plants
{
    [Export] public AnimationPlayer Anim_main;
    [Export] private int _damage = 1800;

    private IHitBox _defenseHitbox;
    private IHitBox _attackHitBox;
    private IHitBox _detectionHitBox;

    private Zombie _targetZombie;
    private Node2D _body;

    private double _timeCount = 0f;
    private bool _canEat = true;

    private enum SquashState
    {
        Idle,
        LookRight,
        LookLeft,
        JumpUp,
        JumpDown,
        AfterJump,
        Finished
    }

    private StateMachine<SquashState> _stateMachine;

    public override void _Ready()
    {
        base._Ready();

        _defenseHitbox = GetNode<IHitBox>("%DefenseHitBox");
        _attackHitBox = GetNode<IHitBox>("%AttackHitBox");
        _detectionHitBox = GetNode<IHitBox>("%DetectionHitBox");

        _detectionHitBox.Monitoring = false;
        _detectionHitBox.HitBoxEntered += OnDetectionHitBoxEntered;

        AddChild(_stateMachine = new StateMachine<SquashState>(SquashState.Idle));
        _stateMachine.StateChanged += OnStateChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_timeCount > 0)
        {
            _timeCount -= delta;
            if (_timeCount <= 0) DamageZombies();
        }
    }

    public override void _Plant(int col, int row, int index)
    {
        base._Plant(col, row, index);
        _detectionHitBox.Monitoring = true;
    }

    public override void _Idle()
    {
        Anim_main.Play("Squash/Squash_idle");
    }

    private void OnDetectionHitBoxEntered(IHitBox hitBox)
    {
        _detectionHitBox.Monitoring = false;
        if (hitBox.AttachedNode is not Zombie zombie || zombie.Row != Row) return;
        _canEat = false;
        _targetZombie = zombie;
        _stateMachine.ForceSetState(zombie.Position.X > Position.X ? SquashState.LookRight : SquashState.LookLeft);
    }

    public override void Hurt(Hurt hurt)
    {
        if (hurt.HurtType != HurtType.Eating || _canEat) base.Hurt(hurt);
    }

    private void OnStateChanged(SquashState newState)
    {
        GD.Print($"Squash state changed: {newState}");
        switch (newState)
        {
            case SquashState.Idle:
                Anim_main.Play("Squash/Squash_idle");
                break;

            case SquashState.LookRight:
                GD.Print("Squash looking right at zombie! Zombie Row: ", _targetZombie?.Row);
                Anim_main.Play("Squash/Squash_lookright", 0.1, 2);
                _stateMachine.SetNextState(SquashState.JumpUp, 0.8);
                break;

            case SquashState.LookLeft:
                GD.Print("Squash looking left at zombie! Zombie Row: ", _targetZombie?.Row);
                Anim_main.Play("Squash/Squash_lookleft", 0.1, 2);
                _stateMachine.SetNextState(SquashState.JumpUp, 0.8);
                break;

            case SquashState.JumpUp:
                // disable defense while jumping
                _defenseHitbox.Monitorable = false;
                ZIndex = (Row + 1) * 10 + (int)ZIndexEnum.Particle;
                Anim_main.Play("Squash/Squash_jumpup", 0.2, 2);
                _stateMachine.SetNextState(SquashState.JumpDown, 0.95);
                break;

            case SquashState.JumpDown:
                GD.Print("Squash jumping down!");
                _timeCount = 0.05d;
                Anim_main.Play("Squash/Squash_jumpdown", 0, 5);
                _stateMachine.SetNextState(SquashState.AfterJump, 0.1);
                break;

            case SquashState.AfterJump:
                MainGame.Instance.Camera.Shake(shakeDirection: new Vector2(0f, -1f), shakeDuration: 0.10f, intensity: 0.3f, shackSteps: 1);
                _stateMachine.SetNextState(SquashState.Finished, 1);
                break;

            case SquashState.Finished:
                Visible = false;
                Shadow.Visible = false;
                FreePlant();
                break;
        }
    }

    private void JumpDown()
    {
        Tween tweenPosDown = CreateTween();
        tweenPosDown
            .TweenProperty(this, "position", new Vector2(Position.X, Position.Y + 120f), 0.1f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void JumpUp()
    {
        if (_targetZombie == null) return;
        Tween tweenPos = CreateTween();
        tweenPos
            .TweenProperty(this, "position", new Vector2(_targetZombie.Position.X, Position.Y - 120f), 0.3f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void DamageZombies()
    {
        GD.Print("Squash damaging zombies in row: " + Row);
        IReadOnlyList<IHitBox> overlappingAreas = _attackHitBox.GetOverlappingHitBox();
        foreach (IHitBox overlappingArea in overlappingAreas)
        {
            GD.Print("Squash overlapping area: " + overlappingArea.GetType());
            if (overlappingArea.AttachedNode is Zombie zombie && zombie.Row == Row)
            {
                GD.Print("Squash damaging zombie! Row: ", Row, "Zombie Row: ", zombie.Row);
                //僵尸扣血
                zombie.Hurt(new Hurt(_damage, HurtType.Squash));
            }
        }
    }
}