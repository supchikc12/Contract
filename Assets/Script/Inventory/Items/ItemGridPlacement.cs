using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Логика размещения предмета в сетке: занятые ячейки, поворот и позиционирование.
// Не управляет drag-состоянием и визуальными эффектами — это ItemInstance/ItemView.
public class ItemGridPlacement
{
    private readonly ItemInstance owner;
    private readonly ItemState state;
    private readonly ItemView view;
    private readonly Transform transform;

    private InitializeGrid grid;

    private readonly List<InventoryCell> occupiedCells = new List<InventoryCell>();
    private readonly List<InventoryCell> savedOccupiedCells = new List<InventoryCell>();

    private bool isRotated;
    private bool startRotated;

    public ItemGridPlacement(ItemInstance owner, ItemState state, ItemView view)
    {
        this.owner = owner;
        this.state = state;
        this.view = view;
        transform = owner.transform;
    }

    public bool IsRotated => isRotated;

    public void BindGrid(InitializeGrid value)
    {
        grid = value;
    }

    // Запоминает состояние перед drag: поворот и занятые ячейки для возврата.
    public void CaptureStartState()
    {
        startRotated = isRotated;
        savedOccupiedCells.Clear();
        if (occupiedCells.Count > 0) savedOccupiedCells.AddRange(occupiedCells);
    }

    public void ReleaseCells()
    {
        if (occupiedCells.Count == 0) return;

        ItemPlacementValidator.FreeCells(occupiedCells);
        occupiedCells.Clear();
    }

    // Возвращает занятие ячеек, которые предмет занимал до перетаскивания.
    public void RestoreSavedCells()
    {
        if (savedOccupiedCells.Count == 0) return;

        occupiedCells.Clear();
        occupiedCells.AddRange(savedOccupiedCells);
        ItemPlacementValidator.OccupyCells(occupiedCells, owner.gameObject);
        savedOccupiedCells.Clear();
    }

    public bool CanPlaceInGrid(GameObject targetCell, InventoryGrid filter)
    {
        if (state == null || state.data == null || grid == null) return false;

        PlacementData placementData;
        if (ItemPlacementValidator.CanPlaceItem(targetCell, grid, filter, state.data, out placementData))
            return true;
        if (ItemPlacementValidator.TryFindNearbyPosition(targetCell, grid, filter, state.data, out placementData))
            return true;

        return false;
    }

    public void PlaceInGrid(GameObject targetCell)
    {
        if (targetCell == null || state == null || state.data == null || grid == null) return;

        InventoryGrid filter = InventoryGrid.FindInParents(targetCell.transform);
        if (filter == null) return;

        PlacementData placementData;
        if (ItemPlacementValidator.CanPlaceItem(targetCell, grid, filter, state.data, out placementData) ||
            ItemPlacementValidator.TryFindNearbyPosition(targetCell, grid, filter, state.data, out placementData))
        {
            ExecutePlacement(placementData);
        }
    }

    public void RequestRotate()
    {
        isRotated = !isRotated;

        float targetAngle = isRotated ? -90f : 0f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (view != null) view.SmoothRotateTo(targetRotation);
        else transform.localRotation = targetRotation;

        RotateGridCells();
    }

    // Возвращает поворот к состоянию начала drag.
    public void RestoreStartRotation()
    {
        if (isRotated != startRotated) RequestRotate();
    }

    // Компенсирует смещение чётных предметов при возврате на исходную позицию.
    public void ApplyReturnOffset()
    {
        if (grid == null || grid.listGrid.Count <= 1) return;

        if (isRotated) transform.position += new Vector3(0f, 25f, 0f);
        else transform.position += new Vector3(-25f, 0f, 0f);
    }

    // Возвращает центр блока ячеек, которые занимает предмет.
    public bool TryGetOccupiedCellsCenter(out Vector3 center)
    {
        center = Vector3.zero;
        if (occupiedCells.Count == 0) return false;

        bool hasCell = false;
        float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

        foreach (InventoryCell cell in occupiedCells)
        {
            if (cell == null) continue;

            // В неактивной сетке localPosition ячеек ещё нулевой — считаем по сетке.
            Vector3 cellPosition = cell.transform.localPosition;
            InitializeGrid cellGrid = InitializeGrid.FindInParents(cell.transform);
            if (cellGrid != null) cellPosition = cellGrid.GetCellLocalPosition(cell.gridSize);

            if (!hasCell)
            {
                minX = maxX = cellPosition.x;
                minY = maxY = cellPosition.y;
                hasCell = true;
            }
            else
            {
                if (cellPosition.x < minX) minX = cellPosition.x;
                if (cellPosition.x > maxX) maxX = cellPosition.x;
                if (cellPosition.y < minY) minY = cellPosition.y;
                if (cellPosition.y > maxY) maxY = cellPosition.y;
            }
        }

        if (!hasCell) return false;

        center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        return true;
    }

    private void ExecutePlacement(PlacementData placementData)
    {
        RectTransform rect = transform as RectTransform;
        Vector2 savedSize = rect != null ? rect.sizeDelta : Vector2.zero;
        Vector3 savedScale = transform.localScale;
        Vector2 savedAnchorMin = rect != null ? rect.anchorMin : Vector2.zero;
        Vector2 savedAnchorMax = rect != null ? rect.anchorMax : Vector2.zero;
        Vector2 savedPivot = rect != null ? rect.pivot : Vector2.zero;

        LayoutElement layoutEl = owner.GetComponent<LayoutElement>();
        if (layoutEl == null) layoutEl = owner.gameObject.AddComponent<LayoutElement>();
        layoutEl.ignoreLayout = true;
        if (rect != null)
        {
            layoutEl.preferredWidth = savedSize.x;
            layoutEl.preferredHeight = savedSize.y;
        }

        Transform targetParent = placementData.inventoryInit.transform;
        transform.SetParent(targetParent, true);
        if (rect != null) rect.sizeDelta = savedSize;

        Vector3 targetLocalPos = targetParent.InverseTransformPoint(placementData.targetCell.transform.position);
        if (placementData.inventoryInit != null && placementData.targetCell != null)
        {
            InventoryCell targetCellData = placementData.targetCell.GetComponent<InventoryCell>();
            if (targetCellData != null)
                targetLocalPos = placementData.inventoryInit.GetCellLocalPosition(targetCellData.gridSize);
        }

        if (grid != null && grid.listGrid.Count > 1 &&
            (state.data.size.x % 2 == 0 || state.data.size.y % 2 == 0))
        {
            if (state.data.size.x != state.data.size.y)
            {
                if (isRotated) targetLocalPos = new Vector3(targetLocalPos.x, targetLocalPos.y + 25f);
                else targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
            }
            else
            {
                targetLocalPos = new Vector3(targetLocalPos.x, targetLocalPos.y + 25f);
                targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
            }
        }

        if (rect != null)
        {
            rect.anchorMin = savedAnchorMin;
            rect.anchorMax = savedAnchorMax;
            rect.pivot = savedPivot;
            rect.sizeDelta = savedSize;
        }
        transform.localScale = savedScale;

        if (view != null) view.SmoothMoveTo(targetLocalPos);
        else transform.localPosition = targetLocalPos;

        occupiedCells.Clear();
        occupiedCells.AddRange(placementData.targetGrids);
        ItemPlacementValidator.OccupyCells(occupiedCells, owner.gameObject);
        savedOccupiedCells.Clear();
    }

    private void RotateGridCells()
    {
        if (grid == null || state == null || state.data == null) return;

        int width = state.data.size.x;
        int height = state.data.size.y;

        for (int i = 0; i < grid.listGrid.Count; i++)
        {
            InventoryCell cell = grid.listGrid[i].GetComponent<InventoryCell>();
            if (cell == null) continue;

            int baseX = i / height;
            int baseY = i % height;

            if (isRotated) cell.gridSize = new Vector2Int(baseY, width - 1 - baseX);
            else cell.gridSize = new Vector2Int(baseX, baseY);
        }
    }
}
