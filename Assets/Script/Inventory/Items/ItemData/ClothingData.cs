using UnityEngine;

// Обычная одежда: футболки, штаны, обувь, перчатки.
[CreateAssetMenu(fileName = "New Clothing", menuName = "Inventory/Clothing Data")]
public class ClothingData : EquippableData
{
    [Header("Одежда")]
    public ClothingSlot clothingSlot = ClothingSlot.Torso;
    public float insulation;

    [Header("Покрытие")]
    [Tooltip("None = покрытие по слоту одежды")]
    public EffectTargetMask coveredParts = EffectTargetMask.None;

    public override EffectTargetMask GetCoveredParts()
    {
        if (coveredParts != EffectTargetMask.None)
            return coveredParts;

        switch (clothingSlot)
        {
            case ClothingSlot.Head: return EffectTargetMask.Head | EffectTargetMask.Neck;
            case ClothingSlot.Torso: return EffectTargetMask.Torso | EffectTargetMask.Pelvis;
            case ClothingSlot.Legs: return EffectTargetMask.Legs;
            case ClothingSlot.Feet: return EffectTargetMask.LeftFoot | EffectTargetMask.RightFoot;
            case ClothingSlot.Hands: return EffectTargetMask.LeftHand | EffectTargetMask.RightHand;
            default: return EffectTargetMask.None;
        }
    }
}
