using UnityEngine;

// Обычная одежда: футболки, штаны, обувь, перчатки.
[CreateAssetMenu(fileName = "New Clothing", menuName = "Inventory/Clothing Data")]
public class ClothingData : EquippableData
{
    [Header("Одежда")]
    public ClothingSlot clothingSlot = ClothingSlot.Torso;
    public float insulation;
}
