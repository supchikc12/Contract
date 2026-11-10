using UnityEngine;

// Отладочный бар стамины. Ничего не знает о движении — только рисует данные PlayerStamina.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerStaminaBar : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerStamina stamina;

    [Header("Тестовый бар")]
    [SerializeField] private bool showStaminaBar = true;
    [SerializeField] private bool hideWhenInventoryOpen = true;

    [Header("Вид (базовый размер для 1080p)")]
    [SerializeField] private float baseWidth = 280f;
    [SerializeField] private float baseHeight = 20f;
    [SerializeField] private float baseMargin = 16f;
    [SerializeField] private float baseFontSize = 14f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 2.5f;

    private GUIStyle labelStyle;
    private int labelFontSize;
    private InventoryManager inventory;

    private void Awake()
    {
        if (stamina == null) stamina = GetComponent<PlayerStamina>();
        inventory = GetComponentInChildren<InventoryManager>(true);
    }

    private void OnGUI()
    {
        if (!showStaminaBar || stamina == null) return;
        if (hideWhenInventoryOpen && inventory != null && inventory.gameObject.activeInHierarchy) return;
        if (Event.current.type != EventType.Repaint) return;

        // Масштаб от 1080p: читаемо и на 720p, и на 4K
        float scale = Mathf.Clamp(Screen.height / 1080f, minScale, maxScale);

        float width = baseWidth * scale;
        float height = baseHeight * scale;
        float margin = baseMargin * scale;
        float border = Mathf.Max(1f, 2f * scale);

        Rect background = new Rect(margin, Screen.height - margin - height, width, height);
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        float percent = stamina.StaminaPercent / 100f;
        Rect fill = new Rect(
            background.x + border,
            background.y + border,
            Mathf.Max(0f, (width - border * 2f) * percent),
            height - border * 2f);

        GUI.color = stamina.SprintBlocked
            ? new Color(0.5f, 0.5f, 0.5f, 0.9f)
            : Color.Lerp(Color.red, Color.green, percent);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = Color.white;

        int fontSize = Mathf.RoundToInt(baseFontSize * scale);
        if (labelStyle == null || labelFontSize != fontSize)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
            labelFontSize = fontSize;
        }

        string status = stamina.SprintBlocked ? "  [LOCK]" : string.Empty;
        GUI.Label(
            new Rect(
                background.x,
                background.y - height - margin * 0.25f,
                Mathf.Max(width, 320f * scale),
                height + margin * 0.25f),
            $"STAMINA {stamina.Stamina:0}/{stamina.MaxStamina:0} ({stamina.StaminaPercent:0}%){status}",
            labelStyle);
    }
}
