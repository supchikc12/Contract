using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// –азметка карманов контейнера: какие €чейки доступны и к какому карману относ€тс€.
// ¬изуально карманы не рисуютс€: между ними остаютс€ пиксельные промежутки из ContainerGridData.
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
            cell.allowedCategories = available ? ItemCategory.All : ItemCategory.None;

            Image image = cellObj.GetComponent<Image>();
            if (image != null) image.enabled = available;
        }
    }
}
