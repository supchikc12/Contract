using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ѕодбор предметов по клавише: смотрит в центр камеры, находит ItemState на слое 8
// и передаЄт предмет в InventoryManager.TryPickupWorldItem.
public class PlayerPickupController : MonoBehaviour
{
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private LayerMask pickupMask;

    [Header("—сылки")]
    [SerializeField] private PlayerInventoryToggle inventoryToggle;
    [SerializeField] private Camera playerCamera;
    private GameObject promptRoot;
    private TMP_Text promptText;
    private float feedbackUntil;
    private string feedbackMessage;

    private void Awake()
    {
        if (pickupMask.value == 0)
            pickupMask = 1 << 8;

        if (inventoryToggle == null) inventoryToggle = GetComponent<PlayerInventoryToggle>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
        if (playerCamera == null) playerCamera = Camera.main;

        BuildPrompt();
    }

    private void Start()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        bool canInteract = inventoryToggle == null || !inventoryToggle.IsOpen;
        ItemState target = canInteract ? FindTarget() : null;

        if (canInteract && target != null && Input.GetKeyDown(pickupKey))
        {
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.TryPickupWorldItem(target.gameObject))
                {
                    feedbackUntil = Time.time + 1.2f;
                    feedbackMessage = "Ќет свободного слота или контейнера";
                }
            }
        }

        UpdatePrompt(target);
    }

    private ItemState FindTarget()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask))
            return null;

        return hit.collider.GetComponentInParent<ItemState>();
    }

    private void UpdatePrompt(ItemState target)
    {
        if (promptRoot == null || promptText == null) return;

        if (Time.time < feedbackUntil)
        {
            promptText.text = feedbackMessage;
            promptRoot.SetActive(true);
            return;
        }

        promptText.text = "E Ч подобрать";
        promptRoot.SetActive(target != null);
    }

    private void BuildPrompt()
    {
        GameObject canvasObj = new GameObject("PickupPromptCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        promptRoot = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        promptRoot.transform.SetParent(canvasObj.transform, false);

        RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.sizeDelta = new Vector2(520f, 64f);
        promptRect.anchoredPosition = new Vector2(0f, 90f);

        Image background = promptRoot.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.86f);
        background.raycastTarget = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(promptRoot.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textObj.GetComponent<TextMeshProUGUI>();
        promptText.font = GetFont();
        promptText.fontSize = 24f;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.text = "E Ч подобрать";
        promptText.raycastTarget = false;

        promptRoot.SetActive(false);
    }

    private static TMP_FontAsset GetFont()
    {
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}
