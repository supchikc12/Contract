using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Ссылки на части тела")]
    [SerializeField] private Transform head;   // Точка головы (GG/Head), поворачивается первой
    [SerializeField] private Transform body;   // Тело (этот объект), догоняет голову по углу

    [Header("Интерфейс инвенторя")]
    [SerializeField] private GameObject Inventory;
    [Header("Настройки")]
    [Range(0.1f, 9f)]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] public float speed = 5f;
    [SerializeField] public float gravity = -9.81f; // Сила гравитации
    [SerializeField] public float jumpHeight = 1.5f;

    [Header("Ограничения")]
    [Range(0f, 90f)]
    [SerializeField] private float yRotationLimit = 88f;
    [Range(5f, 120f)]
    [SerializeField] private float headYawLimit = 60f; // до этого угла крутится только голова

    [Header("Прыжок")]
    [SerializeField] private float coyoteTime = 0.12f; // запас времени после схода с земли
    [SerializeField] private float jumpBuffer = 0.1f;  // запас времени на нажатие пробела

    [Header("Бег и приседание")]
    [SerializeField] private float sprintSpeed = 8f;      // скорость бега (Shift)
    [SerializeField] private float crouchMoveSpeed = 2f;  // скорость в приседе (Ctrl)
    [Range(0.3f, 0.9f)]
    [SerializeField] private float crouchHeightRatio = 0.55f; // доля высоты капсулы и камеры в приседе
    [SerializeField] private float crouchTransitionSpeed = 10f; // скорость сглаживания приседания

    [Header("Тряска камеры")]
    [SerializeField] private float bobFrequency = 12f; // частота шагов
    [SerializeField] private float bobAmount = 0.045f; // амплитуда тряски в метрах
    [SerializeField] private float bobSmoothing = 20f; // сглаживание тряски (больше = резче)
    [Range(0.2f, 1f)]
    [SerializeField] private float bobSharpness = 0.35f; // 1 = плавная синусоида, меньше = резче

    [Header("Тряска при прыжке и приземлении")]
    [SerializeField] private float landShakeStrength = 0.6f;  // сила толчка при приземлении
    [SerializeField] private float jumpShakeStrength = 0.25f; // сила толчка при прыжке
    [SerializeField] private float impactDistance = 0.08f;    // сдвиг камеры при толчке, м
    [SerializeField] private float impactRecovery = 9f;       // скорость затухания толчка
    [SerializeField] private float landMinFallSpeed = 3f;     // минимальная скорость падения для тряски

    // Внутренние переменные для хранения текущего угла поворота
    private Vector2 rotation = Vector2.zero; // x — yaw головы, y — pitch головы

    // Имена осей в Input Manager. Храним их как константы, чтобы не создавать мусор в памяти
    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";

    private CharacterController controller;
    private Vector3 velocity;
    private float lastGroundedTime;
    private float jumpBufferedUntil;
    private bool isCrouching;
    private bool isSprinting;
    private bool baseCaptured;
    private float standHeight;
    private Vector3 standCenter;
    private Vector3 baseHeadLocalPosition;
    private float currentHeight;
    private float currentCameraY;
    private float bobTimer;
    private Vector3 bobOffset;
    private float impact;
    private bool wasGrounded;
    private float lastAirVelocityY;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // ЗАБЛОКИРОВАТЬ КУРСОР В ЦЕНТРЕ ЭКРАНА
        Cursor.lockState = CursorLockMode.Locked;

        EnsureReferences();

        InventoryManager inv = gameObject.GetComponentInChildren<InventoryManager>(true);
        if (inv == null)
        {
            Debug.LogError("InventoryManager не найден!");
            return;
        }
        Inventory = inv.gameObject;
    }

    private void Start()
    {
        // Сцена может быть сохранена с открытым инвентарём — игру начинаем с закрытым.
        // Start, а не Awake: к этому моменту все слоты и сетки уже созданы.
        if (Inventory != null && Inventory.activeSelf)
            Inventory.SetActive(false);
    }

    // Держим ссылки живыми и после горячей перезагрузки в Play Mode (Awake повторно не вызывается)
    private void EnsureReferences()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (body == null) body = transform;

        if (head == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null && cam.transform.parent != null)
                head = cam.transform.parent;
        }
        if (head == null) head = transform;
        CaptureBase();
    }

    private void CaptureBase()
    {
        if (baseCaptured) return;
        standHeight = controller.height;
        standCenter = controller.center;
        baseHeadLocalPosition = head.localPosition;
        currentHeight = standHeight;
        currentCameraY = baseHeadLocalPosition.y;
        baseCaptured = true;
    }

    private void UpdateStance(float dt)
    {
        // Высота капсулы: дно остаётся на месте, центр пересчитываем от дна
        float targetHeight = isCrouching ? standHeight * crouchHeightRatio : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * dt);
        controller.height = currentHeight;

        float bottom = standCenter.y - standHeight * 0.5f;
        controller.center = new Vector3(standCenter.x, bottom + currentHeight * 0.5f, standCenter.z);

        float targetCameraY = isCrouching ? baseHeadLocalPosition.y * crouchHeightRatio : baseHeadLocalPosition.y;
        currentCameraY = Mathf.Lerp(currentCameraY, targetCameraY, crouchTransitionSpeed * dt);
    }

    private void UpdateCameraBob(float dt, Vector2 input, bool grounded)
    {
        bool moving = grounded && input.sqrMagnitude > 0.01f;
        float speedFactor = isSprinting ? 1.4f : (isCrouching ? 0.6f : 1f);

        if (moving) bobTimer += dt * bobFrequency * speedFactor;

        float amplitude = moving ? bobAmount * speedFactor : 0f;
        // Резкая форма волны: степень < 1 делает рывок у нуля вместо плавной синусоиды
        float wave = Mathf.Sin(bobTimer);
        float shaped = Mathf.Sign(wave) * Mathf.Pow(Mathf.Abs(wave), bobSharpness);

        Vector3 target = new Vector3(
            Mathf.Cos(bobTimer * 0.5f) * amplitude * 0.6f,
            shaped * amplitude,
            0f);

        bobOffset = Vector3.Lerp(bobOffset, target, bobSmoothing * dt);

        impact = Mathf.Lerp(impact, 0f, impactRecovery * dt);

        head.localPosition = new Vector3(
            baseHeadLocalPosition.x + bobOffset.x,
            currentCameraY + bobOffset.y - impact * impactDistance,
            baseHeadLocalPosition.z + bobOffset.z);
    }

    private void Update()
    {
        EnsureReferences();
        CameraLook();

        if (Input.GetKeyDown(KeyCode.I)) {
            OpenOrCloseInventory();
        }

        // 1. Получаем ввод (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Приземление: запоминаем скорость падения и даём толчок камере
        bool groundedNow = controller.isGrounded;
        if (!groundedNow) lastAirVelocityY = velocity.y;

        if (groundedNow && !wasGrounded && lastAirVelocityY < -landMinFallSpeed)
        {
            float strength = Mathf.Clamp01(-lastAirVelocityY / (landMinFallSpeed * 3f));
            impact = landShakeStrength * Mathf.Max(strength, 0.25f);
        }
        wasGrounded = groundedNow;

        // 2. Бег и приседание: Shift — бег, Ctrl — присесть (в приседе бег выключен)
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        float currentSpeed = isCrouching ? crouchMoveSpeed : (isSprinting ? sprintSpeed : speed);

        // 3. Движение по направлению взгляда (камеры/головы), а не тела.
        // Проецируем направления на горизонталь, чтобы наклон головы не менял скорость.
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        if (right.sqrMagnitude < 0.0001f) right = transform.right;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (right * x + forward * z) * currentSpeed;

        // Плавное приседание: высота капсулы и высота камеры
        UpdateStance(Time.deltaTime);

        // 4. Прыжок: запоминаем время последнего касания земли и нажатия пробела
        if (controller.isGrounded)
            lastGroundedTime = Time.time;

        if (Input.GetButtonDown("Jump"))
            jumpBufferedUntil = Time.time + jumpBuffer;

        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        if (Time.time <= jumpBufferedUntil && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            impact = -jumpShakeStrength; // небольшой подброс камеры при прыжке
            jumpBufferedUntil = 0f;
        }

        // 5. Гравитация
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // Небольшой прижим к земле

        velocity.y += gravity * Time.deltaTime;

        // 6. ОДИН Move за кадр: два вызова ломали коллизии и прыжки
        controller.Move((move + velocity) * Time.deltaTime);

        // Тряска камеры при движении (затухает на месте)
        UpdateCameraBob(Time.deltaTime, new Vector2(x, z), controller.isGrounded);
    }

    void OpenOrCloseInventory() {

        if (Inventory == null) return;

        Inventory.SetActive(!Inventory.activeSelf);
        if (Inventory.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None; // Мышь свободно двигается
            Cursor.visible = true;                  // Показываем курсор
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Захватываем мышь в центр
            Cursor.visible = false;                   // Прячем курсор
            if (WeaponModdingUI.Instance != null)
                WeaponModdingUI.Instance.Close();
        }
    }

    void CameraLook() {
        // 1. Получаем ввод от мыши и умножаем на чувствительность
        float mouseX = Input.GetAxis(MouseXAxis) * sensitivity;
        float mouseY = Input.GetAxis(MouseYAxis) * sensitivity;

        // 2. Горизонталь: сначала крутится голова, тело подключается после headYawLimit
        rotation.x += mouseX;

        if (rotation.x > headYawLimit)
        {
            float excess = rotation.x - headYawLimit;
            body.Rotate(0f, excess, 0f);   // тело догоняет на превышение порога
            rotation.x = headYawLimit;     // голова остаётся на границе
        }
        else if (rotation.x < -headYawLimit)
        {
            float excess = rotation.x + headYawLimit;
            body.Rotate(0f, excess, 0f);
            rotation.x = -headYawLimit;
        }

        // 3. Вертикаль: только голова, тело не наклоняется
        // Мышь вверх даёт положительный mouseY, а в Unity взгляд вверх — отрицательный угол по X.
        // Вычитаем, иначе вертикаль получается инвертированной.
        rotation.y -= mouseY;
        rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);

        // 4. Поворот применяем к голове, а не ко всему игроку
        head.localRotation = Quaternion.Euler(rotation.y, rotation.x, 0f);
    }

    // Публичный метод, чтобы можно было менять чувствительность из других скриптов (например, из меню настроек)
    public void SetSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
    }

}
