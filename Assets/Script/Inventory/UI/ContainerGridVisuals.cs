using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ќтрисовка карманов контейнера: доступность €чеек, фон панели и overlay.
// InitializeGrid отвечает за генерацию сетки, этот класс Ч только за визуал.
public static class ContainerGridVisuals
{
    public static void Apply(Transform gridRoot, List<GameObject> cells, Vector2Int gridSize, ContainerGridData layout)
    {
        if (gridRoot == null || layout == null) return;

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

            RectTransform pocketRect = pocketObj.GetComponent<RectTransform>();
            pocketRect.anchorMin = new Vector2(0f, 1f);
            pocketRect.anchorMax = new Vector2(0f, 1f);
            pocketRect.pivot = new Vector2(0f, 1f);
            pocketRect.anchoredPosition = new Vector2(pocket.position.y * 50f, -pocket.position.x * 50f);
            pocketRect.sizeDelta = new Vector2(pocket.size.y * 50f, pocket.size.x * 50f);

            Image pocketImage = pocketObj.GetComponent<Image>();
            pocketImage.color = new Color(0.62f, 0.63f, 0.60f, 0.55f);
            pocketImage.raycastTarget = false;
        }
    }
}
