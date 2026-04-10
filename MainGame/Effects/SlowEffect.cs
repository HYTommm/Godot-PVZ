public class SlowEffect : StatusEffect
{
    private readonly float _multiplier;
    private readonly float _attackMul;

    public override bool Stackable => false;

    public SlowEffect(float attackMultiplier = 0.5f, float multiplier = 0.5f, double durationSeconds = 10.0) : base(durationSeconds)
    {
        Type = StatusEffectTypeEnum.Slow;
        _multiplier = multiplier;
        _attackMul = attackMultiplier;
    }

    public override float MovementMultiplier => _multiplier;
    public override float AttackMultiplier => _attackMul;

    public override void OnApply()
    {
        base.OnApply();
    }
}