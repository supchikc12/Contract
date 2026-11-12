using UnityEngine;

// Общие данные для всего, что можно надеть или взять в руки.
public class EquippableData : ItemData
{
    [Header("Экипировка")]
    public EquipmentSlotType equipmentSlot = EquipmentSlotType.None;

    [Header("Позиция на модели")]
    public Vector3 previewLocalPosition;
    public Vector3 previewLocalEuler;
}
