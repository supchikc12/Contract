using UnityEngine;

// Ѕрон€ и шлемы. ќтдельный HelmetData не нужен: различие задаЄтс€ ArmorSlot.
[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/Armor Data")]
public class ArmorData : EquippableData
{
    [Header("Ѕрон€")]
    public ArmorSlot armorSlot = ArmorSlot.Torso;
    public int armorClass = 1;
    [Range(0f, 1f)] public float damageReduction;

    [Header("ѕокрытие")]
    [Tooltip("None = покрытие по слоту: шлем Ч голова/ше€, брон€ Ч торс/таз")]
    public EffectTargetMask coveredParts = EffectTargetMask.None;

    public override EffectTargetMask GetCoveredParts()
    {
        if (coveredParts != EffectTargetMask.None)
            return coveredParts;

        return armorSlot == ArmorSlot.Head
            ? EffectTargetMask.Head | EffectTargetMask.Neck
            : EffectTargetMask.Torso | EffectTargetMask.Pelvis;
    }
}
