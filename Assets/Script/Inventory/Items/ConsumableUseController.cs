using System.Collections;
using UnityEngine;

// Пошаговое использование расходника: задержка useTime и прогресс для HUD.
public class ConsumableUseController : MonoBehaviour
{
    private Coroutine routine;
    private float progress;

    public bool IsUsing { get; private set; }
    public float Progress => progress;
    public string CurrentItemName { get; private set; }

    public bool BeginUse(ItemInstance item, BodyPartType? targetPart)
    {
        if (IsUsing) return false;
        if (!ConsumableUseService.CanUse(item)) return false;

        ItemState state = item.GetItemState();
        ConsumableData data = state != null ? state.data as ConsumableData : null;
        if (data == null) return false;

        if (data.target == ConsumableTarget.BodyPart && !targetPart.HasValue)
            return false;

        float duration = Mathf.Max(0f, data.useTime);
        if (duration <= 0f)
            return ConsumableUseService.TryUse(item, targetPart);

        routine = StartCoroutine(UseRoutine(item, targetPart, duration, data.itemName));
        return true;
    }

    public void CancelUse()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = null;
        IsUsing = false;
        progress = 0f;
        CurrentItemName = null;
    }

    private IEnumerator UseRoutine(ItemInstance item, BodyPartType? targetPart, float duration, string itemName)
    {
        IsUsing = true;
        CurrentItemName = itemName;
        progress = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (item == null || !ConsumableUseService.CanUse(item))
            {
                CancelUse();
                yield break;
            }

            elapsed += Time.deltaTime;
            progress = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        IsUsing = false;
        progress = 0f;

        if (item != null && ConsumableUseService.CanUse(item))
            ConsumableUseService.TryUse(item, targetPart);

        CurrentItemName = null;
        routine = null;
    }
}
