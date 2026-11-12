using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ќтрисовка карманов контейнера: доступность €чеек, фон панели и overlay.
// InitializeGrid отвечает за генерацию сетки, этот класс Ч только за визуал.
public static class ContainerGridVisuals
{
    public static void Apply(Transform gridRoot, List<GameObject> cells, Vector2Int gridSize, ContainerGridData layout)
    {
        if (gridRoot == null || layout == null) return;

        // Ќебольшую сетку разгрузки центрируем в окне контейнера.
        RectTransform gridRect = gridRoot as RectTransform;
        RectTransform viewport = gridRoot.parent as RectTransform;
        if (gridRect != null && viewport != null &&
            gridRect.sizeDelta.x <= viewport.rect.width &&
            gridRect.sizeDelta.y <= viewport.rect.height)
        {
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;
        }

        foreach (GameObject cellObj in cells)
        {
            if (cellObj == null) continue;

            InventoryCell cell = cellObj.GetComponent<InventoryCell>();
            if (cell == null) continue;

            int pocketIndex;
            ContainerGridData.Pocket pocket = layout.GetPocketAt(cell.gridSize, out pocketIndex);
            bool available = pocket != null;

            cell.isAvailable = available;
            cell.pocketId = available ? pocketIndex : -1;
            cell.allowedCategories = available ? pocket.allowedTypes : ItemCategory.None;

            Image image = cellObj.GetComponent<Image>();
            if (image != null)
            {
                if (available)
                {
                    // ”бираем спрайт €чейки, чтобы не перекрывать фон кармана.
                    // ѕрозрачность оставл€ем, чтобы €чейка ловила raycast при перетаскивании.
                    image.sprite = null;
                    image.color = new Color(1f, 1f, 1f, 0.05f);
                    image.enabled = true;
                }
                else
                {
                    image.enabled = false;
                }
            }
        }

        // Ќепрозрачный фон панели, чтобы за карманами не просвечивал инвентарь
        Transform scrollView = gridRoot.parent != null ? gridRoot.parent.parent : null;
        Transform containerPanel = scrollView != null ? scrollView.parent : null;
        if (containerPanel != null)
        {
            Image panelImage = containerPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = true;
                panelImage.color = new Color(0.55f, 0.56f, 0.54f, 0.85f);
            }
        }

        Transform oldOverlay = gridRoot.Find("VestPocketOverlay");
        if (oldOverlay != null) Object.Destroy(oldOverlay.gameObject);

        GameObject overlay = new GameObject("VestPocketOverlay", typeof(RectTransform), typeof(LayoutElement));
        overlay.transform.SetParent(gridRoot, false);
        overlay.transform.SetAsFirstSibling();

        LayoutElement overlayLayout = overlay.GetComponent<LayoutElement>();
        overlayLayout.ignoreLayout = true;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 1f);
        overlayRect.anchorMax = new Vector2(0f, 1f);
        overlayRect.pivot = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = new Vector2(gridSize.y * 50f, gridSize.x * 50f);

        for (int i = 0; i < layout.pockets.Count; i++)
        {
            ContainerGridData.Pocket pocket = layout.pockets[i];
            if (pocket == null) continue;

            GameObject pocketObj = new GameObject("Pocket_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pocketObj.transform.SetParent(overlay.transform, false);

            const float inset = 2f;
            RectTransform pocketRect = pocketObj.GetComponent<RectTransform>();
            pocketRect.anchorMin = new Vector2(0f, 1f);
            pocketRect.anchorMax = new Vector2(0f, 1f);
            pocketRect.pivot = new Vector2(0f, 1f);
            pocketRect.anchoredPosition = new Vector2(
                pocket.position.y * 50f + inset,
                -(pocket.position.x * 50f + inset));
            pocketRect.sizeDelta = new Vector2(
                Mathf.Max(10f, pocket.size.y * 50f - inset * 2f),
                Mathf.Max(10f, pocket.size.x * 50f - inset * 2f));

            Image pocketImage = pocketObj.GetComponent<Image>();
            float shade = 0.50f + (i % 3) * 0.02f;
            pocketImage.color = new Color(shade, shade + 0.01f, shade - 0.015f, 0.82f);
            pocketImage.raycastTarget = false;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(pocketObj.transform, false);

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(5f, -4f);
            labelRect.sizeDelta = new Vector2(-10f, 18f);

            TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = pocket.name;
            label.fontSize = Mathf.Clamp(Mathf.FloorToInt(pocketRect.sizeDelta.x / 6f), 9, 14);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.12f, 0.13f, 0.12f, 0.92f);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        }
    }
}
