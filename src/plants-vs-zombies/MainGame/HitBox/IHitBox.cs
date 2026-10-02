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

    /// <summary>
    /// 世界坐标下的碰撞矩形。
    ///
    /// 子弹要"自己算矩形相交"，就得能从碰撞箱问出形状，而不是把判定丢给物理引擎——
    /// 引擎的判定是个黑盒，负距离这类边界情形没法照原版复刻。
    /// </summary>
    public Rect2 GlobalRect { get; }

    public Node AttachedNode { get; set; }

    public IReadOnlyList<IHitBox> GetOverlappingHitBox();
}