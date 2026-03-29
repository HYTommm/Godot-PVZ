using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NativeHitBox : Area2D, IHitBox
{
    public event Action<IHitBox> HitBoxEntered;

    public event Action<IHitBox> HitBoxExited;

    private bool _isDirty = true;
    private readonly List<IHitBox> _overlappingHitBoxes = new List<IHitBox>();

    public new bool Monitoring
    {
        get => base.Monitoring;
        set { SetDeferred("monitoring", value); _isDirty = true; }
    }

    public new bool Monitorable
    {
        get => base.Monitorable;
        set { SetDeferred("monitorable", value); _isDirty = true; }
    }

    public new Vector2 GlobalPosition => base.GlobalPosition;

    [Export] public NodePath AttachedNodePath { get; set; }
    public Node AttachedNode { get; set; }

    public IReadOnlyList<IHitBox> GetOverlappingHitBox()
    {
        if (_isDirty)
        {
            _overlappingHitBoxes.Clear();
            foreach (Area2D area in GetOverlappingAreas())
                if (area is IHitBox hitBox) _overlappingHitBoxes.Add(hitBox);
            _isDirty = false;
        }
        return _overlappingHitBoxes;
    }

    public override void _Ready()
    {
        base._Ready();
        AttachedNode = GetNode<Node>(AttachedNodePath);
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
    }

    private void OnAreaEntered(Area2D area)
    {
        HitBoxEntered?.Invoke(area as IHitBox);
        _isDirty = true;
    }

    private void OnAreaExited(Area2D area)
    {
        HitBoxExited?.Invoke(area as IHitBox);
        _isDirty = true;
    }
}
