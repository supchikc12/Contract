using System;
using System.Collections.Generic;
using UnityEngine;

// Здоровье игрока по зонам тела.
// Состояние хранится централизованно, чтобы система работала и без готовой модели.
public class PlayerHealth : MonoBehaviour
{
    [Serializable]
    public struct BodyPartState
    {
        public BodyPartType part;
        public float maxHealth;
        public float currentHealth;
    }

    [Header("Зоны тела")]
    [SerializeField] private List<BodyPartState> bodyParts = new List<BodyPartState>();

    [Header("Значения по умолчанию")]
    [SerializeField] private float defaultMaxHealth = 100f;

    // part, currentHealth, maxHealth
    public event Action<BodyPartType, float, float> HealthChanged;
    public event Action<BodyPartType> PartDied;

    public IReadOnlyList<BodyPartState> BodyParts => bodyParts;
    public float DefaultMaxHealth => defaultMaxHealth;

    public float TotalHealth
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < bodyParts.Count; i++)
                total += bodyParts[i].currentHealth;
            return total;
        }
    }

    public float TotalMaxHealth
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < bodyParts.Count; i++)
                total += bodyParts[i].maxHealth;
            return total;
        }
    }

    private void Awake()
    {
        EnsureAllParts();
    }

    private void Reset()
    {
        defaultMaxHealth = 100f;
        FillAllParts();
    }

    private void OnValidate()
    {
        defaultMaxHealth = Mathf.Max(1f, defaultMaxHealth);
        ClampAllParts();
    }

    private void ClampAllParts()
    {
        if (bodyParts == null) return;

        for (int i = 0; i < bodyParts.Count; i++)
        {
            BodyPartState state = bodyParts[i];
            state.maxHealth = Mathf.Max(1f, state.maxHealth);
            state.currentHealth = Mathf.Clamp(state.currentHealth, 0f, state.maxHealth);
            bodyParts[i] = state;
        }
    }
    // Добавляет недостающие зоны, не трогая уже настроенные значения
    public void EnsureAllParts()
    {
        if (bodyParts == null) bodyParts = new List<BodyPartState>();

        foreach (BodyPartType part in Enum.GetValues(typeof(BodyPartType)))
        {
            if (IndexOf(part) >= 0) continue;

            bodyParts.Add(new BodyPartState
            {
                part = part,
                maxHealth = Mathf.Max(1f, defaultMaxHealth),
                currentHealth = Mathf.Max(1f, defaultMaxHealth)
            });
        }

        ClampAllParts();
    }

    // Полностью пересоздаёт список зон со стартовыми значениями
    [ContextMenu("Заполнить все зоны")]
    public void FillAllParts()
    {
        bodyParts = new List<BodyPartState>();

        foreach (BodyPartType part in Enum.GetValues(typeof(BodyPartType)))
        {
            bodyParts.Add(new BodyPartState
            {
                part = part,
                maxHealth = Mathf.Max(1f, defaultMaxHealth),
                currentHealth = Mathf.Max(1f, defaultMaxHealth)
            });
        }
    }

    public int IndexOf(BodyPartType part)
    {
        if (bodyParts == null) return -1;

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].part == part) return i;
        }
        return -1;
    }

    public float GetHealth(BodyPartType part)
    {
        int index = IndexOf(part);
        return index >= 0 ? bodyParts[index].currentHealth : 0f;
    }

    public float GetMaxHealth(BodyPartType part)
    {
        int index = IndexOf(part);
        return index >= 0 ? bodyParts[index].maxHealth : 0f;
    }

    public float GetHealthPercent(BodyPartType part)
    {
        float max = GetMaxHealth(part);
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(GetHealth(part) / max);
    }

    public bool IsAlive(BodyPartType part)
    {
        return GetHealth(part) > 0f;
    }

    public void ApplyDamage(BodyPartType part, float amount)
    {
        if (amount <= 0f) return;
        SetHealth(part, GetHealth(part) - amount);
    }

    public void Heal(BodyPartType part, float amount)
    {
        if (amount <= 0f) return;
        SetHealth(part, GetHealth(part) + amount);
    }

    public void SetHealth(BodyPartType part, float value)
    {
        int index = IndexOf(part);
        if (index < 0) return;

        BodyPartState state = bodyParts[index];
        float previous = state.currentHealth;
        state.currentHealth = Mathf.Clamp(value, 0f, state.maxHealth);
        bodyParts[index] = state;

        if (Mathf.Approximately(previous, state.currentHealth)) return;

        HealthChanged?.Invoke(part, state.currentHealth, state.maxHealth);

        if (previous > 0f && state.currentHealth <= 0f)
            PartDied?.Invoke(part);
    }

    [ContextMenu("Сбросить здоровье")]
    public void ResetAll()
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            BodyPartState state = bodyParts[i];
            state.currentHealth = state.maxHealth;
            bodyParts[i] = state;
            HealthChanged?.Invoke(state.part, state.currentHealth, state.maxHealth);
        }
    }
}
