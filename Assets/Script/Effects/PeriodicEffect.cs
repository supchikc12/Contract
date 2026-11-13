// Периодическая часть эффекта: урон или лечение каждые N секунд.
[System.Serializable]
public class PeriodicEffect
{
    public DamageType damageType = DamageType.Bleeding;
    public float amountPerTick = 1f;
    public float interval = 1f;
    public bool healing = false;
    public bool canKill = true;
}
