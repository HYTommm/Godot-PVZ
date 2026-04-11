using System.Collections.Generic;
using System.Linq;

using System;

public class StatusEffectManager
{
    private readonly List<StatusEffect> _effects = new();

    public IReadOnlyList<StatusEffect> ActiveEffects => _effects.AsReadOnly();

    public event Action EffectsChanged;

    public void AddEffect(StatusEffect effect, HealthEntity owner)
    {
        if (effect == null || owner == null) return;
        effect.Owner = owner;

        if (!effect.Stackable)
        {
            var existing = _effects.FirstOrDefault(e => e.Type == effect.Type);
            if (existing != null)
            {
                RemoveEffect(existing);
            }
        }

        _effects.Add(effect);
        try { effect.OnApply(); } catch { }
        EffectsChanged?.Invoke();
    }

    public void RemoveEffect(StatusEffect effect)
    {
        if (effect == null) return;
        if (_effects.Remove(effect))
        {
            try { effect.OnRemove(); } catch { }
            EffectsChanged?.Invoke();
        }
    }

    public void Tick(double deltaSeconds)
    {
        if (_effects.Count == 0) return;
        var copy = _effects.ToArray();
        bool changed = false;
        foreach (var e in copy)
        {
            try { e.OnTick(deltaSeconds); } catch { }
            if (e.IsExpired)
            {
                RemoveEffect(e);
                changed = true;
            }
        }
        if (changed) EffectsChanged?.Invoke();
    }

    public float MovementMultiplier
    {
        get
        {
            if (_effects.Any(e => e.DisableMovement))
                return 0f;
            float mul = 1f;
            foreach (var e in _effects) mul *= e.MovementMultiplier;
            return mul;
        }
    }

    public float AttackMultiplier
    {
        get
        {
            float mul = 1f;
            foreach (var e in _effects) mul *= e.AttackMultiplier;
            return mul;
        }
    }

    public bool HasEffect(StatusEffectTypeEnum type) => _effects.Any(e => e.Type == type);

    public void Clear()
    {
        foreach (var e in _effects.ToArray()) RemoveEffect(e);
    }
}