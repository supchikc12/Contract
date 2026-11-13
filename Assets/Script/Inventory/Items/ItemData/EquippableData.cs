using System.Collections.Generic;
using UnityEngine;

// ќбщие данные дл€ всего, что можно надеть или вз€ть в руки.
public class EquippableData : ItemData
{
    [Header("Ёкипировка")]
    public EquipmentSlotType equipmentSlot = EquipmentSlotType.None;

    [Header("ѕозици€ на модели")]
    public Vector3 previewLocalPosition;
    public Vector3 previewLocalEuler;

    [Header("Ёффекты при экипировке")]
    [Tooltip("ѕримен€ютс€ к предмету, пока он надет")]
    public List<EffectDefinition> effects = new List<EffectDefinition>();

    public virtual EffectTargetMask GetCoveredParts()
    {
        return EffectTargetMask.None;
    }
}
