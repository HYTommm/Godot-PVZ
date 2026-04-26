public interface IHealthStage
{
    /// <summary>血量阶段组件</summary>
    HealthStageComponent HealthStageComponent { get; set; }

    public bool Alive { get; }
}

public static class HealthStageExtensions
{
    public static void Hurt(this IHealthStage stage, Hurt hurt)
    {
        if (stage.HealthStageComponent.HP <= 0) return;
        stage.HealthStageComponent.TakeDamage(hurt);
    }
}