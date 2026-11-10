using System.Collections.Generic;
using UnityEngine;

// Описание гибкой сетки разгрузки: набор карманов разного размера.
// Позиция и размер задаются в ячейках. X — строка (сверху вниз),
// Y — столбец (слева направо), как в ItemData.size.
[CreateAssetMenu(fileName = "New Vest Layout", menuName = "Inventory/Vest Layout")]
public class VestLayoutData : ScriptableObject
{
    [System.Serializable]
    public class Pocket
    {
        public string name = "Карман";

        [Tooltip("Позиция в ячейках: X — строка, Y — столбец")]
        public Vector2Int position;

        [Tooltip("Размер в ячейках: X — количество строк, Y — количество столбцов")]
        public Vector2Int size = new Vector2Int(1, 1);

        [Tooltip("Какие категории предметов можно класть в карман")]
        public ItemCategory allowedTypes = ItemCategory.All;
    }

    public List<Pocket> pockets = new List<Pocket>();

    public Vector2Int GetBounds()
    {
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
