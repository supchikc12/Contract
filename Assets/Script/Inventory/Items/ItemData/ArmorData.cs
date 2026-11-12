using UnityEngine;

// Броня и шлемы. Отдельный HelmetData не нужен: различие задаётся ArmorSlot.
[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/Armor Data")]
public class ArmorData : EquippableData
{
    [Header("Броня")]
    public ArmorSlot armorSlot = ArmorSlot.Torso;
    public int armorClass = 1;
    [Range(0f, 1f)] public float damageReduction;
}
