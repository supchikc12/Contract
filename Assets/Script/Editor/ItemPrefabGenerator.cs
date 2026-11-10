using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

[CustomEditor(typeof(ItemData), true)]
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
        string templatePath = "Assets/Prefabs/Items/Templates/ContainerTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
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
        if ((category & ItemCategory.WeaponMod) != 0) return "Mods";
        if ((category & ItemCategory.Rig) != 0) return "Containers";
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
        if (iconImage != null)
        {
            // Иконка должна ловить клики и начало drag мышью,
            // иначе события уходят в ячейку/ScrollRect под предметом.
            iconImage.raycastTarget = true;

            if (itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
            }
        }

        // ЕСЛИ КОНТЕЙНЕР — добавляем дочерний Conteyner
        bool isContainer = (itemData.category & (ItemCategory.Container | ItemCategory.Backpack | ItemCategory.Rig)) != 0;
        if (isContainer)
        {
            AddContainer(instance, itemData);
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

    // == ДОБАВЛЯЕМ КОНТЕЙНЕР
    // === ДОБАВЛЕНИЕ КОНТЕЙНЕРА ===
    private void AddContainer(GameObject parent, ItemData itemData)
    {
        // 1. Загружаем ContainerTemplate
        string templatePath = "Assets/Prefabs/Items/Templates/ContainerTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

        if (template == null)
        {
            Debug.LogError($"ContainerTemplate не найден: {templatePath}");
            return;
        }

        // 2. Создаём экземпляр и делаем дочерним
        GameObject conteyner = (GameObject)PrefabUtility.InstantiatePrefab(template);
        conteyner.name = "Conteyner";
        conteyner.transform.SetParent(parent.transform, false);

        // 3. RectTransform — center-middle
        RectTransform conteynerRect = conteyner.GetComponent<RectTransform>();
        conteynerRect.anchorMin = new Vector2(0.5f, 0.5f);
        conteynerRect.anchorMax = new Vector2(0.5f, 0.5f);
        conteynerRect.pivot = new Vector2(0.5f, 0.5f);
        conteynerRect.anchoredPosition = Vector2.zero;

        // 4. Title
        Transform titleTransform = conteyner.transform.Find("Header/Title");
        if (titleTransform != null)
        {
            TMPro.TextMeshProUGUI title = titleTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (title != null) title.text = itemData.itemName;
        }

        // 5. Настраиваем Grid внутри Conteyner
        Transform grid = conteyner.transform.Find("Content/Viewport/Grid");
        if (grid == null)
        {
            Debug.LogWarning("Grid не найден в ContainerTemplate!");
            return;
        }

        RectTransform gridRect = grid.GetComponent<RectTransform>();

        Vector2Int containerSize = itemData.containerSize;
        int columns = Mathf.Min(containerSize.y, 23);
        int totalCells = containerSize.x * containerSize.y;
        int rows = Mathf.CeilToInt((float)totalCells / columns);

        gridRect.sizeDelta = new Vector2(columns * 50f, rows * 50f);

        // 6. GridLayoutGroup
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.cellSize = new Vector2(50, 50);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
        }

        // 7. InitializeGrid
        InitializeGrid initGrid = grid.GetComponent<InitializeGrid>();
        if (initGrid != null)
        {
            initGrid.IsContainerGrid = true;
            initGrid.LimitColumns = true;
        }

        // 8. ContainerSizeHandler
        ContainerSizeHandler sizeHandler = conteyner.GetComponent<ContainerSizeHandler>();
        if (sizeHandler != null)
        {
            sizeHandler.containerRect = conteynerRect;
            sizeHandler.gridRect = gridRect;

            Vector2 padding = new Vector2(20, 60);
            Vector2 targetSize = gridRect.sizeDelta + padding;
            conteynerRect.sizeDelta = new Vector2(
                Mathf.Clamp(targetSize.x, 200, 1250),
                Mathf.Clamp(targetSize.y, 200, 800)
            );
        }
    }
}