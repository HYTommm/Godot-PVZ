using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Squash : Plants
{
    [Export] public AnimationPlayer Anim_jumpup;
    [Export] public AnimationPlayer Anim_jumpDown;
    [Export] public AnimationPlayer Anim_lookRight;
    [Export] public AnimationPlayer Anim_lookLeft;
    [Export] public AnimationPlayer Anim_idle;
    [Export] private int _damage = 1800;

    private IHitBox _attackHitBox;
    private IHitBox _detectionHitBox;

    private Zombie _targetZombie;
    private Node2D _body;

    private double _timeCount = 0f;

    public override void _Ready()
    {
        base._Ready();

        _attackHitBox = GetNode<IHitBox>("%AttackHitBox");
        _detectionHitBox = GetNode<IHitBox>("%DetectionHitBox");
        _detectionHitBox.HitBoxEntered += OnDetectionHitBoxEntered;

        Anim_lookRight.AnimationFinished += OnLookAnimationFinished;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_timeCount > 0) _timeCount -= delta;
        if (_timeCount <= 0.04 && _timeCount + delta > 0.04)
        {
            DamageZombies();
        }
    }

    public override void _Idle()
    {
        Anim_idle.Play("Squash_idle");
    }

    private void OnDetectionHitBoxEntered(IHitBox hitBox)
    {
        _detectionHitBox.Monitoring = false;
        if (hitBox.AttachedNode is Zombie zombie && zombie.Row == Row)
        {
            GD.Print("Squash.cs:43 OnDetectionHitBoxEntered");
            _targetZombie = zombie;
            LookAtZombie(zombie);
        }
    }

    private void LookAtZombie(Zombie zombie)
    {
        Anim_idle.Stop();
        if (zombie.Position.X > Position.X)
        {
            GD.Print("Squash looking right at zombie! Zombie Row: ", zombie.Row);
            Anim_lookRight.Play("Squash_lookright", 1.0 / 6.0);
        }
        else
        {
            GD.Print("Squash looking left at zombie! Zombie Row: ", zombie.Row);
            Anim_lookLeft.Play("Squash_lookleft", 1.0 / 6.0);
        }
    }

    private void OnLookAnimationFinished(StringName animName)
    {
        GD.Print("Squash look animation finished: " + animName);
        if (animName == "Squash_lookright" || animName == "Squash_lookleft")
        {
            JumpToZombie(_targetZombie);
        }
    }

    private void JumpUp()
    {
        Tween tweenPos = CreateTween();
        tweenPos
            .TweenProperty(this, "position", new Vector2(_targetZombie.Position.X, Position.Y - 120f), 0.27f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private void JumpDown()
    {
        _timeCount = 0.1d;
        Tween tweenPos = CreateTween();
        tweenPos
            .TweenProperty(this, "position", new Vector2(Position.X, Position.Y + 120f), 0.1f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Linear);
    }

    private async void JumpToZombie(Zombie zombie)
    {
        //JumpUp();
        Anim_jumpup.Play("Squash_jumpup", 0.06, 2);
        await ToSignal(GetTree().CreateTimer(0.8f), "timeout");

        //DamageZombies();

        //JumpDown();
        Anim_jumpDown.Play("Squash_jumpdown", 0.06, 5);
        await ToSignal(Anim_jumpDown, "animation_finished");
        MainGame.Instance.Camera.Shake(shakeDirection: new Vector2(0f, -1f), intensity: 0.3f, shackSteps: 1);
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        Visible = false;
        Shadow.Visible = false;

        FreePlant();
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
