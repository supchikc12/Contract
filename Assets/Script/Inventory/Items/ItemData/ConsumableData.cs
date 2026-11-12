using UnityEngine;

// ≈да, напитки, медицина и стимул€торы.
// Ёффекты будут добавлены отдельной системой, когда по€витс€ еЄ заглушка.
[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Data")]
public class ConsumableData : ItemData
{
    [Header("»спользование")]
    public ConsumableType consumableType = ConsumableType.Food;
    public float useTime = 1f;
    public int uses = 1;
}
