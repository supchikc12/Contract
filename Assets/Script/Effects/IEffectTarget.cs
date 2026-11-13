// Объект, к которому можно прикрепить эффекты.
public interface IEffectTarget
{
    EffectTargetMask TargetMask { get; }
    EffectReceiver Effects { get; }
}
