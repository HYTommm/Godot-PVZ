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

    /// <summary>形状节点与它的矩形，_Ready 时缓存——矩形判定每帧都会问，不能每次都翻节点</summary>
    private CollisionShape2D _shapeNode;
    private RectangleShape2D _rectShape;

    /// <summary>矩形以形状节点的全局位置为中心，尺寸取 RectangleShape2D.Size（它是完整宽高）</summary>
    public Rect2 GlobalRect
    {
        get
        {
            if (_rectShape == null)
            {
                return new Rect2(GlobalPosition, Vector2.Zero);
            }

            Vector2 size = _rectShape.Size;
            return new Rect2(_shapeNode.GlobalPosition - size * 0.5f, size);
        }
    }

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

        foreach (Node child in GetChildren())
        {
            if (child is CollisionShape2D shapeNode)
            {
                _shapeNode = shapeNode;
                _rectShape = shapeNode.Shape as RectangleShape2D;
                if (_rectShape == null)
                {
                    Log.Error($"[NativeHitBox] {Name} 的形状不是矩形，" +
                                "自己算矩形判定时它会被当成零尺寸");
                }
                break;
            }
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        _isDirty = true;
        HitBoxEntered?.Invoke(area as IHitBox);
    }

    private void OnAreaExited(Area2D area)
    {
        _isDirty = true;
        HitBoxExited?.Invoke(area as IHitBox);
    }
}
