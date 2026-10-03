using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

[CustomEditor(typeof(ItemData))]
public class ItemPrefabGenerator : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemData itemData = (ItemData)target;

        GUILayout.Space(10);

        // 2D-префаб
        GUI.enabled = itemData.modelPrefab2D != null;
        if (GUILayout.Button("Generate 2D Prefab", GUILayout.Height(30)))
            Generate2DPrefab(itemData);
        if (itemData.modelPrefab2D == null)
            EditorGUILayout.HelpBox("modelPrefab2D не назначен!", MessageType.Warning);

        GUILayout.Space(5);

        // 3D-префаб
        GUI.enabled = itemData.modelPrefab3D != null;
        if (GUILayout.Button("Generate 3D Prefab", GUILayout.Height(30)))
            Generate3DPrefab(itemData);
        if (itemData.modelPrefab3D == null)
            EditorGUILayout.HelpBox("modelPrefab3D не назначен!", MessageType.Warning);

        GUILayout.Space(5);

        // Оба префаба
        GUI.enabled = itemData.modelPrefab2D != null && itemData.modelPrefab3D != null;
        if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(30)))
        {
            Generate2DPrefab(itemData);
            Generate3DPrefab(itemData);
        }
        if (itemData.modelPrefab2D == null || itemData.modelPrefab3D == null)
            EditorGUILayout.HelpBox("Для генерации обоих префабов нужны обе модели!", MessageType.Warning);

        GUI.enabled = true;
    }

    // === СОЗДАНИЕ ВНУТРНЕННЕГО КОНТЕЙНЕРА ===
    private void CreateContainer(GameObject parent, ItemData itemData)
    {
        // 1. Создаём Conteyner
        GameObject conteyner = new GameObject("Conteyner", typeof(RectTransform));
        conteyner.transform.SetParent(parent.transform, false);

        RectTransform conteynerRect = conteyner.GetComponent<RectTransform>();
        conteynerRect.anchorMin = new Vector2(0.5f, 0.5f);
        conteynerRect.anchorMax = new Vector2(0.5f, 0.5f);
        conteynerRect.pivot = new Vector2(0.5f, 0.5f);
        conteynerRect.sizeDelta = new Vector2(800, 800);
        conteynerRect.anchoredPosition = Vector2.zero;

        // 2. UIDragHandler (для перетаскивания панели)
        conteyner.AddComponent<UIDragHandler>();

        // 3. CanvasGroup
        CanvasGroup cg = conteyner.AddComponent<CanvasGroup>();
        cg.alpha = 0f;  // Скрыт по умолчанию
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 4. InventoryGrid
        InventoryGrid invGrid = conteyner.AddComponent<InventoryGrid>();
        invGrid.allowedTypes = ItemCategory.All;
        invGrid.isContainerGrid = true;

        // 5. ContainerSizeHandler
        ContainerSizeHandler sizeHandler = conteyner.AddComponent<ContainerSizeHandler>();

        // 6. Scroll View
        GameObject scrollView = new GameObject("Scroll View", typeof(RectTransform));
        scrollView.transform.SetParent(conteyner.transform, false);
        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(35, 50);  // Left, Bottom
        scrollRect.offsetMax = new Vector2(-35, -50); // Right, Top

        ScrollRectNoDrag sr = scrollView.AddComponent<ScrollRectNoDrag>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.1f;
        sr.inertia = true;
        sr.decelerationRate = 0.135f;
        sr.scrollSensitivity = 10;

        // 7. Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        viewport.AddComponent<RectMask2D>();

        // 8. Grid
        GameObject grid = new GameObject("Grid", typeof(RectTransform));
        grid.transform.SetParent(viewport.transform, false);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0, 1);
        gridRect.anchorMax = new Vector2(0, 1);
        gridRect.pivot = new Vector2(0, 1);
        gridRect.anchoredPosition = Vector2.zero;
        gridRect.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(50, 50);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 25;

        InitializeGrid initGrid = grid.AddComponent<InitializeGrid>();
        initGrid.IsContainerGrid = true;
        initGrid.LimitColumns = true;

        // 8.1. Настраиваем размер Grid под размер контейнера
        Vector2Int containerSize = itemData.containerSize;
        int columns = Mathf.Min(containerSize.y, 25);
        int totalCells = containerSize.x * containerSize.y;
        int rows = Mathf.CeilToInt((float)totalCells / columns);

        gridRect.sizeDelta = new Vector2(
            columns * 50f,
            rows * 50f
        );

        // 8.2. Настраиваем размер Conteyner под Grid
        Vector2 padding = new Vector2(70, 100);
        Vector2 targetSize = gridRect.sizeDelta + padding;
        conteynerRect.sizeDelta = new Vector2(
            Mathf.Clamp(targetSize.x, 200, 1250),
            Mathf.Clamp(targetSize.y, 200, 800)
        );

        // 9. Scrollbar Vertical (опционально)
        GameObject scrollbar = new GameObject("Scrollbar Vertical", typeof(RectTransform));
        scrollbar.transform.SetParent(scrollView.transform, false);
        RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(20, 0);

        // 10. Связываем ScrollRect
        sr.viewport = viewportRect;
        sr.content = gridRect;
        sr.verticalScrollbar = null;  // Опционально

        // 11. Настраиваем ContainerSizeHandler
        sizeHandler.containerRect = conteynerRect;
        sizeHandler.gridRect = gridRect;

        // 12. Frame (рамка поверх всего)
        GameObject frame = new GameObject("Frame", typeof(RectTransform));
        frame.transform.SetParent(conteyner.transform, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frameImage = frame.AddComponent<Image>();
        frameImage.sprite = null;  // Сюда спрайт рамки
        frameImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        frameImage.raycastTarget = false;  // Клики проходят сквозь
    }

    // === СОЗДАНИЕ ПАПКИ ПРЕДМЕТА ===
    private string CreateItemFolder(ItemData itemData)
    {
        string categoryFolder = GetCategoryFolder(itemData.category);
        string basePath = $"Assets/Prefabs/Items/{categoryFolder}/{itemData.itemName}/";

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        return basePath;
    }

    private string GetCategoryFolder(ItemCategory category)
    {
        if ((category & ItemCategory.Weapon) != 0) return "Weapons";
        if ((category & ItemCategory.Backpack) != 0) return "Containers";
        if ((category & ItemCategory.Container) != 0) return "Containers";
        if ((category & ItemCategory.Helmet) != 0) return "Equipment";
        if ((category & ItemCategory.Armor) != 0) return "Equipment";
        if ((category & ItemCategory.Food) != 0) return "Consumables";
        if ((category & ItemCategory.Medicine) != 0) return "Consumables";
        return "Misc";
    }

    // === 2D-ПРЕФАБ ===
    private void Generate2DPrefab(ItemData itemData)
    {
        string templatePath = "Assets/Prefabs/Items/Templates/ItemTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

        if (template == null)
        {
            Debug.LogError($"ItemTemplate не найден: {templatePath}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = itemData.itemName + "_2D";

        if ((itemData.category & (ItemCategory.Container | ItemCategory.Backpack)) != 0)
        {
            CreateContainer(instance, itemData);
        }

        // ItemState
        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = itemData;
            state.childrenUI = null;
        }

        // Grid
        Transform gridTransform = instance.transform.Find("Grid");
        if (gridTransform != null)
        {
            InitializeGrid initGrid = gridTransform.GetComponent<InitializeGrid>();
            if (initGrid != null)
            {
                initGrid.IsContainerGrid = false;
                initGrid.LimitColumns = false;
            }

            Vector2Int gridSize = itemData.size;
            int columns = gridSize.y;
            Vector2 gridRectSize = new Vector2(columns * 50f, gridSize.x * 50f);

            RectTransform gridRect = gridTransform.GetComponent<RectTransform>();
            if (gridRect != null) gridRect.sizeDelta = gridRectSize;

            RectTransform selfRect = instance.GetComponent<RectTransform>();
            if (selfRect != null) selfRect.sizeDelta = gridRectSize;
        }

        // Иконка
        Image iconImage = instance.GetComponentInChildren<Image>();
        if (iconImage != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
        }

        // Сохранение в папку предмета
        string savePath = CreateItemFolder(itemData);
        string prefabPath = savePath + itemData.itemName + "_2D.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"2D-префаб создан: {prefabPath}");
    }

    // === 3D-ПРЕФАБ ===
    private void Generate3DPrefab(ItemData itemData)
    {
        string templatePath = "Assets/Prefabs/Items/Templates/Item3DTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

        if (template == null)
        {
            Debug.LogError($"Item3DTemplate не найден: {templatePath}");
            return;
        }

        if (itemData.modelPrefab3D == null)
        {
            Debug.LogWarning($"У {itemData.itemName} нет modelPrefab3D!");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = itemData.itemName + "_3D";

        // Меш и материал
        MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();

        MeshFilter sourceMesh = itemData.modelPrefab3D.GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = itemData.modelPrefab3D.GetComponentInChildren<MeshRenderer>();

        if (sourceMesh != null) meshFilter.mesh = sourceMesh.sharedMesh;
        if (sourceRenderer != null) meshRenderer.materials = sourceRenderer.sharedMaterials;

        if (meshFilter.sharedMesh != null)
        {
            boxCollider.size = meshFilter.sharedMesh.bounds.size;
            boxCollider.center = meshFilter.sharedMesh.bounds.center;
        }

        // ItemState + childrenUI
        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = itemData;

            string savePath = CreateItemFolder(itemData);
            string prefab2DPath = savePath + itemData.itemName + "_2D.prefab";
            GameObject prefab2D = AssetDatabase.LoadAssetAtPath<GameObject>(prefab2DPath);
            if (prefab2D != null)
            {
                state.childrenUI = prefab2D;
            }
        }

        // Сохранение
        string savePath3D = CreateItemFolder(itemData);
        string prefabPath = savePath3D + itemData.itemName + "_3D.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"3D-префаб создан: {prefabPath}");
    }
}