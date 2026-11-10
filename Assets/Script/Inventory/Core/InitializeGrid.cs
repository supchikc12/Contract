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

        VestLayoutData vestLayout = IsContainerGrid ? itemState.data.vestLayout : null;

        if (vestLayout != null)
            gridSize = vestLayout.GetBounds();
        else if (IsContainerGrid)
            gridSize = itemState.data.containerSize;
        else
            gridSize = itemState.data.size;

        listGrid.Clear();

        // Очищаем старые ячейки (важно для клонов)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        GenerateGrid();
        if (vestLayout != null) ApplyVestLayout(vestLayout);
        ChangeSize();
        UpdateParentSize();
    }

    // Строит карманы разгрузки: скрывает ячейки вне карманов,
    // запоминает принадлежность к карману и рисует фон карманов
    private void ApplyVestLayout(VestLayoutData layout)
    {
        foreach (GameObject cellObj in listGrid)
        {
            if (cellObj == null) continue;

            InventoryCell cell = cellObj.GetComponent<InventoryCell>();
            if (cell == null) continue;

            int pocketIndex;
            VestLayoutData.Pocket pocket = layout.GetPocketAt(cell.gridSize, out pocketIndex);
            bool available = pocket != null;

            cell.isAvailable = available;
            cell.pocketId = available ? pocketIndex : -1;
            cell.allowedCategories = available ? pocket.allowedTypes : ItemCategory.None;

            Image image = cellObj.GetComponent<Image>();
            if (image != null)
            {
                if (available)
                {
                    // Убираем спрайт ячейки, чтобы не перекрывать фон кармана.
                    // Прозрачность оставляем, чтобы ячейка ловила raycast при перетаскивании.
                    image.sprite = null;
                    image.color = new Color(1f, 1f, 1f, 0.05f);
                    image.enabled = true;
                }
                else
                {
                    image.enabled = false;
                }
            }
        }

        // Непрозрачный фон панели, чтобы за карманами не просвечивал инвентарь
        Transform scrollView = transform.parent != null ? transform.parent.parent : null;
        Transform containerPanel = scrollView != null ? scrollView.parent : null;
        if (containerPanel != null)
        {
            Image panelImage = containerPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = true;
                panelImage.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            }
        }

        Transform oldOverlay = transform.Find("VestPocketOverlay");
        if (oldOverlay != null) Destroy(oldOverlay.gameObject);

        GameObject overlay = new GameObject("VestPocketOverlay", typeof(RectTransform), typeof(LayoutElement));
        overlay.transform.SetParent(transform, false);
        overlay.transform.SetAsFirstSibling();

        LayoutElement overlayLayout = overlay.GetComponent<LayoutElement>();
        overlayLayout.ignoreLayout = true;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 1f);
        overlayRect.anchorMax = new Vector2(0f, 1f);
        overlayRect.pivot = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = new Vector2(gridSize.y * 50f, gridSize.x * 50f);

        for (int i = 0; i < layout.pockets.Count; i++)
        {
            VestLayoutData.Pocket pocket = layout.pockets[i];
            if (pocket == null) continue;

            GameObject pocketObj = new GameObject("Pocket_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pocketObj.transform.SetParent(overlay.transform, false);

            RectTransform pocketRect = pocketObj.GetComponent<RectTransform>();
            pocketRect.anchorMin = new Vector2(0f, 1f);
            pocketRect.anchorMax = new Vector2(0f, 1f);
            pocketRect.pivot = new Vector2(0f, 1f);
            pocketRect.anchoredPosition = new Vector2(pocket.position.y * 50f, -pocket.position.x * 50f);
            pocketRect.sizeDelta = new Vector2(pocket.size.y * 50f, pocket.size.x * 50f);

            Image pocketImage = pocketObj.GetComponent<Image>();
            pocketImage.color = new Color(0.22f, 0.24f, 0.28f, 0.9f);
            pocketImage.raycastTarget = false;
        }
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

        GridLayoutGroup layout = GetComponent<GridLayoutGroup>();
        Vector2 cellSize = layout != null ? layout.cellSize : new Vector2(50f, 50f);
        Vector2 spacing = layout != null ? layout.spacing : Vector2.zero;
        int padLeft = layout != null ? layout.padding.left : 0;
        int padTop = layout != null ? layout.padding.top : 0;

        float width = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
        float height = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;

        int columns = layout != null ? Mathf.Max(1, layout.constraintCount) : Mathf.Max(1, gridSize.y);
        int index = coord.x * Mathf.Max(1, gridSize.y) + coord.y;
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
