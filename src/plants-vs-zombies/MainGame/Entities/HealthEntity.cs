public abstract partial class HealthEntity : Entity
{
    ///// <summary>生命值</summary>
    public int HP;

    ///// <summary>最大生命值</summary>
    public int MaxHP;

    /// <summary>状态效果管理器</summary>
    protected StatusEffectManager StatusEffectManager { get; } = new();

    /// <summary>
    /// 外部添加状态效果的便捷方法（会把 owner 设置为当前实体）
    /// </summary>
    /// <param name="effect"></param>
    public void AddStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;
        StatusEffectManager.AddEffect(effect);
    }

    public abstract void Hurt(Hurt hurt);
}
