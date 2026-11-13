using UnityEngine;

// Runtime-экземпляр эффекта на конкретной цели.
public class ActiveEffect
{
    public EffectDefinition Definition;
    public Object Source;
    public int Stacks = 1;
    public float RemainingTime;
    public bool Permanent;
    public bool Discovered;
    public EffectTargetMask TargetMask;

    public bool IsExpired => !Permanent && RemainingTime <= 0f;
}
