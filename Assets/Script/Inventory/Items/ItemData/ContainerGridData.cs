using System.Collections.Generic;
using UnityEngine;

// Конфигурация сетки контейнера: размер, колонки и карманы.
// Не является ItemData: один и тот же конфиг может использоваться разными предметами.
[CreateAssetMenu(fileName = "New Container Grid", menuName = "Inventory/Container Grid")]
public class ContainerGridData : ScriptableObject
{
    [System.Serializable]
    public class Pocket
    {
        public string name = "Карман";

        [HideInInspector]
        [Tooltip("Логическая позиция в ячейках. Рассчитывается из pixelPosition.")]
        public Vector2Int position;

        [Tooltip("Позиция левого верхнего угла в пикселях от левого верхнего угла сетки")]
        public Vector2Int pixelPosition;

        [Tooltip("Размер в ячейках: X — количество строк, Y — количество столбцов")]
        public Vector2Int size = new Vector2Int(1, 1);

    }

    [Header("Сетка")]
    [Tooltip("Размер сплошной сетки: X — строки, Y — столбцы. Для карманов используется расчётный bounds.")]
    public Vector2Int size = new Vector2Int(1, 1);
    public bool limitColumns = true;
    public int maxColumns = 20;
    public ItemCategory allowedCategories = ItemCategory.All;

    [Tooltip("Покамерные позиции в пикселях. Если выключено — старая раскладка по position + pocketGapPixels.")]
    public bool usePixelPositions = false;

    [HideInInspector]
    public int pocketGapPixels = 1;

    [Header("Карманы")]
    public List<Pocket> pockets = new List<Pocket>();

    public bool HasPockets => pockets != null && pockets.Count > 0;

    // Логическая ячейка, в которой начинается карман при пиксельной раскладке.
    public static Vector2Int PixelToCell(Vector2Int pixelPosition)
    {
        return new Vector2Int(
            Mathf.Max(0, pixelPosition.y) / 50,
            Mathf.Max(0, pixelPosition.x) / 50);
    }

    public Vector2Int GetPocketPosition(Pocket pocket)
    {
        if (pocket == null) return Vector2Int.zero;
        return usePixelPositions ? PixelToCell(pocket.pixelPosition) : pocket.position;
    }

    public Vector2Int GetBounds()
    {
        if (!HasPockets)
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));

        int rows = 0;
        int columns = 0;

        foreach (Pocket pocket in pockets)
        {
            if (pocket == null) continue;

            Vector2Int position = GetPocketPosition(pocket);
            rows = Mathf.Max(rows, position.x + Mathf.Max(1, pocket.size.x));
            columns = Mathf.Max(columns, position.y + Mathf.Max(1, pocket.size.y));
        }

        return new Vector2Int(Mathf.Max(1, rows), Mathf.Max(1, columns));
    }

    public Pocket GetPocketAt(Vector2Int cell, out int index)
    {
        index = -1;

        if (pockets == null) return null;

        for (int i = 0; i < pockets.Count; i++)
        {
            Pocket pocket = pockets[i];
            if (pocket == null) continue;

            Vector2Int position = GetPocketPosition(pocket);
            int rows = Mathf.Max(1, pocket.size.x);
            int columns = Mathf.Max(1, pocket.size.y);

            if (cell.x >= position.x && cell.x < position.x + rows &&
                cell.y >= position.y && cell.y < position.y + columns)
            {
                index = i;
                return pocket;
            }
        }

        return null;
    }
}
