using UnityEngine;

// Заглушка осмотра тела: показывает хп всех зон и часть под прицелом.
// Полноценный UI медосмотра появится позже.
public class BodyPartInspectionUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Управление")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool showAtStart = false;
    [SerializeField] private bool showHint = true;

    [Header("Осмотр через прицел")]
    [SerializeField] private bool inspectUnderCrosshair = true;
    [SerializeField] private float inspectDistance = 100f;

    [Header("Вид")]
    [SerializeField] private float panelWidth = 380f;

    private bool visible;
    private Vector2 scroll;
    private BodyPartHealth focusedPart;
    private GUIStyle titleStyle;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        visible = showAtStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
            if (visible) scroll = Vector2.zero;
        }

        if (visible && Input.GetKeyDown(KeyCode.Escape))
            visible = false;

        if (visible && inspectUnderCrosshair)
            UpdateFocusedPart();
    }

    private void UpdateFocusedPart()
    {
        focusedPart = null;

        Camera camera = Camera.main;
        if (camera == null) return;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, inspectDistance, ~0, QueryTriggerInteraction.Collide))
            focusedPart = hit.collider.GetComponentInParent<BodyPartHealth>();
    }

    private void OnGUI()
    {
        if (!visible)
        {
            if (showHint)
                DrawHint();
            return;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelHeight = Mathf.Min(Screen.height - 80f, 760f);
        Rect panelRect = new Rect(40f, 40f, panelWidth, panelHeight);

        GUILayout.BeginArea(panelRect, GUI.skin.box);
        GUILayout.BeginVertical();

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;
        }

        GUILayout.Label("Осмотр тела (заглушка)", titleStyle);
        GUILayout.Label("F1 или Esc — закрыть");

        if (inspectUnderCrosshair)
        {
            if (focusedPart != null)
            {
                GUILayout.Label("В фокусе: " + GetDisplayName(focusedPart.PartType)
                    + "  " + focusedPart.CurrentHealth.ToString("0")
                    + " / " + focusedPart.MaxHealth.ToString("0"));
            }
            else
            {
                GUILayout.Label("В фокусе: —");
            }
        }

        GUILayout.Space(6f);

        if (playerHealth == null)
        {
            GUILayout.Label("PlayerHealth не найден.");
        }
        else
        {
            float listHeight = Mathf.Max(100f, panelHeight - 150f);
            scroll = GUILayout.BeginScrollView(scroll, false, true, GUILayout.Height(listHeight));

            for (int i = 0; i < playerHealth.BodyParts.Count; i++)
                DrawPartRow(playerHealth.BodyParts[i]);

            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label("Всего: " + playerHealth.TotalHealth.ToString("0")
                + " / " + playerHealth.TotalMaxHealth.ToString("0"));
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawHint()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(12f, Screen.height - 34f, 190f, 22f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(18f, Screen.height - 32f, 180f, 20f), "F1 — осмотр тела");
    }

    private void DrawPartRow(PlayerHealth.BodyPartState state)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label(GetDisplayName(state.part), GUILayout.Width(170f));
        GUILayout.Label(state.currentHealth.ToString("0") + " / " + state.maxHealth.ToString("0"),
            GUILayout.Width(90f));
        GUILayout.EndHorizontal();

        float percent = state.maxHealth > 0f
            ? Mathf.Clamp01(state.currentHealth / state.maxHealth)
            : 0f;

        Rect bar = GUILayoutUtility.GetRect(1f, 7f, GUILayout.ExpandWidth(true));
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);

        if (percent > 0f)
        {
            GUI.color = Color.Lerp(Color.red, Color.green, percent);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * percent, bar.height), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        GUILayout.Space(4f);
        GUILayout.EndVertical();
    }

    private static string GetDisplayName(BodyPartType part)
    {
        switch (part)
        {
            case BodyPartType.Head: return "Голова";
            case BodyPartType.Neck: return "Шея";
            case BodyPartType.Torso: return "Торс";
            case BodyPartType.Pelvis: return "Таз";
            case BodyPartType.LeftThigh: return "Левое бедро";
            case BodyPartType.LeftShin: return "Левая голень";
            case BodyPartType.LeftFoot: return "Левая стопа";
            case BodyPartType.RightThigh: return "Правое бедро";
            case BodyPartType.RightShin: return "Правая голень";
            case BodyPartType.RightFoot: return "Правая стопа";
            case BodyPartType.LeftUpperArm: return "Левое плечо";
            case BodyPartType.LeftForearm: return "Левое предплечье";
            case BodyPartType.LeftHand: return "Левая кисть";
            case BodyPartType.RightUpperArm: return "Правое плечо";
            case BodyPartType.RightForearm: return "Правое предплечье";
            case BodyPartType.RightHand: return "Правая кисть";
            default: return part.ToString();
        }
    }
}
