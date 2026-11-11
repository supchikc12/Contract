using UnityEngine;

// Движение игрока: WASD, бег, присед, прыжок, гравитация и CharacterController.
// Обзор — PlayerCamera, инвентарь — PlayerInventoryToggle, стамина — PlayerStamina.
public class PlayerMotor : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform view; // направление движения (голова/камера)
    [SerializeField] private PlayerStamina staminaComponent;

    [Header("Настройки")]
    [SerializeField] public float speed = 5f;
    [SerializeField] public float gravity = -9.81f;
    [SerializeField] public float jumpHeight = 1.5f;

    [Header("Прыжок")]
    [SerializeField] private float coyoteTime = 0.12f; // запас времени после схода с земли
    [SerializeField] private float jumpBuffer = 0.1f;  // запас времени на нажатие пробела
    [SerializeField] private float landMinFallSpeed = 3f; // минимальная скорость падения для тряски

    [Header("Бег и приседание")]
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchMoveSpeed = 2f;
    [Range(0.3f, 0.9f)]
    [SerializeField] private float crouchHeightRatio = 0.55f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private CharacterController controller;
    private Vector3 velocity;
    private float lastGroundedTime;
    private float jumpBufferedUntil;
    private bool isCrouching;
    private bool isSprinting;
    private bool baseCaptured;
    private float standHeight;
    private Vector3 standCenter;
    private float currentHeight;
    private bool wasGrounded;
    private float lastAirVelocityY;
    private Vector2 moveInput;

    // События для камеры: прыжок и приземление не должны тянуть сюда визуал.
    public event System.Action Jumped;
    public event System.Action<float> Landed;

    public CharacterController Controller => controller;
    public float StandHeight => standHeight;
    public float CurrentHeight => currentHeight;
    public float CrouchTransitionSpeed => crouchTransitionSpeed;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public Vector2 MoveInput => moveInput;

    // Ввод отключается, пока открыт инвентарь или другое окно.
    public bool InputEnabled { get; set; } = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        if (view == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null && cam.transform.parent != null)
                view = cam.transform.parent;
        }
        if (view == null) view = transform;

        if (staminaComponent == null) staminaComponent = GetComponent<PlayerStamina>();

        CaptureBase();
    }

    private void CaptureBase()
    {
        if (baseCaptured || controller == null) return;

        standHeight = controller.height;
        standCenter = controller.center;
        currentHeight = standHeight;
        baseCaptured = true;
    }

    private void Update()
    {
        EnsureReferences();

        float x = 0f;
        float z = 0f;
        if (InputEnabled)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
        }
        moveInput = new Vector2(x, z);

        // Приземление: сообщаем камере силу удара, визуал остаётся в PlayerCamera.
        bool groundedNow = controller.isGrounded;
        if (!groundedNow) lastAirVelocityY = velocity.y;

        if (groundedNow && !wasGrounded && lastAirVelocityY < -landMinFallSpeed)
        {
            float strength = Mathf.Clamp01(-lastAirVelocityY / (landMinFallSpeed * 3f));
            Landed?.Invoke(strength);
        }
        wasGrounded = groundedNow;

        if (InputEnabled)
            isCrouching = Input.GetKey(KeyCode.LeftControl);

        bool moving = moveInput.sqrMagnitude > 0.01f;
        bool sprintHeld = InputEnabled && Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        isSprinting = staminaComponent != null && staminaComponent.EvaluateSprint(sprintHeld, moving);

        float currentSpeed = isCrouching ? crouchMoveSpeed : (isSprinting ? sprintSpeed : speed);

        // Движение по направлению взгляда, а не тела.
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        if (right.sqrMagnitude < 0.0001f) right = transform.right;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (right * x + forward * z) * currentSpeed;

        UpdateStance(Time.deltaTime);

        // Прыжок: coyote time + буфер нажатия.
        if (controller.isGrounded)
            lastGroundedTime = Time.time;

        if (InputEnabled && Input.GetButtonDown("Jump"))
            jumpBufferedUntil = Time.time + jumpBuffer;

        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        if (Time.time <= jumpBufferedUntil && canJump && staminaComponent != null && staminaComponent.TryJump())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferedUntil = 0f;
            Jumped?.Invoke();
        }

        // Гравитация и один Move за кадр.
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime);

        if (staminaComponent != null)
            staminaComponent.Tick(Time.deltaTime, moving, controller.isGrounded);
    }

    private void UpdateStance(float dt)
    {
        // Высота капсулы: дно остаётся на месте, центр пересчитываем от дна.
        float targetHeight = isCrouching ? standHeight * crouchHeightRatio : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * dt);
        controller.height = currentHeight;

        float bottom = standCenter.y - standHeight * 0.5f;
        controller.center = new Vector3(standCenter.x, bottom + currentHeight * 0.5f, standCenter.z);
    }
}
