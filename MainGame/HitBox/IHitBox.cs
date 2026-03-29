using System;
using System.Collections.Generic;
using Godot;

public interface IHitBox
{
    public event Action<IHitBox> HitBoxEntered;

    public event Action<IHitBox> HitBoxExited;

    public bool Monitorable { get; set; }
    public bool Monitoring { get; set; }

    public Vector2 GlobalPosition { get; }

    public Node AttachedNode { get; set; }

    public IReadOnlyList<IHitBox> GetOverlappingAreas();
}