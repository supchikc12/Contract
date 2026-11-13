using UnityEngine;

// Информация об уроне: тип, часть тела, источник и пробитие.
public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public BodyPartType part;
    public Object source;
    public float penetration;

    public DamageInfo(float amount, DamageType type, BodyPartType part, Object source = null)
    {
        this.amount = amount;
        this.type = type;
        this.part = part;
        this.source = source;
        this.penetration = 0f;
    }
}
