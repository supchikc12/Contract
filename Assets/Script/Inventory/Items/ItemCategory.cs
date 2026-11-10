[System.Flags]
public enum ItemCategory
{
    None = 0,
    Weapon = 1 << 0,
    LightWeapon = 1 << 1,
    SecondaryWeapons = 1 << 2,
    Helmet = 1 << 3,
    Armor = 1 << 4,
    Container = 1 << 5,
    Food = 1 << 6,
    Medicine = 1 << 7,
    Backpack = 1 << 8,
    Rig = 1 << 9,
    WeaponMod = 1 << 10,
    All = ~0
}