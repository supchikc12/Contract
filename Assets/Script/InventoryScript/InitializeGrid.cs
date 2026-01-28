using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Этот скрипт создает "коврик" из ячеек 1x1 внутри рюкзака или склада
public class InitializeGrid : MonoBehaviour
{
    [Header("Настройки префаба")]
    [SerializeField] private GameObject prefabGrid; // Префаб одной ячейки (с компонентом Grid)

    private Vector2Int gridSize; // Сколько ячеек по X и Y

    [Header("Сгенерированные данные")]
    // Список всех созданных ячеек (нужен для логики предметов)
    public List<GameObject> listGrid = new List<GameObject>();

    private void Awake()
    {
        ItemState itemState = GetComponentInParent<ItemState>();
        if (itemState == null || itemState.data == null)
        {
            Debug.LogError("InitializeGrid requires an ItemState with assigned ItemData in a parent.");
            return;
        }

        gridSize = itemState.data.size;
        // Сначала очищаем список, если в нем что-то было
        listGrid.Clear();
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        // 1. Убеждаемся, что на объекте есть GridLayoutGroup. 
        // Он автоматически расставит ячейки ровно, нам не нужно считать координаты вручную.
        GridLayoutGroup layout = GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<GridLayoutGroup>();
        }

        // 3. Создаем ячейки в цикле
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                // Создаем ячейку как дочерний объект сетки
                GameObject newGrid = Instantiate(prefabGrid, transform);
                Grid grid = newGrid.GetComponent<Grid>();
                grid.gridSize.x = i;
                grid.gridSize.y = j;
                // Называем её для удобства отладки
                newGrid.name = $"Cell_{i}_{j}";

                // СОВЕТ: Делаем ячейку невидимой (как в твоем коде), 
                // но лучше управлять этим через прозрачность префаба в инспекторе.
                if (newGrid.TryGetComponent<Image>(out Image img) && gameObject.layer == 6)
                {
                    img.color = new Color(1, 1, 1, 0f);
                }

                // Добавляем созданную ячейку в список
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

    // ...added utility methods for external use by ItemInstance
    // Возвращает размерность сетки (количество столбцов X и строк Y).
    public Vector2Int GetDimensions()
    {
        int maxX = 0, maxY = 0;
        foreach (var go in listGrid)
        {
            Grid g = go.GetComponent<Grid>();
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
            Grid g = go.GetComponent<Grid>();
            if (g == null) continue;
            if (g.gridSize == coord) return go;
        }
        return null;
    }

    // Возвращает Grid-компонент ячейки по координатам (x,y) или null.
    public Grid GetGridAt(Vector2Int coord)
    {
        var go = GetCellAt(coord);
        return go != null ? go.GetComponent<Grid>() : null;
    }
}
