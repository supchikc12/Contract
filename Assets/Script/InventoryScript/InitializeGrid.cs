using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ётот скрипт создает "коврик" из €чеек 1x1 внутри рюкзака или склада
public class InitializeGrid : MonoBehaviour
{
    [Header("Ќастройки префаба")]
    [SerializeField] private GameObject prefabGrid; // ѕрефаб одной €чейки (с компонентом Grid)

    private Vector2Int gridSize; // —колько €чеек по X и Y

    [Header("—генерированные данные")]
    // —писок всех созданных €чеек (нужен дл€ логики предметов)
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
        // —начала очищаем список, если в нем что-то было
        listGrid.Clear();
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        // 1. ”беждаемс€, что на объекте есть GridLayoutGroup. 
        // ќн автоматически расставит €чейки ровно, нам не нужно считать координаты вручную.
        GridLayoutGroup layout = GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<GridLayoutGroup>();
        }

        // 3. —оздаем €чейки в цикле
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                // —оздаем €чейку как дочерний объект сетки
                GameObject newGrid = Instantiate(prefabGrid, transform);

                // Ќазываем еЄ дл€ удобства отладки
                newGrid.name = $"Cell_{i}_{j}";

                // —ќ¬≈“: ƒелаем €чейку невидимой (как в твоем коде), 
                // но лучше управл€ть этим через прозрачность префаба в инспекторе.
                if (newGrid.TryGetComponent<Image>(out Image img) && gameObject.layer == 6)
                {
                    img.color = new Color(1, 1, 1, 0f);
                }

                // ƒобавл€ем созданную €чейку в список
                listGrid.Add(newGrid);
            }
            
        }
    }

    // ћетод дл€ очистки сетки (например, если мы мен€ем рюкзак)
    public void ClearGrid()
    {
        foreach (GameObject cell in listGrid)
        {
            Destroy(cell);
        }
        listGrid.Clear();
    }
}
