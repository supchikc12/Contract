using System.Collections.Generic;
using UnityEngine;

// “ехнические ограничени€ сеток и карманов.
// Ёто не геймплейные лимиты, а защита от пересечений, выхода за границы и огромных сеток.
public static class ContainerGridValidator
{
    public const int MinPocketSize = 1;
    public const int MinPocketGapPixels = 0;
    public const int MaxPocketGapPixels = 32;
    public const int MaxRows = 50;
    public const int MaxColumns = 20;
    public const int MaxCells = 1000;

    public static List<string> Validate(ContainerGridData grid, bool requirePockets)
    {
        List<string> errors = new List<string>();
        if (grid == null)
        {
            errors.Add("ContainerGridData не задан.");
            return errors;
        }

        if (grid.pockets == null || grid.pockets.Count == 0)
        {
            if (requirePockets)
                errors.Add("ƒл€ разгрузки нужен хот€ бы один карман.");

            ValidateSize(grid.size, grid.maxColumns, errors);
            return errors;
        }

        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> pixelPositions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (ContainerGridData.Pocket pocket in grid.pockets)
        {
            if (pocket == null) continue;
            positions.Add(grid.GetPocketPosition(pocket));
            pixelPositions.Add(pocket.pixelPosition);
            sizes.Add(pocket.size);
        }

        if (grid.usePixelPositions)
        {
            HashSet<int> invalidPixels;
            errors.AddRange(ValidatePixelLayout(pixelPositions, sizes, out invalidPixels));
        }
        else if (grid.pocketGapPixels < MinPocketGapPixels || grid.pocketGapPixels > MaxPocketGapPixels)
        {
            errors.Add("«азор между карманами должен быть от " + MinPocketGapPixels + " до " + MaxPocketGapPixels + " px.");
        }

        Vector2Int bounds = CalculateBounds(positions, sizes);
        HashSet<int> invalid;
        errors.AddRange(ValidateLayout(bounds, positions, sizes, grid.maxColumns, out invalid));
        return errors;
    }

    public static List<string> ValidateLayout(
        Vector2Int gridSize,
        IReadOnlyList<Vector2Int> positions,
        IReadOnlyList<Vector2Int> sizes,
        int maxColumns,
        out HashSet<int> invalidPockets)
    {
        List<string> errors = new List<string>();
        invalidPockets = new HashSet<int>();

        if (gridSize.x <= 0 || gridSize.y <= 0)
        {
            errors.Add("–азмер сетки должен быть больше нул€.");
            return errors;
        }

        if (gridSize.x > MaxRows)
            errors.Add("ћаксимум строк сетки: " + MaxRows + ".");
        if (gridSize.y > MaxColumns)
            errors.Add("ћаксимум колонок сетки: " + MaxColumns + ".");
        if (gridSize.x * gridSize.y > MaxCells)
            errors.Add("ћаксимум €чеек сетки: " + MaxCells + ".");
        if (maxColumns < gridSize.y)
            errors.Add("maxColumns не может быть меньше фактического количества колонок.");

        if (positions == null || sizes == null || positions.Count != sizes.Count)
        {
            errors.Add("—писки позиций и размеров карманов не совпадают.");
            return errors;
        }

        for (int i = 0; i < sizes.Count; i++)
        {
            Vector2Int size = sizes[i];
            Vector2Int position = positions[i];

            if (size.x < MinPocketSize || size.y < MinPocketSize)
            {
                invalidPockets.Add(i);
                errors.Add(" арман " + (i + 1) + ": минимальный размер 1x1.");
            }

            if (position.x < 0 || position.y < 0)
            {
                invalidPockets.Add(i);
                errors.Add(" арман " + (i + 1) + ": позици€ не может быть отрицательной.");
            }

            if (position.x + size.x > gridSize.x || position.y + size.y > gridSize.y)
            {
                invalidPockets.Add(i);
                errors.Add(" арман " + (i + 1) + ": выходит за границы сетки.");
            }
        }

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                if (Overlaps(positions[i], sizes[i], positions[j], sizes[j]))
                {
                    invalidPockets.Add(i);
                    invalidPockets.Add(j);
                    errors.Add(" арманы " + (i + 1) + " и " + (j + 1) + " пересекаютс€.");
                    continue;
                }
            }
        }

        return errors;
    }

    // ѕроверка покамерных позиций в пиксел€х: координаты неотрицательные,
    // пр€моугольники карманов не пересекаютс€.
    public static List<string> ValidatePixelLayout(
        IReadOnlyList<Vector2Int> positions,
        IReadOnlyList<Vector2Int> sizes,
        out HashSet<int> invalidPockets)
    {
        List<string> errors = new List<string>();
        invalidPockets = new HashSet<int>();

        if (positions == null || sizes == null || positions.Count != sizes.Count)
        {
            errors.Add("—писки пиксельных позиций и размеров карманов не совпадают.");
            return errors;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i].x < 0 || positions[i].y < 0)
            {
                invalidPockets.Add(i);
                errors.Add(" арман " + (i + 1) + ": пиксельна€ позици€ не может быть отрицательной.");
            }
        }

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                if (!PixelRectsOverlap(positions[i], sizes[i], positions[j], sizes[j]))
                    continue;

                invalidPockets.Add(i);
                invalidPockets.Add(j);
                errors.Add(" арманы " + (i + 1) + " и " + (j + 1) + " пересекаютс€ в пиксел€х.");
            }
        }

        return errors;
    }

    private static bool PixelRectsOverlap(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
    {
        int aWidth = Mathf.Max(1, aSize.y) * 50;
        int aHeight = Mathf.Max(1, aSize.x) * 50;
        int bWidth = Mathf.Max(1, bSize.y) * 50;
        int bHeight = Mathf.Max(1, bSize.x) * 50;

        return aPos.x < bPos.x + bWidth && bPos.x < aPos.x + aWidth &&
               aPos.y < bPos.y + bHeight && bPos.y < aPos.y + aHeight;
    }

    // ѕиксельные начала карманов.  арман Ч единый блок: зазор добавл€етс€
    // между соседними карманами, но не разрезает €чейки внутри кармана.
    //  арманы с одинаковой начальной строкой/колонкой получают общий сдвиг,
    // чтобы соседние блоки не разъезжались.
    public static void CalculatePocketOrigins(
        IReadOnlyList<Vector2Int> positions,
        IReadOnlyList<Vector2Int> sizes,
        int gapPixels,
        out List<float> originX,
        out List<float> originY)
    {
        originX = new List<float>();
        originY = new List<float>();

        if (positions == null || sizes == null)
            return;

        int count = Mathf.Min(positions.Count, sizes.Count);
        int gap = Mathf.Max(0, gapPixels);

        Dictionary<int, float> rowOffsets = new Dictionary<int, float>();
        List<int> rows = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (!rows.Contains(positions[i].x))
                rows.Add(positions[i].x);
        }
        rows.Sort();

        for (int r = 0; r < rows.Count; r++)
        {
            int row = rows[r];
            float origin = row * 50f;

            for (int i = 0; i < count; i++)
            {
                if (positions[i].x != row) continue;

                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;
                    if (positions[j].x + sizes[j].x != row) continue;
                    if (!ColumnsOverlap(positions[i], sizes[i], positions[j], sizes[j])) continue;

                    origin = Mathf.Max(origin, rowOffsets[positions[j].x] + sizes[j].x * 50f + gap);
                }
            }

            rowOffsets[row] = origin;
        }

        Dictionary<int, float> columnOffsets = new Dictionary<int, float>();
        List<int> columns = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (!columns.Contains(positions[i].y))
                columns.Add(positions[i].y);
        }
        columns.Sort();

        for (int c = 0; c < columns.Count; c++)
        {
            int column = columns[c];
            float origin = column * 50f;

            for (int i = 0; i < count; i++)
            {
                if (positions[i].y != column) continue;

                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;
                    if (positions[j].y + sizes[j].y != column) continue;
                    if (!RowsOverlap(positions[i], sizes[i], positions[j], sizes[j])) continue;

                    origin = Mathf.Max(origin, columnOffsets[positions[j].y] + sizes[j].y * 50f + gap);
                }
            }

            columnOffsets[column] = origin;
        }

        for (int i = 0; i < count; i++)
        {
            originX.Add(columnOffsets[positions[i].y]);
            originY.Add(rowOffsets[positions[i].x]);
        }
    }

    private static bool RowsOverlap(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
    {
        return aPos.x < bPos.x + bSize.x && bPos.x < aPos.x + aSize.x;
    }

    private static bool ColumnsOverlap(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
    {
        return aPos.y < bPos.y + bSize.y && bPos.y < aPos.y + aSize.y;
    }

    public static Vector2Int CalculateBounds(IReadOnlyList<Vector2Int> positions, IReadOnlyList<Vector2Int> sizes)
    {
        if (positions == null || sizes == null || positions.Count == 0)
            return new Vector2Int(1, 1);

        int rows = 1;
        int columns = 1;
        for (int i = 0; i < positions.Count && i < sizes.Count; i++)
        {
            Vector2Int size = sizes[i];
            Vector2Int position = positions[i];
            rows = Mathf.Max(rows, position.x + Mathf.Max(MinPocketSize, size.x));
            columns = Mathf.Max(columns, position.y + Mathf.Max(MinPocketSize, size.y));
        }

        return new Vector2Int(rows, columns);
    }

    private static void ValidateSize(Vector2Int size, int maxColumns, List<string> errors)
    {
        if (size.x <= 0 || size.y <= 0)
            errors.Add("–азмер сетки должен быть больше нул€.");
        if (size.x > MaxRows)
            errors.Add("ћаксимум строк сетки: " + MaxRows + ".");
        if (size.y > MaxColumns)
            errors.Add("ћаксимум колонок сетки: " + MaxColumns + ".");
        if (size.x * size.y > MaxCells)
            errors.Add("ћаксимум €чеек сетки: " + MaxCells + ".");
        if (maxColumns < size.y)
            errors.Add("maxColumns не может быть меньше фактического количества колонок.");
    }

    private static bool Overlaps(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
    {
        return aPos.x < bPos.x + bSize.x && bPos.x < aPos.x + aSize.x &&
               aPos.y < bPos.y + bSize.y && bPos.y < aPos.y + aSize.y;
    }

}
