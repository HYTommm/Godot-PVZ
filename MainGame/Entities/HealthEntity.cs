
public abstract partial class HealthEntity : Entity
{
    /// <summary>生命值</summary>
    public int HP;
    /// <summary>最大生命值</summary>
    public int MaxHP;

    public abstract int Hurt(int damage);
}
