using UnityEngine;

// Мост между геймплеем и Animator запечённой анимации модели игрока.
// Пишет параметры Speed / Strafe / Crouch / Lean / Grounded по договорённости
// гибридной анимации; самих клипов не требует и без Animator просто ничего не делает.
[DisallowMultipleComponent]
public class PlayerAnimationDriver : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform modelRoot;

    [Header("Сглаживание")]
    [SerializeField] private float speedSmoothing = 10f;
    [SerializeField] private float strafeSmoothing = 10f;
    [SerializeField] private float crouchSmoothing = 8f;
    [SerializeField] private float referenceSpeed = 8f;  // Скорость, при которой Speed = 1

    private CharacterController controller;
    private float standHeight;

    private float currentSpeed;
    private float currentStrafe;
    private float currentCrouch;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        controller = GetComponent<CharacterController>();

        standHeight = motor != null && motor.StandHeight > 0.01f
            ? motor.StandHeight
            : (controller != null ? controller.height : 0f);

        ResolveAnimator();
    }

    private void Start()
    {
        ResolveAnimator();
    }

    // Находит Animator на модели игрока, если ссылка не назначена
    private void ResolveAnimator()
    {
        if (animator != null) return;

        if (modelRoot == null)
            modelRoot = transform.Find("VisualBody/PlayerModel");

        if (modelRoot != null)
            animator = modelRoot.GetComponentInChildren<Animator>(true);
    }

    private void Update()
    {
        ResolveAnimator();
        if (animator == null || controller == null) return;

        // Горизонтальная скорость без гравитации
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        Vector3 local = transform.InverseTransformDirection(velocity);
        float strafe = speed > 0.1f ? Mathf.Clamp(local.x / speed, -1f, 1f) : 0f;
        float lean = speed > 0.1f ? Mathf.Clamp(local.z / speed, -1f, 1f) : 0f;

        float crouch = standHeight > 0.01f
            ? Mathf.InverseLerp(standHeight, standHeight * 0.55f, controller.height)
            : 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, speed, speedSmoothing * Time.deltaTime);
        currentStrafe = Mathf.Lerp(currentStrafe, strafe, strafeSmoothing * Time.deltaTime);
        currentCrouch = Mathf.Lerp(currentCrouch, crouch, crouchSmoothing * Time.deltaTime);

        animator.SetFloat("Speed", currentSpeed / Mathf.Max(0.1f, referenceSpeed));
        animator.SetFloat("Strafe", currentStrafe);
        animator.SetFloat("Crouch", currentCrouch);
        animator.SetFloat("Lean", lean);
        animator.SetBool("Grounded", controller.isGrounded);
    }
}
