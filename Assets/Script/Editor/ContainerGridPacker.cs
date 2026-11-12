using System.Collections.Generic;
using UnityEngine;

// Автоматическая упаковка карманов в логическую сетку.
// Карманы ставятся вплотную: визуальный зазор задаётся в ContainerGridData.pocketGapPixels.
public static class ContainerGridPacker
{
    public const int Gap = 0;

    public class LayoutResult
    {
        public Vector2Int GridSize;
        public List<Vector2Int> Positions = new List<Vector2Int>();
        public int EmptyCells;
    }

    public static LayoutResult Pack(IReadOnlyList<Vector2Int> sizes)
    {
        LayoutResult result = new LayoutResult();
        result.GridSize = new Vector2Int(1, 1);

        if (sizes == null || sizes.Count == 0)
            return result;

        int maxWidth = 1;
        int totalArea = 0;
        for (int i = 0; i < sizes.Count; i++)
        {
            Vector2Int size = ClampSize(sizes[i]);
            maxWidth = Mathf.Max(maxWidth, size.y);
            totalArea += size.x * size.y;
        }

        LayoutResult best = null;
        int bestScore = int.MaxValue;

        for (int width = maxWidth; width <= maxWidth + 10; width++)
        {
            LayoutResult candidate = PackAtWidth(sizes, width, totalArea);
            int score = Mathf.Abs(candidate.GridSize.y - candidate.GridSize.x) * 2 + candidate.EmptyCells;
            if (best == null || score < bestScore ||
                (score == bestScore && candidate.GridSize.x < best.GridSize.x))
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best ?? result;
    }

    private static LayoutResult PackAtWidth(IReadOnlyList<Vector2Int> sizes, int width, int totalArea)
    {
        LayoutResult result = new LayoutResult();

        int x = 0;
        int y = 0;
        int rowHeight = 0;
        int usedWidth = 0;

        for (int i = 0; i < sizes.Count; i++)
        {
            Vector2Int size = ClampSize(sizes[i]);

            if (x > 0 && x + size.y > width)
            {
                y += rowHeight + Gap;
                x = 0;
                rowHeight = 0;
            }

            result.Positions.Add(new Vector2Int(y, x));
            x += size.y + Gap;
            rowHeight = Mathf.Max(rowHeight, size.x);
            usedWidth = Mathf.Max(usedWidth, x - Gap);
        }

        int height = y + rowHeight;
        result.GridSize = new Vector2Int(Mathf.Max(1, height), Mathf.Max(1, usedWidth));
        result.EmptyCells = Mathf.Max(0, usedWidth * height - totalArea);
        return result;
    }

    private static Vector2Int ClampSize(Vector2Int size)
    {
        return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
    }
}
