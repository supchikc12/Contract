using System;
using System.Collections.Generic;
using UnityEngine;

// Активные эффекты одной цели: части тела, оружия, предмета или персонажа.
public class EffectReceiver
{
    private readonly IEffectTarget owner;
    private readonly List<ActiveEffect> active = new List<ActiveEffect>();

    public event Action<ActiveEffect> EffectApplied;
    public event Action<ActiveEffect> EffectRemoved;
    public event Action<ActiveEffect, PeriodicEffect> PeriodicTick;
    public event Action EffectsChanged;

    public EffectReceiver(IEffectTarget owner)
    {
        this.owner = owner;
    }

    public IReadOnlyList<ActiveEffect> Active => active;

    public bool Apply(EffectDefinition definition, UnityEngine.Object source = null, int stacks = 1)
    {
        if (definition == null || owner == null) return false;

        EffectTargetMask allowed = definition.allowedTargets;
        if (allowed == EffectTargetMask.None)
            allowed = EffectTargetMask.All; // совместимость со старыми ассетами

        if ((allowed & owner.TargetMask) == 0) return false;
        if (stacks < 1) stacks = 1;

        ActiveEffect existing = Find(definition, source);
        if (existing != null)
        {
            switch (definition.stackPolicy)
            {
                case EffectDefinition.StackPolicy.Ignore:
                    return true;

                case EffectDefinition.StackPolicy.Stack:
                    existing.Stacks = Mathf.Min(
                        Mathf.Max(1, definition.maxStacks),
                        existing.Stacks + stacks);
                    break;

                case EffectDefinition.StackPolicy.Refresh:
                    existing.Stacks = Mathf.Min(
                        Mathf.Max(1, definition.maxStacks),
                        Mathf.Max(1, existing.Stacks));
                    break;
            }

            existing.RemainingTime = Mathf.Max(0f, definition.duration);
            existing.Permanent = definition.duration <= 0f;
            EffectsChanged?.Invoke();
            return true;
        }

        ActiveEffect effect = new ActiveEffect
        {
            Definition = definition,
            Source = source,
            Stacks = stacks,
            RemainingTime = Mathf.Max(0f, definition.duration),
            Permanent = definition.duration <= 0f,
            Discovered = IsInitiallyDiscovered(definition),
            TargetMask = owner.TargetMask,
            PeriodicTimers = new float[definition.periodicEffects != null
                ? definition.periodicEffects.Count
                : 0]
        };

        active.Add(effect);
        EffectApplied?.Invoke(effect);
        EffectsChanged?.Invoke();
        return true;
    }

    public bool Remove(EffectDefinition definition, UnityEngine.Object source = null)
    {
        ActiveEffect effect = Find(definition, source);
        if (effect == null) return false;

        active.Remove(effect);
        EffectRemoved?.Invoke(effect);
        EffectsChanged?.Invoke();
        return true;
    }

    public int RemoveBySource(UnityEngine.Object source)
    {
        int removed = 0;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = active[i];
            if (effect.Source != source) continue;

            active.RemoveAt(i);
            EffectRemoved?.Invoke(effect);
            removed++;
        }

        if (removed > 0) EffectsChanged?.Invoke();
        return removed;
    }

    public int RemoveByTag(EffectTag tag)
    {
        int removed = 0;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = active[i];
            if (effect.Definition == null || (effect.Definition.tags & tag) == 0)
                continue;

            active.RemoveAt(i);
            EffectRemoved?.Invoke(effect);
            removed++;
        }

        if (removed > 0) EffectsChanged?.Invoke();
        return removed;
    }

    public void Tick(float deltaTime)
    {
        if (active.Count == 0 || deltaTime <= 0f) return;

        bool changed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = active[i];
            TickPeriodic(effect, deltaTime);

            if (effect.Permanent) continue;

            effect.RemainingTime -= deltaTime;
            if (effect.RemainingTime > 0f) continue;

            active.RemoveAt(i);
            EffectRemoved?.Invoke(effect);
            changed = true;
        }

        if (changed) EffectsChanged?.Invoke();
    }

    private void TickPeriodic(ActiveEffect effect, float deltaTime)
    {
        if (effect.Definition == null || effect.Definition.periodicEffects == null)
            return;

        int count = effect.Definition.periodicEffects.Count;
        if (count == 0) return;

        if (effect.PeriodicTimers == null || effect.PeriodicTimers.Length != count)
            effect.PeriodicTimers = new float[count];

        for (int i = 0; i < count; i++)
        {
            PeriodicEffect periodic = effect.Definition.periodicEffects[i];
            if (periodic == null || periodic.interval <= 0f) continue;

            effect.PeriodicTimers[i] += deltaTime;
            while (effect.PeriodicTimers[i] >= periodic.interval)
            {
                effect.PeriodicTimers[i] -= periodic.interval;
                PeriodicTick?.Invoke(effect, periodic);
            }
        }
    }

    public float GetValue(StatType stat, float baseValue)
    {
        float add = 0f;
        float mult = 0f;
        Accumulate(stat, ref add, ref mult);
        return (baseValue + add) * (1f + mult);
    }

    public void Accumulate(StatType stat, ref float add, ref float mult)
    {
        for (int i = 0; i < active.Count; i++)
        {
            ActiveEffect effect = active[i];
            if (effect.Definition == null || effect.Definition.modifiers == null)
                continue;

            for (int j = 0; j < effect.Definition.modifiers.Count; j++)
            {
                StatModifier modifier = effect.Definition.modifiers[j];
                if (modifier.stat != stat) continue;

                float value = modifier.value * effect.Stacks;
                if (modifier.mode == StatModifier.Mode.Add)
                    add += value;
                else
                    mult += value;
            }
        }
    }

    public int MarkAllDiscovered()
    {
        int changed = 0;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Discovered) continue;
            active[i].Discovered = true;
            changed++;
        }

        if (changed > 0) EffectsChanged?.Invoke();
        return changed;
    }

    public List<ActiveEffect> GetVisibleEffects()
    {
        List<ActiveEffect> result = new List<ActiveEffect>();
        CollectVisible(result);
        return result;
    }

    public void CollectAll(List<ActiveEffect> result)
    {
        if (result == null) return;
        result.AddRange(active);
    }

    public bool ApplySaved(EffectDefinition definition, int stacks, float remainingTime, bool permanent, bool discovered)
    {
        if (definition == null || owner == null) return false;

        EffectTargetMask allowed = definition.allowedTargets;
        if (allowed == EffectTargetMask.None)
            allowed = EffectTargetMask.All;

        if ((allowed & owner.TargetMask) == 0) return false;

        ActiveEffect effect = new ActiveEffect
        {
            Definition = definition,
            Source = null,
            Stacks = Mathf.Max(1, stacks),
            RemainingTime = Mathf.Max(0f, remainingTime),
            Permanent = permanent,
            Discovered = discovered,
            TargetMask = owner.TargetMask,
            PeriodicTimers = new float[definition.periodicEffects != null
                ? definition.periodicEffects.Count
                : 0]
        };

        active.Add(effect);
        EffectApplied?.Invoke(effect);
        EffectsChanged?.Invoke();
        return true;
    }

    public void CollectVisible(List<ActiveEffect> result)
    {
        if (result == null) return;

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Discovered)
                result.Add(active[i]);
        }
    }

    public bool HasEffect(EffectDefinition definition)
    {
        return Find(definition, null) != null;
    }

    public bool HasTag(EffectTag tag)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Definition != null && (active[i].Definition.tags & tag) != 0)
                return true;
        }

        return false;
    }

    public int GetStacks(EffectDefinition definition)
    {
        ActiveEffect effect = Find(definition, null);
        return effect != null ? effect.Stacks : 0;
    }

    public void Clear()
    {
        if (active.Count == 0) return;

        active.Clear();
        EffectsChanged?.Invoke();
    }

    private ActiveEffect Find(EffectDefinition definition, UnityEngine.Object source)
    {
        for (int i = 0; i < active.Count; i++)
        {
            ActiveEffect effect = active[i];
            if (effect.Definition == definition && effect.Source == source)
                return effect;
        }

        return null;
    }

    private static bool IsInitiallyDiscovered(EffectDefinition definition)
    {
        return definition.visibility == EffectVisibility.VisibleOnApply
            || definition.visibility == EffectVisibility.KnownFromItem;
    }
}
