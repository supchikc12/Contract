using System.Collections.Generic;
using UnityEngine;

// Описание эффекта: длительность, стаки и модификаторы разных характеристик.
[CreateAssetMenu(fileName = "New Effect", menuName = "Effects/Effect")]
public class EffectDefinition : ScriptableObject
{
    public enum StackPolicy
    {
        Refresh,
        Stack,
        Ignore
    }

    [Header("Описание")]
    [Tooltip("Уникальный ID эффекта; используется для стаков и сохранения")]
    public string id;
    public string effectName = "Новый эффект";
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Применение")]
    [Tooltip("К каким целям можно прикрепить эффект")]
    public EffectTargetMask allowedTargets = EffectTargetMask.Character;
    public EffectVisibility visibility = EffectVisibility.InspectTarget;
    public EffectTag tags = EffectTag.None;

    [Header("Длительность")]
    [Tooltip("0 = постоянный эффект до ручного снятия")]
    public float duration = 10f;

    [Header("Стаки")]
    public StackPolicy stackPolicy = StackPolicy.Refresh;
    public int maxStacks = 1;

    [Header("Модификаторы")]
    public List<StatModifier> modifiers = new List<StatModifier>();

    private void OnValidate()
    {
        duration = Mathf.Max(0f, duration);
        maxStacks = Mathf.Max(1, maxStacks);
        if (modifiers == null) modifiers = new List<StatModifier>();
    }
}
