using System.Collections.Generic;
using UnityEngine;

// Собирает эффекты персонажа: самого игрока, частей тела и надетых предметов.
public class CharacterEffectAggregator : MonoBehaviour, IEffectTarget
{
    [SerializeField] private BodyPartType periodicFallbackPart = BodyPartType.Torso;

    private EffectReceiver characterEffects;

    private readonly List<BodyPartHealth> bodyParts = new List<BodyPartHealth>();
    private readonly List<EffectReceiver> partReceivers = new List<EffectReceiver>();

    private readonly List<ItemInstance> itemTargets = new List<ItemInstance>();
    private readonly List<EffectReceiver> itemReceivers = new List<EffectReceiver>();

    public EffectTargetMask TargetMask => EffectTargetMask.Character;

    public EffectReceiver Effects
    {
        get
        {
            EnsureCharacterReceiver();
            return characterEffects;
        }
    }

    private void Awake()
    {
        EnsureCharacterReceiver();
        CollectBodyParts();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        Effects.Tick(dt);

        for (int i = 0; i < partReceivers.Count; i++)
            partReceivers[i].Tick(dt);

        for (int i = 0; i < itemReceivers.Count; i++)
            itemReceivers[i].Tick(dt);
    }

    public float GetValue(StatType stat, float baseValue)
    {
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
