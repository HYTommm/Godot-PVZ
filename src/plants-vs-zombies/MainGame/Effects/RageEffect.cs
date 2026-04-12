public class RageEffect : StatusEffect
{
    private readonly float _attackMul;

    public override bool Stackable => false;

    public RageEffect(float attackMultiplier = 1.5f, double durationSeconds = 4.0) : base(durationSeconds)
    {
        Type = StatusEffectTypeEnum.Rage;
        _attackMul = attackMultiplier;
    }

    public override float AttackMultiplier => _attackMul;
}