using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Этот скрипт создает "коврик" из ячеек 1x1 внутри рюкзака или склада
public class InitializeGrid : MonoBehaviour
{
    [Header("Настройки префаба")]
    [SerializeField] private GameObject prefabGrid; // Префаб одной ячейки (с компонентом InventoryCell)

    private Vector2Int gridSize; // Сколько ячеек по X и Y
    
    [Header("Тип сетки")]
    [SerializeField] public bool IsContainerGrid = false;
    [SerializeField] public bool LimitColumns = false;
    [SerializeField] public int maxColumns = 20;

    [Header("Сгенерированные данные")]
    // Список всех созданных ячеек (нужен для логики предметов)
    public List<GameObject> listGrid = new List<GameObject>();

    private ContainerGridData containerGrid;
    private bool usePixelGaps;
    private readonly List<float> pocketOriginX = new List<float>();
    private readonly List<float> pocketOriginY = new List<float>();

    private void Awake() // ? БЫЛО Start, СТАЛО Awake
    {
        TryInit();
    }

    public void TryInit()
    {
        ItemState itemState = ItemState.FindInParents(transform);
        if (itemState == null || itemState.data == null)
        {
            return; // Тихо ждем, пока InventoryManager передаст данные
        }

        ItemData data = itemState.data;
        ContainerData containerData = null;

        if (IsContainerGrid)
        {
            containerData = data as ContainerData;
            if (containerData == null || containerData.grid == null)
            {
                Debug.LogError($"InitializeGrid: у предмета {data.itemName} нет ContainerData или ContainerGridData", this);
                return;
            }

            gridSize = containerData.grid.GetBounds();
            LimitColumns = containerData.grid.limitColumns;
            maxColumns = containerData.grid.maxColumns;
        }
        else
        {
            gridSize = data.size;
        }

        listGrid.Clear();

        // Очищаем старые ячейки (важно для клонов)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        PreparePixelGaps(containerData != null ? containerData.grid : null);

        GenerateGrid();
        ChangeSize();
        if (containerData != null && containerData.grid.HasPockets)
            ContainerGridVisuals.Apply(transform, listGrid, gridSize, containerData.grid);
        UpdateParentSize();
    }

    private void UpdateParentSize()
    {
        if (IsContainerGrid) return;  // Не трогаем контейнеры

        RectTransform parentRect = transform.parent?.GetComponent<RectTransform>();
        if (parentRect == null) return;

        // Проверяем, что родитель — ItemTemplate (есть ItemState)
        ItemState parentState = parentRect.GetComponent<ItemState>();
        if (parentState == null) return;

        parentRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
    }

    void ChangeSize()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        int columns = LimitColumns ? Mathf.Min(gridSize.y, maxColumns) : gridSize.y;
        float width = columns * 50f;
        float height = gridSize.x * 50f;

        if (usePixelGaps && containerGrid != null)
        {
            for (int i = 0; i < containerGrid.pockets.Count && i < pocketOriginX.Count; i++)
            {
                ContainerGridData.Pocket pocket = containerGrid.pockets[i];
                if (pocket == null) continue;

                width = Mathf.Max(width, pocketOriginX[i] + pocket.size.y * 50f);
                height = Mathf.Max(height, pocketOriginY[i] + pocket.size.x * 50f);
            }
        }

        rectTransform.sizeDelta = new Vector2(width, height);

        GridLayoutGroup gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.enabled = !usePixelGaps;
            gridLayoutGroup.constraintCount = columns;
        }

        if (usePixelGaps) ApplyPixelCellLayout();
    }

    private void PreparePixelGaps(ContainerGridData grid)
    {
        containerGrid = grid;
        usePixelGaps = false;
        pocketOriginX.Clear();
        pocketOriginY.Clear();

        if (grid == null || !grid.HasPockets || grid.pocketGapPixels <= 0)
            return;

        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (ContainerGridData.Pocket pocket in grid.pockets)
        {
            if (pocket == null) continue;
            positions.Add(pocket.position);
            sizes.Add(pocket.size);
        }

        List<float> originsX;
        List<float> originsY;
        ContainerGridValidator.CalculatePocketOrigins(
            positions, sizes, grid.pocketGapPixels, out originsX, out originsY);

        pocketOriginX.AddRange(originsX);
        pocketOriginY.AddRange(originsY);
        usePixelGaps = pocketOriginX.Count > 0;
    }

    private void ApplyPixelCellLayout()
    {
        foreach (GameObject cellObj in listGrid)
        {
            if (cellObj == null) continue;
            InventoryCell cell = cellObj.GetComponent<InventoryCell>();
            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
            if (cell == null || cellRect == null) continue;

            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(50f, 50f);
            cellRect.anchoredPosition = GetPixelCellLocalPosition(cell.gridSize);
        }
    }

    private Vector3 GetPixelCellLocalPosition(Vector2Int coord)
    {
        RectTransform rect = GetComponent<RectTransform>();
        float width = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
        float height = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;

        float originX = coord.y * 50f;
        float originY = coord.x * 50f;
        int startRow = coord.x;
        int startColumn = coord.y;

        if (containerGrid != null)
        {
            int pocketIndex;
            ContainerGridData.Pocket pocket = containerGrid.GetPocketAt(coord, out pocketIndex);
            if (pocket != null && pocketIndex >= 0 && pocketIndex < pocketOriginX.Count)
            {
                originX = pocketOriginX[pocketIndex];
                originY = pocketOriginY[pocketIndex];
                startRow = pocket.position.x;
                startColumn = pocket.position.y;
            }
        }

        float x = -rect.pivot.x * width + originX + (coord.y - startColumn) * 50f + 25f;
        float y = (1f - rect.pivot.y) * height - originY - (coord.x - startRow) * 50f - 25f;
        return new Vector3(x, y, 0f);
    }
    private void GenerateGrid()
    {

        if (prefabGrid == null)
        {
            Debug.LogError("InitializeGrid: prefabGrid не назначен!");
            return;
        }

        // Удаляем старые ячейки (важно при клонировании предметов)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.SetParent(null); // Отвязываем, чтобы они не мешали
            Destroy(child.gameObject);
        }

        //Убеждаемся, что на объекте есть GridLayoutGroup.
        GridLayoutGroup layout = GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<GridLayoutGroup>();

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = LimitColumns ? Mathf.Min(gridSize.y, maxColumns) : gridSize.y;
        layout.cellSize = new Vector2(50, 50);

        // Создаем ячейки в цикле
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                // Создаем ячейку как дочерний объект сетки
                GameObject newGrid = Instantiate(prefabGrid, transform);
                InventoryCell cell = newGrid.GetComponent<InventoryCell>();
                Transform parent = transform.parent;
                newGrid.layer = parent.gameObject.layer;
                cell.gridSize.x = i;
                cell.gridSize.y = j;
                // Называем её для удобства отладки
                newGrid.name = $"Cell_{i}_{j}";
 
                listGrid.Add(newGrid);
            }
            
        }
    }

    // Поиск InitializeGrid по родителям без учета activeInHierarchy
    public static InitializeGrid FindInParents(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            InitializeGrid grid = current.GetComponent<InitializeGrid>();
            if (grid != null) return grid;
            current = current.parent;
        }
        return null;
    }

    // Метод для очистки сетки (например, если мы меняем рюкзак)
    public void ClearGrid()
    {
        foreach (GameObject cell in listGrid)
        {
            Destroy(cell);
        }
        listGrid.Clear();
    }

    // Возвращает размерность сетки (количество столбцов X и строк Y).
    public Vector2Int GetDimensions()
    {
        int maxX = 0, maxY = 0;
        foreach (var go in listGrid)
        {
            InventoryCell g = go.GetComponent<InventoryCell>();
            if (g == null) continue;
            if (g.gridSize.x > maxX) maxX = g.gridSize.x;
            if (g.gridSize.y > maxY) maxY = g.gridSize.y;
        }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    // Локальная позиция центра ячейки, рассчитанная по параметрам GridLayoutGroup.
    // Работает и в неактивной иерархии, когда LayoutGroup ещё не расставил детей.
    public Vector3 GetCellLocalPosition(Vector2Int coord)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return Vector3.zero;

        if (usePixelGaps) return GetPixelCellLocalPosition(coord);

        GridLayoutGroup layout = GetComponent<GridLayoutGroup>();
        Vector2 cellSize = layout != null ? layout.cellSize : new Vector2(50f, 50f);
        Vector2 spacing = layout != null ? layout.spacing : Vector2.zero;
        int padLeft = layout != null ? layout.padding.left : 0;
        int padTop = layout != null ? layout.padding.top : 0;

        float width = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
        float height = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;

        int columns = layout != null ? Mathf.Max(1, layout.constraintCount) : Mathf.Max(1, gridSize.y);
        int index = coord.x * Mathf.Max(1, columns) + coord.y;
        int row = index / columns;
        int column = index % columns;

        float x = -rect.pivot.x * width + padLeft + column * (cellSize.x + spacing.x) + cellSize.x * 0.5f;
        float y = (1f - rect.pivot.y) * height - padTop - row * (cellSize.y + spacing.y) - cellSize.y * 0.5f;
        return new Vector3(x, y, 0f);
    }

    // Возвращает GameObject ячейки инвентаря по координатам (x,y) или null, если не найдено.
    public GameObject GetCellAt(Vector2Int coord)
    {
        foreach (var go in listGrid)
        {
            InventoryCell g = go.GetComponent<InventoryCell>();
            if (g == null) continue;
            if (g.gridSize == coord) return go;
        }
        return null;
    }

    // Возвращает InventoryCell-компонент ячейки по координатам (x,y) или null.
    public InventoryCell GetGridAt(Vector2Int coord)
    {
        var go = GetCellAt(coord);
        return go != null ? go.GetComponent<InventoryCell>() : null;
    }
}
