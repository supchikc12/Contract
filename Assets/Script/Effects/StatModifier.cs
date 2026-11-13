// Модификатор характеристики. Add - плоская прибавка, Multiply - доля (0.25 = +25%).
[System.Serializable]
public struct StatModifier
{
    public enum Mode
    {
        Add,
        Multiply
    }

    public StatType stat;
    public Mode mode;
    public float value;
}
