using UnityEngine;

// Обзор от первого лица и камера: мышь, наклон головы, доворот тела,
// шаговая тряска и толчки при прыжке/приземлении.
// Не двигает игрока и не управляет инвентарём.
public class PlayerCamera : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform body;
    [SerializeField] private PlayerMotor motor;

    [Header("Чувствительность")]
    [Range(0.1f, 9f)]
    [SerializeField] private float sensitivity = 2f;
    [Range(0f, 90f)]
    [SerializeField] private float yRotationLimit = 88f;
    [Range(5f, 120f)]
    [SerializeField] private float headYawLimit = 60f; // до этого угла крутится только голова

    [Header("Доворот тела")]
    [SerializeField] private float bodyAlignSpeed = 240f; // градусов/сек; 0 = выключить

    [Header("Шаговая тряска")]
    [SerializeField] private float bobFrequency = 12f;
    [SerializeField] private float bobAmount = 0.045f;
    [SerializeField] private float bobSmoothing = 20f;
    [Range(0.2f, 1f)]
    [SerializeField] private float bobSharpness = 0.35f;

    [Header("Прыжок и приземление")]
    [SerializeField] private float landShakeStrength = 0.6f;
    [SerializeField] private float jumpShakeStrength = 0.25f;
    [SerializeField] private float impactDistance = 0.08f;
    [SerializeField] private float impactRecovery = 9f;

    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";

    private Vector2 rotation = Vector2.zero; // x — yaw головы, y — pitch головы
    private Vector3 baseHeadLocalPosition;
    private float currentCameraY;
    private float bobTimer;
    private Vector3 bobOffset;
    private float impact;
    private bool baseCaptured;

    public Transform Head => head;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (motor == null) return;

        motor.Jumped += OnJumped;
        motor.Landed += OnLanded;
    }

    private void OnDisable()
    {
        if (motor == null) return;

        motor.Jumped -= OnJumped;
        motor.Landed -= OnLanded;
    }

    private void EnsureReferences()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
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

        baseHeadLocalPosition = head.localPosition;
        currentCameraY = baseHeadLocalPosition.y;
        baseCaptured = true;
    }

    private void Update()
    {
        EnsureReferences();

        // Пока открыт инвентарь, мышь не крутит камеру.
        if (motor != null && !motor.InputEnabled) return;

        CameraLook();
    }

    private void LateUpdate()
    {
        EnsureReferences();
        if (motor == null) return;

        AlignBodyToView();
        UpdateCameraHeight();
        UpdateCameraBob();
    }

    private void CameraLook()
    {
        float mouseX = Input.GetAxis(MouseXAxis) * sensitivity;
        float mouseY = Input.GetAxis(MouseYAxis) * sensitivity;

        // Горизонталь: сначала голова, тело подключается после headYawLimit.
        rotation.x += mouseX;

        if (rotation.x > headYawLimit)
        {
            float excess = rotation.x - headYawLimit;
            body.Rotate(0f, excess, 0f);
            rotation.x = headYawLimit;
        }
        else if (rotation.x < -headYawLimit)
        {
            float excess = rotation.x + headYawLimit;
            body.Rotate(0f, excess, 0f);
            rotation.x = -headYawLimit;
        }

        // Вертикаль: только голова.
        rotation.y -= mouseY;
        rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);

        head.localRotation = Quaternion.Euler(rotation.y, rotation.x, 0f);
    }

    private void AlignBodyToView()
    {
        if (!motor.IsMoving || bodyAlignSpeed <= 0f) return;

        // Тело плавно доворачивается, голова компенсирует поворот.
        float maxStep = bodyAlignSpeed * Time.deltaTime;
        float step = Mathf.Clamp(rotation.x, -maxStep, maxStep);
        if (Mathf.Abs(step) <= 0.001f) return;

        body.Rotate(0f, step, 0f);
        rotation.x -= step;
        head.localRotation = Quaternion.Euler(rotation.y, rotation.x, 0f);
    }

    private void UpdateCameraHeight()
    {
        // Высота камеры повторяет сжатие капсулы при приседе.
        float ratio = motor.StandHeight > 0.01f
            ? Mathf.Clamp(motor.CurrentHeight / motor.StandHeight, 0.3f, 1f)
            : 1f;

        float targetY = baseHeadLocalPosition.y * ratio;
        currentCameraY = Mathf.Lerp(currentCameraY, targetY, motor.CrouchTransitionSpeed * Time.deltaTime);
    }

    private void UpdateCameraBob()
    {
        float dt = Time.deltaTime;
        bool moving = motor.IsGrounded && motor.IsMoving;
        float speedFactor = motor.IsSprinting ? 1.4f : (motor.IsCrouching ? 0.6f : 1f);

        if (moving) bobTimer += dt * bobFrequency * speedFactor;

        float amplitude = moving ? bobAmount * speedFactor : 0f;
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

    private void OnJumped()
    {
        impact = -jumpShakeStrength;
    }

    private void OnLanded(float strength)
    {
        impact = landShakeStrength * Mathf.Max(strength, 0.25f);
    }
}
