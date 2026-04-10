using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum StatusEffectTypeEnum
{
    Slow,
    Freeze,
    Rage,
    // extendable
}

public abstract partial class StatusEffect
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public StatusEffectTypeEnum Type { get; protected init; }

    // Duration in seconds. <=0 means infinite
    public double Duration { get; protected set; }

    public double RemainingSeconds { get; private set; }

    public virtual bool Stackable => false;

    public HealthEntity Owner { get; internal set; }

    public virtual float MovementMultiplier => 1f;
    public virtual float AttackMultiplier => 1f;
    public virtual bool DisableMovement => false;

    protected StatusEffect(double durationSeconds)
    {
        Duration = durationSeconds;
        RemainingSeconds = durationSeconds;
    }

    public virtual void OnApply()
    { }

    public virtual void OnRemove()
    { }

    public virtual void OnTick(double deltaSeconds)
    {
        if (Duration > 0)
        {
            RemainingSeconds -= deltaSeconds;
        }
    }

    public bool IsExpired => Duration > 0 && RemainingSeconds <= 0;
}