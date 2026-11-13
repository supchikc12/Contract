using System;

// Теги эффектов: нужны для лечения, UI и группировки.
[Flags]
public enum EffectTag
{
    None = 0,
    Buff = 1 << 0,
    Debuff = 1 << 1,
    Bleeding = 1 << 2,
    Burning = 1 << 3,
    Poisoned = 1 << 4,
    Shocked = 1 << 5,
    Fracture = 1 << 6,
    Pain = 1 << 7,
    Thermal = 1 << 8,
    Electrical = 1 << 9,
    Chemical = 1 << 10
}
