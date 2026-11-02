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

    private void Awake()
    {
        ItemState itemState = GetComponentInParent<ItemState>();
        if (itemState == null || itemState.data == null)
        {
            string parentName = "неизвестно (объект без родителя)";

            // Проверяем, есть ли у нас родитель, перед тем как обращаться к нему
            if (transform.parent != null)
            {
                parentName = transform.parent.gameObject.name;
            }

            Debug.LogError("InitializeGrid требует ItemState с назначенными ItemData. Родительский объект: " + parentName);
            return;
        }

        if (IsContainerGrid)
            gridSize = itemState.data.containerSize;
        else
            gridSize = itemState.data.size;

        // Сначала очищаем список, если в нем что-то было
        listGrid.Clear();
        GenerateGrid();
        ChangeSize();       

        // Подстраиваем родителя под размер Grid
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

        rectTransform.sizeDelta = new Vector2(width, height);

        GridLayoutGroup gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
            gridLayoutGroup.constraintCount = columns;
    }
    private void GenerateGrid()
    {

        if (prefabGrid == null)
        {
            Debug.LogError("InitializeGrid: prefabGrid не назначен!");
            return;
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
