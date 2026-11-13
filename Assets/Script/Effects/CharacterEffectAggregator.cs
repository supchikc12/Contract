using System.Collections.Generic;
using UnityEngine;

// Собирает эффекты персонажа: самого игрока, частей тела и надетых предметов.
public class CharacterEffectAggregator : MonoBehaviour, IEffectTarget
{
    [SerializeField] private BodyPartType periodicFallbackPart = BodyPartType.Torso;

    private EffectReceiver characterEffects;
    private bool initialized;

    private readonly List<BodyPartHealth> bodyParts = new List<BodyPartHealth>();
    private readonly List<EffectReceiver> partReceivers = new List<EffectReceiver>();

    private readonly List<ItemInstance> itemTargets = new List<ItemInstance>();
    private readonly List<EffectReceiver> itemReceivers = new List<EffectReceiver>();

    public EffectTargetMask TargetMask => EffectTargetMask.Character;

    public EffectReceiver Effects
    {
        get
        {
            EnsureInitialized();
            return characterEffects;
        }
    }

    private void Awake()
    {
        if (GetComponent<ConsumableUseController>() == null)
            gameObject.AddComponent<ConsumableUseController>();
        if (GetComponent<EffectSaveController>() == null)
            gameObject.AddComponent<EffectSaveController>();

        EnsureInitialized();
    }

    private void Update()
    {
        EnsureInitialized();

        float dt = Time.deltaTime;

        Effects.Tick(dt);

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].Tick(dt);

        for (int i = 0; i < itemReceivers.Count; i++)
            itemReceivers[i].Tick(dt);
    }

    public float GetValue(StatType stat, float baseValue)
    {
        EnsureInitialized();

        float add = 0f;
        float mult = 0f;

        Effects.Accumulate(stat, ref add, ref mult);

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].Accumulate(stat, ref add, ref mult);

        for (int i = 0; i < itemReceivers.Count; i++)
        {
            if (itemTargets[i] != null &&
                (itemTargets[i].TargetMask & EffectTargetMask.Weapon) != 0)
                continue;

            itemReceivers[i].Accumulate(stat, ref add, ref mult);
        }

        return (baseValue + add) * (1f + mult);
    }

    public float GetResistance(DamageType type, BodyPartType part)
    {
        EnsureInitialized();

        StatType stat = ResistanceStat(type);
        float add = 0f;
        float mult = 0f;

        Effects.Accumulate(stat, ref add, ref mult);

        BodyPartHealth partTarget = FindPart(part);
        if (partTarget != null)
            partTarget.Effects.Accumulate(stat, ref add, ref mult);

        for (int i = 0; i < itemReceivers.Count; i++)
        {
            ItemInstance item = itemTargets[i];
            if (item == null || !item.CoversPart(part)) continue;
            if ((item.TargetMask & EffectTargetMask.Weapon) != 0) continue;

            itemReceivers[i].Accumulate(stat, ref add, ref mult);
        }

        float resistance = (1f + add) * (1f + mult) - 1f;
        return Mathf.Clamp(resistance, -1f, 0.9f);
    }

    public float GetArmorReduction(BodyPartType part)
    {
        EnsureInitialized();

        float reduction = 0f;

        for (int i = 0; i < itemTargets.Count; i++)
        {
            ItemInstance item = itemTargets[i];
            if (item == null || !item.CoversPart(part)) continue;

            ItemState state = item.GetItemState();
            ArmorData armor = state != null ? state.data as ArmorData : null;
            if (armor == null) continue;

            reduction += armor.damageReduction;
        }

        return Mathf.Clamp(reduction, 0f, 0.9f);
    }

    public bool ApplyToPart(BodyPartType part, EffectDefinition definition, Object source = null, int stacks = 1)
    {
        BodyPartHealth target = FindPart(part);
        return target != null && target.Effects.Apply(definition, source, stacks);
    }

    public bool ApplyToCharacter(EffectDefinition definition, Object source = null, int stacks = 1)
    {
        return Effects.Apply(definition, source, stacks);
    }

    public bool ApplyToItem(ItemInstance item, EffectDefinition definition, Object source = null, int stacks = 1)
    {
        return item != null && item.Effects.Apply(definition, source, stacks);
    }

    public int RemoveEffectsByTag(EffectTag tag, BodyPartType? part = null)
    {
        EnsureInitialized();

        if (tag == EffectTag.None) return 0;

        if (part.HasValue)
        {
            BodyPartHealth target = FindPart(part.Value);
            return target != null ? target.Effects.RemoveByTag(tag) : 0;
        }

        int removed = Effects.RemoveByTag(tag);

        for (int i = 0; i < partReceivers.Count; i++)
            removed += partReceivers[i].RemoveByTag(tag);

        for (int i = 0; i < itemReceivers.Count; i++)
            removed += itemReceivers[i].RemoveByTag(tag);

        return removed;
    }

    public int RemoveEffectsFromSource(Object source)
    {
        EnsureInitialized();

        int removed = Effects.RemoveBySource(source);
        for (int i = 0; i < partReceivers.Count; i++)
            removed += partReceivers[i].RemoveBySource(source);
        for (int i = 0; i < itemReceivers.Count; i++)
            removed += itemReceivers[i].RemoveBySource(source);

        return removed;
    }

    public List<ActiveEffect> GetSavableEffects()
    {
        EnsureInitialized();

        List<ActiveEffect> result = new List<ActiveEffect>();
        Effects.CollectAll(result);

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].CollectAll(result);

        return result;
    }

    public void ClearAllEffects()
    {
        EnsureInitialized();

        Effects.Clear();
        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].Clear();
        for (int i = 0; i < itemReceivers.Count; i++)
            itemReceivers[i].Clear();
    }

    public bool ApplySavedEffect(
        EffectDefinition definition,
        EffectTargetMask targetMask,
        int stacks,
        float remainingTime,
        bool permanent,
        bool discovered)
    {
        EnsureInitialized();

        if (targetMask == EffectTargetMask.Character)
            return Effects.ApplySaved(definition, stacks, remainingTime, permanent, discovered);

        BodyPartType? part = FindPartByMask(targetMask);
        if (!part.HasValue) return false;

        BodyPartHealth target = FindPart(part.Value);
        return target != null
            && target.Effects.ApplySaved(definition, stacks, remainingTime, permanent, discovered);
    }

    public void InspectAll()
    {
        Effects.MarkAllDiscovered();

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].MarkAllDiscovered();

        for (int i = 0; i < itemReceivers.Count; i++)
            itemReceivers[i].MarkAllDiscovered();
    }

    public void InspectPart(BodyPartType part)
    {
        BodyPartHealth target = FindPart(part);
        if (target != null) target.Effects.MarkAllDiscovered();
    }

    public List<ActiveEffect> GetVisibleEffects()
    {
        List<ActiveEffect> result = new List<ActiveEffect>();

        Effects.CollectVisible(result);

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].CollectVisible(result);

        for (int i = 0; i < itemReceivers.Count; i++)
            itemReceivers[i].CollectVisible(result);

        return result;
    }

    public List<ActiveEffect> GetVisibleEffects(BodyPartType part)
    {
        BodyPartHealth target = FindPart(part);
        return target != null
            ? target.Effects.GetVisibleEffects()
            : new List<ActiveEffect>();
    }

    public void RegisterItem(ItemInstance item)
    {
        if (item == null || itemTargets.Contains(item)) return;

        itemTargets.Add(item);
        itemReceivers.Add(item.Effects);
    }

    public void UnregisterItem(ItemInstance item)
    {
        int index = itemTargets.IndexOf(item);
        if (index < 0) return;

        itemTargets.RemoveAt(index);
        itemReceivers.RemoveAt(index);
    }

    private void EnsureInitialized()
    {
        if (initialized && characterEffects != null) return;

        initialized = true;
        EnsureCharacterReceiver();
        CollectBodyParts();
    }

    private static StatType ResistanceStat(DamageType type)
    {
        switch (type)
        {
            case DamageType.Ballistic: return StatType.BallisticResistance;
            case DamageType.Thermal: return StatType.ThermalResistance;
            case DamageType.Electrical: return StatType.ElectricalResistance;
            case DamageType.Chemical: return StatType.ChemicalResistance;
            case DamageType.Bleeding: return StatType.BleedingResistance;
            case DamageType.Radiation: return StatType.RadiationResistance;
            default: return StatType.DamageResistance;
        }
    }

    private void EnsureCharacterReceiver()
    {
        if (characterEffects != null) return;

        characterEffects = new EffectReceiver(this);
        characterEffects.PeriodicTick += OnCharacterPeriodicTick;
    }

    private void OnCharacterPeriodicTick(ActiveEffect effect, PeriodicEffect periodic)
    {
        BodyPartHealth part = FindPart(periodicFallbackPart);
        if (part != null)
            part.ApplyPeriodicEffect(effect, periodic);
    }

    private void CollectBodyParts()
    {
        bodyParts.Clear();
        partReceivers.Clear();

        foreach (BodyPartHealth part in GetComponentsInChildren<BodyPartHealth>(true))
        {
            if (part == null) continue;
            bodyParts.Add(part);
            partReceivers.Add(part.Effects);
        }
    }

    private BodyPartType? FindPartByMask(EffectTargetMask mask)
    {
        foreach (BodyPartType part in System.Enum.GetValues(typeof(BodyPartType)))
        {
            if (mask == EffectTargets.ForBodyPart(part))
                return part;
        }

        return null;
    }

    private BodyPartHealth FindPart(BodyPartType part)
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i] != null && bodyParts[i].PartType == part)
                return bodyParts[i];
        }

        return null;
    }
}
