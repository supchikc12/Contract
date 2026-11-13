using System;

// Маска целей, к которым можно применить эффект.
// Биты частей тела совпадают с порядком BodyPartType.
[Flags]
public enum EffectTargetMask
{
    None = 0,

    Head = 1 << 0,
    Neck = 1 << 1,
    Torso = 1 << 2,
    Pelvis = 1 << 3,

    LeftThigh = 1 << 4,
    LeftShin = 1 << 5,
    LeftFoot = 1 << 6,
    RightThigh = 1 << 7,
    RightShin = 1 << 8,
    RightFoot = 1 << 9,

    LeftUpperArm = 1 << 10,
    LeftForearm = 1 << 11,
    LeftHand = 1 << 12,
    RightUpperArm = 1 << 13,
    RightForearm = 1 << 14,
    RightHand = 1 << 15,

    Character = 1 << 16,
    Weapon = 1 << 17,
    Item = 1 << 18,
    Container = 1 << 19,
    World = 1 << 20,

    LeftLeg = LeftThigh | LeftShin | LeftFoot,
    RightLeg = RightThigh | RightShin | RightFoot,
    LeftArm = LeftUpperArm | LeftForearm | LeftHand,
    RightArm = RightUpperArm | RightForearm | RightHand,
    Legs = LeftLeg | RightLeg,
    Arms = LeftArm | RightArm,
    AllBodyParts = Head | Neck | Torso | Pelvis | Legs | Arms,

    All = ~0
}
