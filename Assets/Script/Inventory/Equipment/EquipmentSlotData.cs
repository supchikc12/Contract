using UnityEngine;

[CreateAssetMenu(fileName = "New EquipmentSlot", menuName = "Inventory/Equipment Slot")]
public class EquipmentSlotData : ScriptableObject
{
    [Header("Настройки")]
    public string slotName;
    public ItemCategory allowedCategories;
    public Sprite defaultSprite;

    [Header("Позиция (в UI)")]
    public Vector2 anchoredPosition;
    public Vector2 size = new Vector2(100, 100);
}