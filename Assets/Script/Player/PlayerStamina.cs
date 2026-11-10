using UnityEngine;

// Отвечает только за стамину: расход, восстановление, порог бега и стоимость прыжка.
// Не знает ни про камеру, ни про UI — только цифры и правила.
[DisallowMultipleComponent]
public class PlayerStamina : MonoBehaviour
{
    [Header("Стамина")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegen = 12f;          // восстановление в секунду
    [SerializeField] private float regenDelay = 0.8f;           // пауза перед восстановлением
    [SerializeField] private float sprintDrain = 15f;           // расход при беге в секунду
    [SerializeField] private float walkOverweightDrain = 10f;   // базовый расход при перевесе
    [SerializeField] private float jumpCost = 15f;              // стоимость прыжка
    [SerializeField] private float sprintResumeThreshold = 12f; // % стамины для разблокировки бега

    [Header("Вес (заглушка)")]
    [SerializeField] private float carryWeight = 0f;               // текущий вес, пока задаётся вручную
    [SerializeField] private float maxCarryWeight = 50f;           // лимит без штрафа
    [SerializeField] private float overweightDrainMultiplier = 1f; // рост расхода от перевеса

    public float Stamina => stamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => maxStamina > 0f ? Mathf.Clamp01(stamina / maxStamina) * 100f : 0f;
    public float JumpCost => jumpCost;
    public bool IsSprinting => isSprinting;

    // Бег недоступен: стамины мало и сейчас не бежим (для подсветки в UI)
    public bool SprintBlocked => !isSprinting && StaminaPercent <= sprintResumeThreshold;

    private float stamina;
    private float regenBlockedUntil;
    private bool isSprinting;
    private bool initialized;

    private float Overweight => Mathf.Max(0f, carryWeight - maxCarryWeight);
    private float OverweightRatio => maxCarryWeight > 0f ? Overweight / maxCarryWeight : 0f;

    private void Awake()
    {
        stamina = maxStamina;
        initialized = true;
    }

    // Вызывается каждый кадр до расчёта скорости. Возвращает, бежим ли сейчас.
    public bool EvaluateSprint(bool sprintHeld, bool moving)
    {
        if (!initialized)
        {
            stamina = maxStamina; // страховка после горячей перезагрузки
            initialized = true;
        }

        // Уже начатый бег можно продолжать до нуля, начать заново — только выше порога
        bool canContinue = isSprinting && stamina > 0f;
        bool canStart = stamina > 0f && StaminaPercent > sprintResumeThreshold;

        isSprinting = sprintHeld && moving && (canContinue || canStart);
        return isSprinting;
    }

    // Прыжок: списывает стоимость и возвращает false, если стамины не хватает
    public bool TryJump()
    {
        if (stamina < jumpCost) return false;

        stamina -= jumpCost;
        regenBlockedUntil = Time.time + regenDelay;
        return true;
    }

    // Вызывается после движения: трата на бег/перевес и восстановление
    public void Tick(float dt, bool moving, bool grounded)
    {
        float drain = 0f;

        if (isSprinting && grounded)
            drain += sprintDrain;
        else if (moving && grounded && Overweight > 0f)
            drain += walkOverweightDrain * (1f + OverweightRatio * overweightDrainMultiplier);

        if (drain > 0f)
        {
            stamina = Mathf.Max(0f, stamina - drain * dt);
            regenBlockedUntil = Time.time + regenDelay;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isSprinting = false;
            }
        }
        else if (Time.time >= regenBlockedUntil)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegen * dt);
        }
    }

    // Точка входа для будущей системы веса
    public void SetCarryWeight(float weight) => carryWeight = Mathf.Max(0f, weight);
}
