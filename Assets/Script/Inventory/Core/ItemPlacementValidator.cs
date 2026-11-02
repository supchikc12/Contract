using System.Collections.Generic;
using UnityEngine;

// ItemPlacementValidator отвечает за проверку возможности размещения предмета
// Отдельный класс для четкого разделения ответственности
public class ItemPlacementValidator
{
    // Проверка возможности размещения предмета в целевой ячейке
    public static bool CanPlaceItem(
        GameObject targetCell,
        InitializeGrid itemGrid,
        InventoryGrid inventoryContainer,
        ItemData itemData,
        out PlacementData placementData)
    {
        placementData = new PlacementData();

        // Валидация входных данных
        if (!ValidateInputs(targetCell, itemGrid, inventoryContainer, itemData))
        {
            return false;
        }

        InitializeGrid inventoryInit = targetCell.GetComponentInParent<InitializeGrid>();
        InventoryCell targetGridCell = targetCell.GetComponent<InventoryCell>();

        if (inventoryInit == null || targetGridCell == null)
        {
            return false;
        }

        // Проверка: не пытаемся ли мы положить предмет в сетку другого предмета
        if (inventoryInit != null && !inventoryInit.IsContainerGrid)
        {
            // Если у этой сетки есть родитель ItemInstance, значит это сетка предмета (например, автомата)
            ItemInstance otherItem = inventoryInit.GetComponentInParent<ItemInstance>();
            if (otherItem != null)
            {
                return false; // Запрещаем класть предметы друг на друга
            }
        }

        // Проверка категории предмета
        if (!inventoryContainer.CanAcceptItem(itemData.category))
        {
            return false;
        }

        if (IsContainer(itemData.category) && IsInsideContainer(targetCell))
        {
            return false;
        }

        // Получаем ячейки предмета
        List<InventoryCell> itemCells = GetItemCells(itemGrid);
        if (itemCells == null || itemCells.Count == 0)
        {
            return false;
        }

        // Вычисляем позиции в инвентаре
        InventoryCell middleCell = itemCells[itemCells.Count / 2];
        Vector2Int offset = targetGridCell.gridSize - middleCell.gridSize;
        List<Vector2Int> desiredPositions = ComputeDesiredPositions(itemCells, offset);

        // Проверяем границы
        Vector2Int inventoryDimensions = inventoryInit.GetDimensions();
        if (!ArePositionsWithinBounds(desiredPositions, inventoryDimensions))
        {
            return false;
        }

        // Получаем целевые ячейки
        List<InventoryCell> targetGrids = GetInventoryGridsForPositions(desiredPositions, inventoryInit);
        if (targetGrids == null || targetGrids.Count != desiredPositions.Count)
        {
            return false;
        }

        // Проверяем свободность ячеек
        if (!AreCellsFree(targetGrids))
        {
            return false;
        }

        // Заполняем данные размещения
        placementData.targetCell = targetCell;
        placementData.targetGrids = targetGrids;
        placementData.itemCells = itemCells;
        placementData.offset = offset;
        placementData.inventoryInit = inventoryInit;

        return true;
    }

    // Попытка найти альтернативную позицию рядом с целевой ячейкой
    public static bool TryFindNearbyPosition(
        GameObject initialTarget,
        InitializeGrid itemGrid,
        InventoryGrid inventoryContainer,
        ItemData itemData,
        out PlacementData placementData)
    {
        placementData = new PlacementData();
        InitializeGrid inventoryInit = initialTarget.GetComponentInParent<InitializeGrid>();
        InventoryCell initialCell = initialTarget.GetComponent<InventoryCell>();

        if (inventoryInit == null || initialCell == null)
        {
            return false;
        }

        // Пробуем соседние ячейки
        Vector2Int[] offsets = new Vector2Int[]
        {
            new Vector2Int(0, 0),   // Текущая
            new Vector2Int(1, 0),   // Справа
            new Vector2Int(-1, 0),  // Слева
            new Vector2Int(0, 1),   // Сверху
            new Vector2Int(0, -1)   // Снизу
        };

        foreach (var offset in offsets)
        {
            Vector2Int testPos = initialCell.gridSize + offset;
            GameObject testCell = inventoryInit.GetCellAt(testPos);

            if (testCell != null && CanPlaceItem(testCell, itemGrid, inventoryContainer, itemData, out placementData))
            {
                return true;
            }
        }

        return false;
    }

    // Занять ячейки для предмета
    public static void OccupyCells(List<InventoryCell> cells, GameObject item)
    {
        foreach (var cell in cells)
        {
            cell.fullness = item;
        }
    }

    // Освободить ячейки
    public static void FreeCells(List<InventoryCell> cells)
    {
        foreach (var cell in cells)
        {
            cell.fullness = null;
        }
    }

    // --- Приватные вспомогательные методы ---

    private static bool IsContainer(ItemCategory category)
    {
        return (category & (ItemCategory.Container | ItemCategory.Backpack)) != 0;
    }

    private static bool IsInsideContainer(GameObject targetCell)
    {
        Transform current = targetCell.transform.parent;

        while (current != null)
        {
            // Если нашли InventoryGrid с меткой контейнера
            InventoryGrid grid = current.GetComponent<InventoryGrid>();
            if (grid != null && grid.isContainerGrid)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool ValidateInputs(GameObject targetCell, InitializeGrid itemGrid, 
        InventoryGrid inventoryContainer, ItemData itemData)
    {
        return targetCell != null && 
               itemGrid != null && 
               inventoryContainer != null && 
               itemData != null;
    }

    private static List<InventoryCell> GetItemCells(InitializeGrid itemInit)
    {
        List<InventoryCell> itemCells = new List<InventoryCell>();
        foreach (var go in itemInit.listGrid)
        {
            InventoryCell g = go.GetComponent<InventoryCell>();
            if (g != null) itemCells.Add(g);
        }
        return itemCells;
    }

    private static List<Vector2Int> ComputeDesiredPositions(List<InventoryCell> itemCells, Vector2Int offset)
    {
        List<Vector2Int> positions = new List<Vector2Int>(itemCells.Count);
        foreach (InventoryCell g in itemCells)
        {
            positions.Add(g.gridSize + offset);
        }
        return positions;
    }

    private static bool ArePositionsWithinBounds(List<Vector2Int> positions, Vector2Int dims)
    {
        foreach (var pos in positions)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= dims.x || pos.y >= dims.y)
                return false;
        }
        return true;
    }

    private static List<InventoryCell> GetInventoryGridsForPositions(List<Vector2Int> positions, InitializeGrid inventoryInit)
    {
        List<InventoryCell> grids = new List<InventoryCell>(positions.Count);
        foreach (var pos in positions)
        {
            InventoryCell g = inventoryInit.GetGridAt(pos);
            if (g == null) return null;
            grids.Add(g);
        }
        return grids;
    }

    private static bool AreCellsFree(List<InventoryCell> grids)
    {
        foreach (var g in grids)
        {
            if (g.fullness != null) return false;
        }
        return true;
    }
}

// Структура для хранения данных о размещении предмета
public class PlacementData
{
    public GameObject targetCell;
    public List<InventoryCell> targetGrids;
    public List<InventoryCell> itemCells;
    public Vector2Int offset;
    public InitializeGrid inventoryInit;
}
