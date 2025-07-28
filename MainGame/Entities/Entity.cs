using Godot;

public abstract partial class Entity : Node2D
{
	/// <summary>实体的索引/栈数</summary>
	public int Index;

	public virtual void SetZIndex()
    {
        ;
    }

    public override void _Ready()
    {
        base._Ready();
        SetZIndex();
    }
}
