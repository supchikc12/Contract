using UnityEngine;

// Маркер зоны тела: вешается на объект/коллайдер части тела.
// Сами хп хранятся в PlayerHealth, компонент только связывает объект с зоной.
[DisallowMultipleComponent]
public class BodyPartHealth : MonoBehaviour, IEffectTarget
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BodyPartType partType = BodyPartType.Torso;

    private EffectReceiver effects;

    public PlayerHealth PlayerHealth => playerHealth;
    public BodyPartType PartType => partType;
    public EffectReceiver Effects => effects ?? (effects = new EffectReceiver(this));
    public EffectTargetMask TargetMask => EffectTargets.ForBodyPart(partType);
    public float CurrentHealth => playerHealth != null ? playerHealth.GetHealth(partType) : 0f;
    public float MaxHealth => playerHealth != null ? playerHealth.GetMaxHealth(partType) : 0f;
    public float HealthPercent => playerHealth != null ? playerHealth.GetHealthPercent(partType) : 0f;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogWarning("BodyPartHealth: не найден PlayerHealth у родителя.", this);
    }

    private void OnValidate()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();
    }

    public void ApplyDamage(float amount)
    {
        if (playerHealth != null)
            playerHealth.ApplyDamage(partType, amount);
    }

    public void Heal(float amount)
    {
        if (playerHealth != null)
            playerHealth.Heal(partType, amount);
    }

    [ContextMenu("Тест: 10 урона")]
    private void DebugDamage()
    {
        ApplyDamage(10f);
    }
}
