using UnityEngine;

// Патроны. Отдельный тип, потому что у них свои калибр, пробитие и баллистика.
[CreateAssetMenu(fileName = "New Ammo", menuName = "Inventory/Ammo Data")]
public class AmmoData : ItemData
{
    [Header("Баллистика")]
    public string caliber;
    public string bulletType = "FMJ";
    public float damageMultiplier = 1f;
    public float penetration = 1f;
}
