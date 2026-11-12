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

        [Tooltip("Позиция в ячейках: X — строка, Y — столбец")]
        public Vector2Int position;

        [Tooltip("Размер в ячейках: X — количество строк, Y — количество столбцов")]
        public Vector2Int size = new Vector2Int(1, 1);

    }

    [Header("Сетка")]
    [Tooltip("Размер сплошной сетки: X — строки, Y — столбцы. Для карманов используется расчётный bounds.")]
    public Vector2Int size = new Vector2Int(1, 1);
    public bool limitColumns = true;
    public int maxColumns = 20;
    public ItemCategory allowedCategories = ItemCategory.All;

    [Header("Карманы")]
    public List<Pocket> pockets = new List<Pocket>();

    public bool HasPockets => pockets != null && pockets.Count > 0;

    public Vector2Int GetBounds()
    {
        if (!HasPockets)
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));

        int rows = 0;
        int columns = 0;

        foreach (Pocket pocket in pockets)
        {
            if (pocket == null) continue;
            rows = Mathf.Max(rows, pocket.position.x + Mathf.Max(1, pocket.size.x));
            columns = Mathf.Max(columns, pocket.position.y + Mathf.Max(1, pocket.size.y));
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

            int rows = Mathf.Max(1, pocket.size.x);
            int columns = Mathf.Max(1, pocket.size.y);

            if (cell.x >= pocket.position.x && cell.x < pocket.position.x + rows &&
                cell.y >= pocket.position.y && cell.y < pocket.position.y + columns)
            {
                index = i;
                return pocket;
            }
        }

        return null;
    }
}
