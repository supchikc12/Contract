// Суммирует эффекты оружия и глобальные эффекты персонажа для оружейных статов.
public static class WeaponEffectAggregator
{
    public static float GetValue(
        ItemInstance weapon,
        CharacterEffectAggregator character,
        StatType stat,
        float baseValue)
    {
        float add = 0f;
        float mult = 0f;

        if (weapon != null)
            weapon.Effects.Accumulate(stat, ref add, ref mult);

        if (character != null)
            character.Effects.Accumulate(stat, ref add, ref mult);

        return (baseValue + add) * (1f + mult);
    }
}
