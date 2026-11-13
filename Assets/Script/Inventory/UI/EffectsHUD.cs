using System.Collections.Generic;
using UnityEngine;

// HUD инвентаря: известные эффекты, лечение и прогресс использования расходника.
public class EffectsHUD : MonoBehaviour
{
    [SerializeField] private CharacterEffectAggregator effectAggregator;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ConsumableUseController useController;

    private ItemInstance treatmentItem;
    private ConsumableData treatmentData;
    private string treatmentStatus;
    private Vector2 scroll;
    private bool expanded;

    public bool BeginTreatment(ItemInstance item)
    {
        if (item == null) return false;

        ItemState state = item.GetItemState();
        ConsumableData data = state != null ? state.data as ConsumableData : null;
        if (data == null || data.target != ConsumableTarget.BodyPart)
            return false;

        treatmentItem = item;
        treatmentData = data;
        treatmentStatus = null;
        scroll = Vector2.zero;
        expanded = true;
        return true;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            expanded = !expanded;

        if (useController != null && useController.IsUsing)
            expanded = true;
    }

    private void OnGUI()
    {
        ResolveReferences();
        if (effectAggregator == null || playerHealth == null) return;

        if (!expanded)
        {
            Rect buttonRect = new Rect(20f, Screen.height - 56f, 260f, 30f);
            if (GUI.Button(buttonRect, "СОСТОЯНИЕ (H)   эффектов: " + effectAggregator.GetVisibleEffects().Count))
                expanded = true;

            return;
        }

        float width = 360f;
        float height = Mathf.Min(Screen.height - 80f, 760f);
        Rect panel = new Rect(20f, 60f, width, height);

        GUI.color = new Color(0.04f, 0.04f, 0.04f, 0.82f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, panel.height - 20f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("СОСТОЯНИЕ", TitleStyle());
        if (GUILayout.Button("Свернуть (H)", GUILayout.Width(110f)))
        {
            expanded = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        if (useController != null && useController.IsUsing)
        {
            GUILayout.Label("Использование: " + useController.CurrentItemName);
            Rect bar = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.75f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * useController.Progress, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (treatmentItem != null)
        {
            DrawTreatment();
        }
        else
        {
            DrawEffects();
        }

        GUILayout.EndArea();
    }

    private void DrawTreatment()
    {
        GUILayout.Space(6f);
        GUILayout.Label("Лечение: " + treatmentData.itemName);
        GUILayout.Label("Выбери часть тела.");

        if (!string.IsNullOrEmpty(treatmentStatus))
            GUILayout.Label(treatmentStatus);

        if (GUILayout.Button("Отмена"))
        {
            ClearTreatment();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll, false, true);
        for (int i = 0; i < playerHealth.BodyParts.Count; i++)
        {
            PlayerHealth.BodyPartState state = playerHealth.BodyParts[i];
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetPartName(state.part), GUILayout.Width(135f));

            string effects = GetPartEffectText(state.part);
            GUILayout.Label(effects, GUILayout.Width(100f));

            if (GUILayout.Button("Применить", GUILayout.Width(90f)))
                ApplyTreatment(state.part);

            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private void DrawEffects()
    {
        List<ActiveEffect> effects = effectAggregator.GetVisibleEffects();

        if (effects.Count == 0)
        {
            GUILayout.Label("Нет известных эффектов.");
            GUILayout.Label("Осмотри тело или предмет.");
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll, false, true);
        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect effect = effects[i];
            if (effect == null || effect.Definition == null) continue;

            string time = effect.Permanent
                ? "постоянно"
                : effect.RemainingTime.ToString("0") + " с";
            GUILayout.Label(GetTargetName(effect.TargetMask)
                + ": " + effect.Definition.effectName
                + (effect.Stacks > 1 ? " x" + effect.Stacks : "")
                + "  (" + time + ")");
        }
        GUILayout.EndScrollView();
    }

    private void ApplyTreatment(BodyPartType part)
    {
        if (treatmentItem == null) return;

        bool started;
        if (useController != null)
            started = useController.BeginUse(treatmentItem, part);
        else
            started = ConsumableUseService.TryUse(treatmentItem, part);

        if (started)
        {
            ClearTreatment();
        }
        else
        {
            treatmentStatus = "Нечего лечить или применить.";
        }
    }

    private string GetPartEffectText(BodyPartType part)
    {
        List<ActiveEffect> effects = effectAggregator.GetVisibleEffects(part);
        if (effects.Count == 0) return "-";

        string text = "";
        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect effect = effects[i];
            if (effect == null || effect.Definition == null) continue;
            if (text.Length > 0) text += ", ";
            text += effect.Definition.effectName;
        }

        return text;
    }

    private void ClearTreatment()
    {
        treatmentItem = null;
        treatmentData = null;
        treatmentStatus = null;
    }

    private void ResolveReferences()
    {
        if (effectAggregator == null)
            effectAggregator = GetComponentInParent<CharacterEffectAggregator>();
        if (effectAggregator == null)
            effectAggregator = FindAnyObjectByType<CharacterEffectAggregator>();

        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (useController == null)
            useController = GetComponentInParent<ConsumableUseController>();
        if (useController == null)
            useController = FindAnyObjectByType<ConsumableUseController>();
    }

    private static GUIStyle titleStyle;

    private static GUIStyle TitleStyle()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
        }

        return titleStyle;
    }

    private static string GetTargetName(EffectTargetMask mask)
    {
        if (mask == EffectTargetMask.Character) return "Персонаж";
        if (mask == EffectTargetMask.Weapon) return "Оружие";
        if (mask == EffectTargetMask.Item) return "Предмет";
        if (mask == EffectTargetMask.Container) return "Контейнер";
        if (mask == (EffectTargetMask.Item | EffectTargetMask.Container)) return "Предмет/контейнер";

        foreach (BodyPartType part in System.Enum.GetValues(typeof(BodyPartType)))
        {
            if (mask == EffectTargets.ForBodyPart(part))
                return GetPartName(part);
        }

        return mask.ToString();
    }

    private static string GetPartName(BodyPartType part)
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
