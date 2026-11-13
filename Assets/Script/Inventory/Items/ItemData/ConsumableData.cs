using System.Collections.Generic;
using UnityEngine;

// ≈да, напитки, медицина и стимул€торы.
// Ёффекты хран€тс€ здесь, их применение будет отдельной системой.
[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Data")]
public class ConsumableData : ItemData
{
    [Header("»спользование")]
    public ConsumableType consumableType = ConsumableType.Food;
    public float useTime = 1f;
    public int uses = 1;

    [Header("ѕрименение")]
    public ConsumableTarget target = ConsumableTarget.Self;
    public EffectTag cureTags = EffectTag.None;

    [Header("Ёффекты")]
    public List<EffectDefinition> effects = new List<EffectDefinition>();
}
