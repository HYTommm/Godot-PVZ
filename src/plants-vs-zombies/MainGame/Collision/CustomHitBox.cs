using System;
using System.Collections.Generic;
using Godot;

namespace PlantsVsZombies.Collision;

/// <summary>
/// 不参与物理引擎的箱子：形状在初始化时从子节点的 <see cref="CollisionShape2D"/>
/// 读一次，之后判定完全自算，不再碰任何节点。
///
/// 它和 <c>NativeHitBox</c> 的差别**不在几何从哪来**（两边都从场景配置读，
/// 都能在编辑器里可视化），而在**要不要引擎那套**：
///
/// | | NativeHitBox | 这里 |
/// |---|---|---|
/// | 基类 | Area2D，进物理世界 | Node2D，不进 |
/// | 每帧开销 | 引擎为它跑 broadphase | 零，不问就不算 |
/// | 区域事件 | 有 | 没有 |
/// | `Monitorable` | 引擎属性，只能延迟生效 | 普通字段，设了立刻读到 |
///
/// 只要自算判定、不需要引擎事件和重叠查询的实体用这个：省掉引擎那笔每帧开销，
/// 也省掉"设了当帧读不到"那个坑。将来实体数据化之后，箱子还能进一步变成纯数据，
/// 判定方看不出区别。
///
/// **形状只认矩形和圆。** 别的 <see cref="Shape2D"/> 是配置错误，直接抛——
/// 静默当零尺寸糊过去的话，判定会悄悄失效，那种错比启动就炸难查得多。
/// </summary>
[GlobalClass]
public partial class CustomHitBox : Node2D, IHitBox
{
    /// <summary>
    /// 特判修正：在场景配的基准形状上做调整（比如土豆雷按同行撑杆数平移左界）。
    /// null 就是直接用场景配的。
    ///
    /// 建一次存着，别每帧新建——捕获局部变量的 lambda 每次求值都会分配。
    /// </summary>
    public Func<HitShape, HitShape> AdjustShape;

    // 初始化时从场景读出来的两样东西：形状本身（尺寸/半径），以及它相对箱子的偏移。
    // 读完就不再碰那些节点了——它们只是启动时的配置载体
    private HitShape[] _shapes;
    private Vector2[] _offsets;

    public bool Monitorable { get; set; } = true;

    /// <summary>箱子挂在谁身上。和走引擎那个实现一样，在场景里配路径</summary>
    [Export] public NodePath AttachedNodePath { get; set; }

    public Node AttachedNode { get; set; }

    public int ShapeCount => _shapes?.Length ?? 0;

    public override void _Ready()
    {
        AttachedNode = GetNodeOrNull<Node>(AttachedNodePath);

        List<CollisionShape2D> nodes = new();

        foreach (Node child in GetChildren())
        {
            if (child is CollisionShape2D shapeNode)
            {
                nodes.Add(shapeNode);
            }
        }

        _shapes = new HitShape[nodes.Count];
        _offsets = new Vector2[nodes.Count];

        for (int i = 0; i < nodes.Count; i++)
        {
            _shapes[i] = ReadShape(nodes[i]);
            // 用两个全局位置求差、而不是直接取 Position：这样形状节点挂在哪一层都对
            _offsets[i] = nodes[i].GlobalPosition - GlobalPosition;
        }
    }

    /// <summary>
    /// 现算：箱子自己的位置加上初始化时记下的偏移，摆到世界坐标，再套上特判修正。
    /// **不碰形状节点**——它们在 <c>_Ready</c> 之后就没有用处了，删掉也不影响。
    ///
    /// 每次问都重算，同一帧内问两次可以拿到两个不同的值（箱子会动、特判也会改形状）。
    /// </summary>
    public HitShape GetShape(int index)
    {
        HitShape shape = ToWorld(_shapes[index], GlobalPosition + _offsets[index]);
        return AdjustShape?.Invoke(shape) ?? shape;
    }

    /// <summary>从形状节点读出尺寸。位置不在这里管</summary>
    private static HitShape ReadShape(CollisionShape2D node)
    {
        switch (node.Shape)
        {
            case RectangleShape2D rect:
                return HitShape.Rect(0, 0, (int)rect.Size.X, (int)rect.Size.Y);

            case CircleShape2D circle:
                return HitShape.Circle(0, 0, (int)circle.Radius);

            default:
                throw new NotSupportedException(
                    $"{node.Name} 的形状是 {node.Shape?.GetType().Name ?? "null"}，" +
                    "判定只认 RectangleShape2D 和 CircleShape2D");
        }
    }

    /// <summary>
    /// 把尺寸摆到世界坐标。形状节点的全局位置是形状中心，所以矩形要减掉半个尺寸。
    ///
    /// 取整是向零截断（C# 的 (int) 与原版一致），注意负坐标：-1.5 取到 -1。
    /// </summary>
    private static HitShape ToWorld(in HitShape shape, Vector2 center)
    {
        if (shape.Kind == ShapeKind.Rect)
        {
            return HitShape.Rect(
                (int)(center.X - shape.Width * 0.5f),
                (int)(center.Y - shape.Height * 0.5f),
                shape.Width,
                shape.Height);
        }

        return HitShape.Circle((int)center.X, (int)center.Y, shape.Radius);
    }
}
