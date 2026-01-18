[System.Flags]
public enum ItemCategory
{
    None = 0,
    Weapon = 1 << 0,
    SecondaryWeapons = 1 << 1,
    Helmet = 1 << 2,
    Armor = 1 << 3,
    Container = 1 << 4,
    Food = 1 << 5,
    Medicine = 1 << 6,
    All = ~0
}