public interface IStatusEffect
{
    /// <summary>状态效果管理器</summary>
    StatusEffectManager StatusEffectManager { get; }
}

public static class StatusEffectExtensions
{
    /// <summary>
    /// 便捷方法：直接添加状态效果到实体
    /// </summary>
    public static void AddStatusEffect(this IStatusEffect entity, StatusEffect effect) => entity.StatusEffectManager.AddEffect(effect);
}
