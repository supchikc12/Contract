using System.Collections.Generic;
using UnityEngine;

// Данные модификации оружия. Это обычный предмет инвентаря,
// но с типом слота, в который его можно установить.
[CreateAssetMenu(fileName = "New Weapon Mod", menuName = "Inventory/Weapon Mod")]
public class WeaponModData : ItemData
{
    [Header("Модификация оружия")]
    public WeaponModSlotType slotType = WeaponModSlotType.None;

    [Tooltip("Пусто — модификация подходит к любому оружию")]
    public List<string> compatibleWeaponIds = new List<string>();

    [Header("Будущие характеристики")]
    [Tooltip("Задел под систему статов; сейчас не используется")]
    public List<StatModifier> statModifiers = new List<StatModifier>();

    [System.Serializable]
    public class StatModifier
    {
        public string statId;
        public float value;
    }

    public bool CanInstallOn(ItemData weaponData)
    {
        if (weaponData == null) return false;
        if (compatibleWeaponIds == null || compatibleWeaponIds.Count == 0) return true;
        return compatibleWeaponIds.Contains(weaponData.id);
    }
}
