using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Этот скрипт создает "коврик" из ячеек 1x1 внутри рюкзака или склада
public class InitializeGrid : MonoBehaviour
{
    [Header("Настройки префаба")]
    [SerializeField] private GameObject prefabGrid; // Префаб одной ячейки (с компонентом Grid)

    [Header("Размер сетки")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10); // Сколько ячеек по X и Y

    [Header("Сгенерированные данные")]
    // Список всех созданных ячеек (нужен для логики предметов)
    public List<GameObject> listGrid = new List<GameObject>();

    private void Awake()
    {
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

        // 2. Рассчитываем общее количество ячеек
        int totalCells = gridSize.x * gridSize.y;

        // 3. Создаем ячейки в цикле
        for (int i = 0; i < totalCells; i++)
        {
            // Создаем ячейку как дочерний объект сетки
            GameObject newGrid = Instantiate(prefabGrid, transform);

            // Называем её для удобства отладки
            newGrid.name = $"Cell_{i}";

            // СОВЕТ: Делаем ячейку невидимой (как в твоем коде), 
            // но лучше управлять этим через прозрачность префаба в инспекторе.
            if (newGrid.TryGetComponent<Image>(out Image img))
            {
                img.color = new Color(1, 1, 1, 0f);
            }

            // Добавляем созданную ячейку в список
            listGrid.Add(newGrid);
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
}
