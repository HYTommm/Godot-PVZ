public class FreezeEffect : StatusEffect
{
    public FreezeEffect(double durationSeconds = 1.5) : base(durationSeconds)
    {
        Type = StatusEffectTypeEnum.Freeze;
    }

    public override bool Stackable => false;

    public override bool DisableMovement => true;
}