using System.Collections.Generic;
using UnityEngine;

// Данные оружия. Общие поля предмета наследуются от EquippableData.
[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon Data")]
public class WeaponData : EquippableData
{
    [Header("Оружие")]
    public WeaponClass weaponClass = WeaponClass.Primary;
    public float damage = 30f;
    public float fireRate = 600f;
    public string caliber;
    public float recoil = 1f;
    public float accuracy = 1f;

    [Header("Модификации")]
    public List<WeaponModSlotType> allowedModSlots = new List<WeaponModSlotType>();
}
