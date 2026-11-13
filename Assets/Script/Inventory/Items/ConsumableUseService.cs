using UnityEngine;

// Применение расходников: эффекты, лечение по тегам и расход стака.
public static class ConsumableUseService
{
    public static bool CanUse(ItemInstance item)
    {
        if (item == null) return false;

        ItemState state = item.GetItemState();
        if (state == null || state.data == null || state.currentStack <= 0)
            return false;

        if (!(state.data is ConsumableData))
            return false;

        if (state.currentUses == 0)
            return false;

        if (item.transform.parent != null &&
            item.transform.parent.GetComponent<EquipmentSlot>() != null)
            return false;

        return true;
    }

    public static bool TryUse(ItemInstance item, BodyPartType? targetPart)
    {
        if (!CanUse(item)) return false;

        ItemState state = item.GetItemState();
        ConsumableData data = state.data as ConsumableData;
        if (data == null) return false;

        EnsureUses(state, data);
        if (state.currentUses <= 0) return false;

        if (data.target == ConsumableTarget.BodyPart && !targetPart.HasValue)
            return false;

        CharacterEffectAggregator aggregator = item.GetComponentInParent<CharacterEffectAggregator>();
        if (aggregator == null)
            aggregator = Object.FindAnyObjectByType<CharacterEffectAggregator>();

        if (aggregator == null)
        {
            Debug.LogWarning("ConsumableUseService: CharacterEffectAggregator не найден.");
            return false;
        }

        if (targetPart.HasValue)
            aggregator.InspectPart(targetPart.Value);

        int applied = 0;

        if (data.effects != null)
        {
            for (int i = 0; i < data.effects.Count; i++)
            {
                EffectDefinition effect = data.effects[i];
                if (effect == null) continue;

                bool result = targetPart.HasValue
                    ? aggregator.ApplyToPart(targetPart.Value, effect, null)
                    : aggregator.ApplyToCharacter(effect, null);

                if (result) applied++;
            }
        }

        int cured = 0;
        if (data.cureTags != EffectTag.None)
            cured = aggregator.RemoveEffectsByTag(data.cureTags, targetPart);

        if (applied == 0 && cured == 0)
        {
            Debug.LogWarning("ConsumableUseService: нечего применять или лечить.");
            return false;
        }

        ConsumeOne(item, data);
        return true;
    }

    private static void EnsureUses(ItemState state, ConsumableData data)
    {
        if (state.currentUses <= 0)
            state.currentUses = MaxUses(data);
    }

    private static int MaxUses(ConsumableData data)
    {
        return Mathf.Max(1, data.uses);
    }

    private static void ConsumeOne(ItemInstance item, ConsumableData data)
    {
        ItemState state = item.GetItemState();
        if (state == null) return;

        state.currentUses--;
        if (state.currentUses > 0)
        {
            item.UpdateStackUI();
            return;
        }

        if (state.currentStack > 1)
        {
            state.currentStack--;
            state.currentUses = MaxUses(data);
            item.UpdateStackUI();
            return;
        }

        Object.Destroy(item.gameObject);
    }
}
