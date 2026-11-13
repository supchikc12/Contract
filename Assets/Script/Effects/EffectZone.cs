using UnityEngine;

// Триггер-зона: накладывает эффект на персонажа или часть тела.
[RequireComponent(typeof(Collider))]
public class EffectZone : MonoBehaviour
{
    public enum TargetMode
    {
        Character,
        BodyPart
    }

    [SerializeField] private EffectDefinition effect;
    [SerializeField] private TargetMode targetMode = TargetMode.Character;
    [SerializeField] private bool removeOnExit = true;
    [SerializeField] private BodyPartType[] allowedParts;

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
    }

    public void Configure(
        EffectDefinition definition,
        TargetMode mode,
        bool removeWhenExit,
        BodyPartType[] parts)
    {
        effect = definition;
        targetMode = mode;
        removeOnExit = removeWhenExit;
        allowedParts = parts;

        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (effect == null) return;

        if (targetMode == TargetMode.BodyPart)
        {
            BodyPartHealth part = other.GetComponentInParent<BodyPartHealth>();
            if (part == null || !IsAllowed(part.PartType)) return;

            part.Effects.Apply(effect, this);
            return;
        }

        if (other.GetComponentInParent<BodyPartHealth>() != null)
            return;

        CharacterEffectAggregator character = other.GetComponentInParent<CharacterEffectAggregator>();
        if (character != null)
            character.ApplyToCharacter(effect, this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!removeOnExit || effect == null) return;

        if (targetMode == TargetMode.BodyPart)
        {
            BodyPartHealth part = other.GetComponentInParent<BodyPartHealth>();
            if (part == null || !IsAllowed(part.PartType)) return;

            part.Effects.Remove(effect, this);
            return;
        }

        CharacterEffectAggregator character = other.GetComponentInParent<CharacterEffectAggregator>();
        if (character != null)
            character.RemoveEffectsFromSource(this);
    }

    private bool IsAllowed(BodyPartType part)
    {
        if (allowedParts == null || allowedParts.Length == 0) return true;

        for (int i = 0; i < allowedParts.Length; i++)
        {
            if (allowedParts[i] == part) return true;
        }

        return false;
    }
}
