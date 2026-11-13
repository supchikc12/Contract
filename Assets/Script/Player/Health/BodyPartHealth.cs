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
    public EffectTargetMask TargetMask => EffectTargets.ForBodyPart(partType);

    public EffectReceiver Effects
    {
        get
        {
            if (effects == null)
            {
                effects = new EffectReceiver(this);
                effects.PeriodicTick += OnPeriodicTick;
            }

            return effects;
        }
    }
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

    public void ApplyDamage(DamageInfo info)
    {
        if (playerHealth != null)
            playerHealth.ApplyDamage(partType, info);
    }

    public void ApplyPeriodicEffect(ActiveEffect effect, PeriodicEffect periodic)
    {
        OnPeriodicTick(effect, periodic);
    }

    private void OnPeriodicTick(ActiveEffect effect, PeriodicEffect periodic)
    {
        if (periodic == null || playerHealth == null) return;

        float amount = periodic.amountPerTick * (effect != null ? effect.Stacks : 1);

        if (periodic.healing)
        {
            playerHealth.Heal(partType, amount);
            return;
        }

        if (!periodic.canKill)
        {
            float health = playerHealth.GetHealth(partType);
            if (health <= 1f) return;
            amount = Mathf.Min(amount, health - 1f);
        }

        DamageInfo info = new DamageInfo(
            amount,
            periodic.damageType,
            partType,
            effect != null ? effect.Source : null);

        playerHealth.ApplyDamage(partType, info);
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
