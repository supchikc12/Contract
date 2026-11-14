using System.Collections.Generic;
using UnityEngine;

// Процедурная анимация готовой модели игрока.
// Сама модель - объект сцены Player/VisualBody; скрипт только вращает её части.
// Гибридный режим: если у skinned-модели есть Animator (запечённые клипы),
// ведущей становится модель, а невидимый коллайдерный риг повторяет её позу.
[DisallowMultipleComponent]
public class PlayerProceduralBody : MonoBehaviour
{
    [Header("Ссылки на части модели")]
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform upperBody;

    [SerializeField] private Transform leftHip;
    [SerializeField] private Transform rightHip;
    [SerializeField] private Transform leftKnee;
    [SerializeField] private Transform rightKnee;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;

    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;
    [SerializeField] private Transform leftElbow;
    [SerializeField] private Transform rightElbow;

    [Header("Skinned-модель (необязательно)")]
    [SerializeField] private Transform modelRoot;   // Корень низкополигональной модели
    [SerializeField] private bool driveModelBones = true;
    [SerializeField] private bool syncRigFromModel = true; // Риг ведётся от костей модели

    [Header("Анимация")]
    [SerializeField] private float walkSwing = 24f;       // Амплитуда шага на ходьбе, градусы
    [SerializeField] private float runSwing = 42f;        // Амплитуда на бегу
    [SerializeField] private float walkCadence = 7f;      // Темп шагов на ходьбе, рад/с
    [SerializeField] private float runCadence = 14f;      // Темп на бегу
    [SerializeField] private float leanAngle = 7f;        // Наклон корпуса при движении
    [SerializeField] private float crouchKneeBend = 45f;  // Сгиб коленей в приседе
    [SerializeField] private float smoothing = 12f;       // Сглаживание углов

    private CharacterController controller;
    private Transform bodyAnchor;

    private float standHeight;
    private float gaitPhase;
    private float currentLean;

    private float leftHipAngle;
    private float rightHipAngle;
    private float leftKneeAngle;
    private float rightKneeAngle;
    private float leftFootAngle;
    private float rightFootAngle;
    private float leftArmAngle;
    private float rightArmAngle;

    private bool ready;

    // Привязка кости модели к части старого рига
    private class BoneBind
    {
        public Transform part;
        public Quaternion partRestWorld;
        public Transform bone;
        public Quaternion boneRestWorld;
    }

    private readonly List<BoneBind> modelBinds = new List<BoneBind>();

    private Animator modelAnimator;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        controller = GetComponent<CharacterController>();
        bodyAnchor = motor != null ? motor.transform : transform;
        ResolveRig();
    }

    private void Start()
    {
        if (!ready) ResolveRig();
        ResolveModelBones();

        // Гибридный режим включается только при наличии Animator на модели
        modelAnimator = modelRoot != null ? modelRoot.GetComponentInChildren<Animator>(true) : null;
        syncRigFromModel = syncRigFromModel && modelAnimator != null;

        standHeight = motor != null ? motor.StandHeight : 0f;
        if (standHeight <= 0.01f && controller != null) standHeight = controller.height;

        ready = visualRoot != null && upperBody != null
            && leftHip != null && rightHip != null
            && leftKnee != null && rightKnee != null
            && leftFoot != null && rightFoot != null
            && leftShoulder != null && rightShoulder != null
            && leftElbow != null && rightElbow != null;

        if (!ready)
        {
            Debug.LogError("PlayerProceduralBody: не найдены части модели Player/VisualBody. Анимация тела отключена.", this);
            enabled = false;
        }
    }

    // Ищет части модели по именам, если ссылки не назначены в инспекторе
    private void ResolveRig()
    {
        if (visualRoot == null) visualRoot = transform.Find("VisualBody");
        if (visualRoot == null) return;

        if (upperBody == null) upperBody = visualRoot.Find("UpperBody");

        if (leftHip == null) leftHip = visualRoot.Find("Hip_L");
        if (rightHip == null) rightHip = visualRoot.Find("Hip_R");

        if (leftKnee == null && leftHip != null) leftKnee = leftHip.Find("Knee_L");
        if (rightKnee == null && rightHip != null) rightKnee = rightHip.Find("Knee_R");

        if (leftFoot == null && leftKnee != null) leftFoot = leftKnee.Find("Ankle_L");
        if (rightFoot == null && rightKnee != null) rightFoot = rightKnee.Find("Ankle_R");

        if (leftShoulder == null && upperBody != null) leftShoulder = upperBody.Find("Shoulder_L");
        if (rightShoulder == null && upperBody != null) rightShoulder = upperBody.Find("Shoulder_R");

        if (leftElbow == null && leftShoulder != null) leftElbow = leftShoulder.Find("Elbow_L");
        if (rightElbow == null && rightShoulder != null) rightElbow = rightShoulder.Find("Elbow_R");
    }

    // Находит кости skinned-модели и запоминает их позу покоя
    private void ResolveModelBones()
    {
        modelBinds.Clear();
        if (!driveModelBones) return;

        if (modelRoot == null && visualRoot != null)
            modelRoot = visualRoot.Find("PlayerModel");

        if (modelRoot == null) return;

        string[] boneNames =
        {
            "Торс",
            "Бедро_L", "Ляжка_L", "Ступня_L",
            "Бедро_R", "Ляжка_R", "Ступня_R",
            "Плечо_L", "Предплечье_L",
            "Плечо_R", "Предплечье_R"
        };

        Transform[] parts =
        {
            upperBody,
            leftHip, leftKnee, leftFoot,
            rightHip, rightKnee, rightFoot,
            leftShoulder, leftElbow,
            rightShoulder, rightElbow
        };

        for (int i = 0; i < boneNames.Length; i++)
        {
            Transform bone = FindDeep(modelRoot, boneNames[i]);
            Transform part = i < parts.Length ? parts[i] : null;
            if (bone == null || part == null) continue;

            modelBinds.Add(new BoneBind
            {
                part = part,
                partRestWorld = part.rotation,
                bone = bone,
                boneRestWorld = bone.rotation
            });
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;

        foreach (Transform child in root)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }

        return null;
    }

    // Синхронизация модели и рига: ведущий определяется режимом
    private void LateUpdate()
    {
        if (syncRigFromModel)
        {
            // Запечённая анимация: риг повторяет позу костей модели
            for (int i = 0; i < modelBinds.Count; i++)
            {
                BoneBind bind = modelBinds[i];
                if (bind.part == null || bind.bone == null) continue;

                bind.part.rotation = bind.bone.rotation
                    * Quaternion.Inverse(bind.boneRestWorld)
                    * bind.partRestWorld;
            }

            return;
        }

        // Процедурная анимация: кости модели повторяют позу рига
        for (int i = 0; i < modelBinds.Count; i++)
        {
            BoneBind bind = modelBinds[i];
            if (bind.part == null || bind.bone == null) continue;

            bind.bone.rotation = bind.part.rotation
                * Quaternion.Inverse(bind.partRestWorld)
                * bind.boneRestWorld;
        }
    }

    private void Update()
    {
        if (!ready || controller == null) return;

        // В гибридном режиме позу задаёт Animator, процедурка не вмешивается
        if (syncRigFromModel) return;

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;
        Vector3 localVelocity = bodyAnchor.InverseTransformDirection(velocity);
        bool moving = controller.isGrounded && speed > 0.1f;

        float speed01 = Mathf.InverseLerp(0.1f, 8f, speed);
        Vector3 direction = localVelocity.sqrMagnitude > 0.0001f
            ? localVelocity.normalized
            : Vector3.forward;

        // Ось шага - перпендикуляр к направлению движения в плоскости земли
        Vector3 swingAxis = Vector3.Cross(Vector3.up, direction);
        if (swingAxis.sqrMagnitude < 0.0001f) swingAxis = Vector3.right;
        swingAxis.Normalize();

        if (moving)
            gaitPhase += Time.deltaTime * Mathf.Lerp(walkCadence, runCadence, speed01);

        float swingAmplitude = Mathf.Lerp(walkSwing, runSwing, speed01);
        float swing = moving ? Mathf.Sin(gaitPhase) * swingAmplitude : 0f;

        float crouchFactor = standHeight > 0.01f
            ? Mathf.InverseLerp(standHeight, standHeight * 0.55f, controller.height)
            : 0f;

        float kneeBend = Mathf.Lerp(25f, 50f, speed01);
        float targetLeftHip = swing;
        float targetRightHip = -swing;
        float targetLeftKnee = (moving ? Mathf.Max(0f, Mathf.Sin(gaitPhase)) * kneeBend : 0f)
            + crouchFactor * crouchKneeBend;
        float targetRightKnee = (moving ? Mathf.Max(0f, -Mathf.Sin(gaitPhase)) * kneeBend : 0f)
            + crouchFactor * crouchKneeBend;

        leftHipAngle = Mathf.Lerp(leftHipAngle, targetLeftHip, smoothing * Time.deltaTime);
        rightHipAngle = Mathf.Lerp(rightHipAngle, targetRightHip, smoothing * Time.deltaTime);
        leftKneeAngle = Mathf.Lerp(leftKneeAngle, targetLeftKnee, smoothing * Time.deltaTime);
        rightKneeAngle = Mathf.Lerp(rightKneeAngle, targetRightKnee, smoothing * Time.deltaTime);

        leftFootAngle = Mathf.Lerp(leftFootAngle, -(leftHipAngle + leftKneeAngle) * 0.6f,
            smoothing * Time.deltaTime);
        rightFootAngle = Mathf.Lerp(rightFootAngle, -(rightHipAngle + rightKneeAngle) * 0.6f,
            smoothing * Time.deltaTime);

        float armAmplitude = moving ? swingAmplitude * 0.7f : 0f;
        float armSwing = moving ? Mathf.Sin(gaitPhase) * armAmplitude : 0f;
        leftArmAngle = Mathf.Lerp(leftArmAngle, -armSwing, smoothing * Time.deltaTime);
        rightArmAngle = Mathf.Lerp(rightArmAngle, armSwing, smoothing * Time.deltaTime);

        float targetLean = moving ? Mathf.Lerp(0f, leanAngle, speed01) : 0f;
        currentLean = Mathf.Lerp(currentLean, targetLean, smoothing * Time.deltaTime);

        // Присед: опускаем всю модель, чтобы ноги сгибались в коленях
        float crouchDrop = Mathf.Max(0f, standHeight - controller.height) * 0.5f;
        visualRoot.localPosition = new Vector3(0f, -crouchDrop, 0f);

        leftHip.localRotation = Quaternion.AngleAxis(leftHipAngle, swingAxis);
        rightHip.localRotation = Quaternion.AngleAxis(rightHipAngle, swingAxis);
        leftKnee.localRotation = Quaternion.AngleAxis(leftKneeAngle, swingAxis);
        rightKnee.localRotation = Quaternion.AngleAxis(rightKneeAngle, swingAxis);
        leftFoot.localRotation = Quaternion.AngleAxis(leftFootAngle, swingAxis);
        rightFoot.localRotation = Quaternion.AngleAxis(rightFootAngle, swingAxis);

        leftShoulder.localRotation = Quaternion.AngleAxis(leftArmAngle, swingAxis);
        rightShoulder.localRotation = Quaternion.AngleAxis(rightArmAngle, swingAxis);
        leftElbow.localRotation = Quaternion.AngleAxis(8f + Mathf.Abs(leftArmAngle) * 0.3f, swingAxis);
        rightElbow.localRotation = Quaternion.AngleAxis(8f + Mathf.Abs(rightArmAngle) * 0.3f, swingAxis);

        upperBody.localRotation = Quaternion.AngleAxis(currentLean, swingAxis);
    }
}
