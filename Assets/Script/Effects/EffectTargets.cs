// Помощники для работы с маской целей эффектов.
public static class EffectTargets
{
    public static EffectTargetMask ForBodyPart(BodyPartType part)
    {
        return (EffectTargetMask)(1 << (int)part);
    }
}
