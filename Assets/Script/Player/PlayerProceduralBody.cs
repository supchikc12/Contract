using UnityEngine;

// Строит из примитивов человекоподобное тестовое тело и анимирует ноги/руки
// процедурно, по фактическому направлению движения. Сцену и префабы не трогает.
[DisallowMultipleComponent]
public class PlayerProceduralBody : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerManager player;

    [Header("Анимация")]
    [SerializeField] private float walkSwing = 24f;      // амплитуда шага при ходьбе, градусы
    [SerializeField] private float runSwing = 42f;       // амплитуда при беге
    [SerializeField] private float walkCadence = 7f;     // темп шагов при ходьбе, рад/с
    [SerializeField] private float runCadence = 14f;     // темп при беге
    [SerializeField] private float leanAngle = 7f;       // наклон корпуса в движении
    [SerializeField] private float crouchKneeBend = 45f; // добавка к сгибу коленей в приседе
    [SerializeField] private float smoothing = 12f;      // сглаживание углов

    private CharacterController controller;
    private Transform bodyAnchor;
    private Transform visualRoot;
    private Transform upperBody;

    private Transform leftHip;
    private Transform rightHip;
    private Transform leftKnee;
    private Transform rightKnee;
    private Transform leftFoot;
    private Transform rightFoot;

    private Transform leftShoulder;
    private Transform rightShoulder;
    private Transform leftElbow;
    private Transform rightElbow;

    private Material skinMaterial;
    private Material shirtMaterial;
    private Material pantsMaterial;
    private Material shoeMaterial;
    private Material eyeMaterial;

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

    private bool built;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerManager>();
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        Build();
    }

    private void Build()
    {
        if (built || controller == null) return;

        bodyAnchor = player != null ? player.Body : transform;
        Transform headAnchor = player != null ? player.Head : FindHeadByCamera();
        if (headAnchor == null) headAnchor = bodyAnchor;

        standHeight = player != null ? player.StandHeight : controller.height;
        if (standHeight <= 0.01f) standHeight = controller.height;

        PositionFeet(out float feetY);
        float headY = bodyAnchor.InverseTransformPoint(headAnchor.position).y;
        float height = Mathf.Max(1.2f, headY - feetY);

        // Прячем старую капсулу и сферу-голову, оставляя их коллайдеры
        HideRenderer(bodyAnchor.Find("GG"));
        HideRenderer(headAnchor);

        Transform existing = bodyAnchor.Find("VisualBody");
        if (existing != null) Destroy(existing.gameObject);

        visualRoot = new GameObject("VisualBody").transform;
        visualRoot.SetParent(bodyAnchor, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;

        CreateMaterials();

        // Пропорции от пола до центра головы
        float hipY = feetY + 0.52f * height;
        float kneeY = feetY + 0.28f * height;
        float ankleY = feetY + 0.07f * height;
        float shoulderY = feetY + 0.87f * height;
        float neckY = feetY + 0.91f * height;

        float hipHalf = 0.06f * height;
        float shoulderHalf = 0.11f * height;
        float torsoWidth = 0.19f * height;
        float torsoDepth = 0.11f * height;
        float pelvisWidth = 0.16f * height;
        float pelvisHeight = 0.09f * height;
        float pelvisDepth = 0.10f * height;

        float thighRadius = 0.032f * height;
        float shinRadius = 0.028f * height;
        float armRadius = 0.025f * height;
        float forearmRadius = 0.022f * height;
        float headRadius = 0.065f * height;
        float footLength = 0.11f * height;
        float footWidth = 0.065f * height;
        float footHeight = 0.04f * height;

        float thighLength = hipY - kneeY;
        float shinLength = kneeY - ankleY;
        float upperArmLength = 0.17f * height;
        float forearmLength = 0.14f * height;

        // Таз и корпус
        CreateBox("Pelvis", visualRoot, new Vector3(0f, hipY, 0f),
            new Vector3(pelvisWidth, pelvisHeight, pelvisDepth), pantsMaterial);

        upperBody = new GameObject("UpperBody").transform;
        upperBody.SetParent(visualRoot, false);
        upperBody.localPosition = new Vector3(0f, hipY, 0f);

        CreateBox("Torso", upperBody,
            new Vector3(0f, (shoulderY - hipY) * 0.5f, 0f),
            new Vector3(torsoWidth, shoulderY - hipY, torsoDepth), shirtMaterial);

        CreateCapsule("Neck", upperBody, new Vector3(0f, neckY - hipY, 0f),
            0.05f * height, 0.02f * height, skinMaterial);

        // Ноги: бедро -> колено -> голень -> стопа
        leftHip = CreatePivot("Hip_L", visualRoot, new Vector3(-hipHalf, hipY, 0f));
        rightHip = CreatePivot("Hip_R", visualRoot, new Vector3(hipHalf, hipY, 0f));

        CreateCapsule("Thigh_L", leftHip, new Vector3(0f, -thighLength * 0.5f, 0f),
            thighLength, thighRadius, pantsMaterial);
        CreateCapsule("Thigh_R", rightHip, new Vector3(0f, -thighLength * 0.5f, 0f),
            thighLength, thighRadius, pantsMaterial);

        leftKnee = CreatePivot("Knee_L", leftHip, new Vector3(0f, -thighLength, 0f));
        rightKnee = CreatePivot("Knee_R", rightHip, new Vector3(0f, -thighLength, 0f));

        CreateCapsule("Shin_L", leftKnee, new Vector3(0f, -shinLength * 0.5f, 0f),
            shinLength, shinRadius, pantsMaterial);
        CreateCapsule("Shin_R", rightKnee, new Vector3(0f, -shinLength * 0.5f, 0f),
            shinLength, shinRadius, pantsMaterial);

        leftFoot = CreatePivot("Ankle_L", leftKnee, new Vector3(0f, -shinLength, 0f));
        rightFoot = CreatePivot("Ankle_R", rightKnee, new Vector3(0f, -shinLength, 0f));

        CreateBox("Foot_L", leftFoot, new Vector3(0f, -footHeight * 0.15f, footLength * 0.35f),
            new Vector3(footWidth, footHeight, footLength), shoeMaterial);
        CreateBox("Foot_R", rightFoot, new Vector3(0f, -footHeight * 0.15f, footLength * 0.35f),
            new Vector3(footWidth, footHeight, footLength), shoeMaterial);

        // Руки: плечо -> локоть -> предплечье -> кисть
        leftShoulder = CreatePivot("Shoulder_L", upperBody,
            new Vector3(-shoulderHalf, shoulderY - hipY, 0f));
        rightShoulder = CreatePivot("Shoulder_R", upperBody,
            new Vector3(shoulderHalf, shoulderY - hipY, 0f));

        CreateCapsule("UpperArm_L", leftShoulder, new Vector3(0f, -upperArmLength * 0.5f, 0f),
            upperArmLength, armRadius, shirtMaterial);
        CreateCapsule("UpperArm_R", rightShoulder, new Vector3(0f, -upperArmLength * 0.5f, 0f),
            upperArmLength, armRadius, shirtMaterial);

        leftElbow = CreatePivot("Elbow_L", leftShoulder, new Vector3(0f, -upperArmLength, 0f));
        rightElbow = CreatePivot("Elbow_R", rightShoulder, new Vector3(0f, -upperArmLength, 0f));

        CreateCapsule("Forearm_L", leftElbow, new Vector3(0f, -forearmLength * 0.5f, 0f),
            forearmLength, forearmRadius, skinMaterial);
        CreateCapsule("Forearm_R", rightElbow, new Vector3(0f, -forearmLength * 0.5f, 0f),
            forearmLength, forearmRadius, skinMaterial);

        CreateSphere("Hand_L", leftElbow, new Vector3(0f, -forearmLength, 0.01f),
            0.05f * height, skinMaterial);
        CreateSphere("Hand_R", rightElbow, new Vector3(0f, -forearmLength, 0.01f),
            0.05f * height, skinMaterial);

        // Голова и глаза — на существующем Head, чтобы смотреть вместе с камерой
        CreateSphere("HeadVisual", headAnchor, Vector3.zero, headRadius * 2f, skinMaterial);
        CreateSphere("Eye_L", headAnchor,
            new Vector3(-headRadius * 0.35f, headRadius * 0.12f, headRadius * 0.88f),
            headRadius * 0.26f, eyeMaterial);
        CreateSphere("Eye_R", headAnchor,
            new Vector3(headRadius * 0.35f, headRadius * 0.12f, headRadius * 0.88f),
            headRadius * 0.26f, eyeMaterial);

        built = true;
    }

    private void Update()
    {
        if (!built || controller == null) return;

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;
        Vector3 localVelocity = bodyAnchor.InverseTransformDirection(velocity);
        bool moving = controller.isGrounded && speed > 0.1f;

        float speed01 = Mathf.InverseLerp(0.1f, 8f, speed);
        Vector3 direction = localVelocity.sqrMagnitude > 0.0001f
            ? localVelocity.normalized
            : Vector3.forward;

        // Ось маха — перпендикуляр к направлению движения в плоскости пола
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

        // Присед: опускаем всё тело, колени уже согнуты добавкой
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

    private void OnDestroy()
    {
        if (skinMaterial != null) Destroy(skinMaterial);
        if (shirtMaterial != null) Destroy(shirtMaterial);
        if (pantsMaterial != null) Destroy(pantsMaterial);
        if (shoeMaterial != null) Destroy(shoeMaterial);
        if (eyeMaterial != null) Destroy(eyeMaterial);
    }

    private void PositionFeet(out float feetY)
    {
        feetY = controller.center.y - controller.height * 0.5f;
    }

    private Transform FindHeadByCamera()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        return cam != null ? cam.transform.parent : null;
    }

    private void CreateMaterials()
    {
        skinMaterial = CreateMaterial("Skin", new Color(0.86f, 0.68f, 0.55f));
        shirtMaterial = CreateMaterial("Shirt", new Color(0.20f, 0.30f, 0.46f));
        pantsMaterial = CreateMaterial("Pants", new Color(0.20f, 0.20f, 0.23f));
        shoeMaterial = CreateMaterial("Shoes", new Color(0.07f, 0.07f, 0.08f));
        eyeMaterial = CreateMaterial("Eyes", new Color(0.05f, 0.05f, 0.06f));
    }

    private Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader) { name = name, color = color };
        return material;
    }

    private static void HideRenderer(Transform target)
    {
        if (target == null) return;
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        return pivot.transform;
    }

    private void CreateCapsule(string name, Transform parent, Vector3 localPosition,
        float length, float radius, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        part.name = name;
        DestroyPartCollider(part);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        ApplyMaterial(part, material);
    }

    private void CreateBox(string name, Transform parent, Vector3 localPosition,
        Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        DestroyPartCollider(part);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = size;
        ApplyMaterial(part, material);
    }

    private void CreateSphere(string name, Transform parent, Vector3 localPosition,
        float diameter, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = name;
        DestroyPartCollider(part);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one * diameter;
        ApplyMaterial(part, material);
    }

    private static void DestroyPartCollider(GameObject part)
    {
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static void ApplyMaterial(GameObject part, Material material)
    {
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
    }
}
