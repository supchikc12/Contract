using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum PrefabOverwriteMode
{
    Overwrite,
    SkipExisting,
    CreateCopy
}

// Единая фабрика prefabs предметов. Используется мастером и инспектором ItemData.
public static class ItemPrefabFactory
{
    public static List<string> Validate(ItemData data)
    {
        List<string> errors = new List<string>();

        if (data == null) { errors.Add("ItemData не задан."); return errors; }
        if (string.IsNullOrWhiteSpace(data.id)) errors.Add("Не заполнен id предмета.");
        if (string.IsNullOrWhiteSpace(data.itemName)) errors.Add("Не заполнено itemName.");
        if (data.size.x <= 0 || data.size.y <= 0) errors.Add("size должен быть больше нуля.");
        if (data.category == ItemCategory.None) errors.Add("Не выбрана category.");
        if (data.icon == null) errors.Add("Не назначена иконка.");

        if (data.sourceModel3D == null && data.modelPrefab3D == null)
            errors.Add("Не назначена исходная 3D-модель (sourceModel3D).");

        ContainerData container = data as ContainerData;
        if (container != null)
        {
            if (container.grid == null)
                errors.Add("У контейнера не назначен ContainerGridData.");
            else
            {
                errors.AddRange(ContainerGridValidator.Validate(container.grid, container is RigData));
            }
        }

        return errors;
    }

    public static bool Generate(ItemData data, PrefabOverwriteMode mode)
    {
        List<string> errors = Validate(data);
        if (errors.Count > 0)
        {
            foreach (string error in errors)
                Debug.LogError($"ItemPrefabFactory: {data?.itemName}: {error}");
            return false;
        }

        GameObject prefab2D = Create2D(data, mode);
        if (prefab2D == null) return false;

        data.modelPrefab2D = prefab2D;
        EditorUtility.SetDirty(data);

        GameObject prefab3D = Create3D(data, mode);
        if (prefab3D == null) return false;

        data.modelPrefab3D = prefab3D;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return true;
    }

    public static GameObject Create2D(ItemData data, PrefabOverwriteMode mode)
    {
        string path = ResolvePath(ItemPrefabPaths.Get2DPath(data), mode);
        if (path == null) return null;

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (mode == PrefabOverwriteMode.SkipExisting && existing != null)
            return existing;

        ItemPrefabPaths.EnsureFolder(ItemPrefabPaths.GetPrefabFolder(data));

        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPaths.ItemTemplate);
        if (template == null)
        {
            Debug.LogError($"ItemPrefabFactory: не найден шаблон {ItemPrefabPaths.ItemTemplate}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = data.itemName + "_2D";

        ConfigureRootRect(instance, data.size);
        ConfigureItemState(instance, data, null);
        ConfigureItemView(instance, data);
        ConfigureItemGrid(instance, data);

        ContainerData container = data as ContainerData;
        if (container != null)
            CreateContainerPanel(instance, container);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        return saved != null ? saved : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    public static GameObject Create3D(ItemData data, PrefabOverwriteMode mode)
    {
        GameObject source = data.sourceModel3D != null ? data.sourceModel3D : data.modelPrefab3D;
        if (source == null)
        {
            Debug.LogError($"ItemPrefabFactory: у {data.itemName} нет sourceModel3D.");
            return null;
        }

        string path = ResolvePath(ItemPrefabPaths.Get3DPath(data), mode);
        if (path == null) return null;

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (mode == PrefabOverwriteMode.SkipExisting && existing != null)
            return existing;

        ItemPrefabPaths.EnsureFolder(ItemPrefabPaths.GetPrefabFolder(data));

        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPaths.Item3DTemplate);
        if (template == null)
        {
            Debug.LogError($"ItemPrefabFactory: не найден шаблон {ItemPrefabPaths.Item3DTemplate}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = data.itemName + "_3D";

        MeshFilter sourceFilter = source.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer sourceRenderer = source.GetComponentInChildren<MeshRenderer>(true);
        if (sourceFilter == null)
        {
            Debug.LogError($"ItemPrefabFactory: у {source.name} нет MeshFilter.");
            Object.DestroyImmediate(instance);
            return null;
        }

        MeshFilter targetFilter = instance.GetComponent<MeshFilter>();
        if (targetFilter == null) targetFilter = instance.AddComponent<MeshFilter>();
        targetFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer targetRenderer = instance.GetComponent<MeshRenderer>();
        if (targetRenderer == null) targetRenderer = instance.AddComponent<MeshRenderer>();
        if (sourceRenderer != null) targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;

        BoxCollider box = instance.GetComponent<BoxCollider>();
        if (box == null) box = instance.AddComponent<BoxCollider>();
        box.isTrigger = false;
        if (targetFilter.sharedMesh != null)
        {
            box.size = targetFilter.sharedMesh.bounds.size;
            box.center = targetFilter.sharedMesh.bounds.center;
        }

        Rigidbody body = instance.GetComponent<Rigidbody>();
        if (body == null) body = instance.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.mass = Mathf.Max(0.1f, data.weight);
        body.constraints = RigidbodyConstraints.FreezeRotation;

        ConfigureItemState(instance, data, data.modelPrefab2D);
        SetLayerRecursively(instance, LayerMask.NameToLayer("Item"));

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        return saved != null ? saved : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void ConfigureRootRect(GameObject root, Vector2Int size)
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null) return;

        Vector2 dimensions = new Vector2(size.y * 50f, size.x * 50f);
        rect.sizeDelta = dimensions;

        Transform grid = root.transform.Find("Grid");
        if (grid == null) return;

        RectTransform gridRect = grid.GetComponent<RectTransform>();
        if (gridRect != null) gridRect.sizeDelta = dimensions;

        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.cellSize = new Vector2(50f, 50f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, size.y);
        }
    }

    private static void ConfigureItemState(GameObject root, ItemData data, GameObject childrenUI)
    {
        ItemState state = root.GetComponent<ItemState>();
        if (state == null) state = root.AddComponent<ItemState>();

        state.data = data;
        state.childrenUI = childrenUI;
        state.currentStack = 1;
        state.currentDurability = Mathf.Max(1f, data.maxDurability);
    }

    private static void ConfigureItemView(GameObject root, ItemData data)
    {
        ItemView view = root.GetComponent<ItemView>();
        if (view == null) view = root.AddComponent<ItemView>();

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null) group = root.AddComponent<CanvasGroup>();

        Transform imageTransform = root.transform.Find("Image");
        Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        TMP_Text stackText = root.transform.Find("StackText") != null
            ? root.transform.Find("StackText").GetComponent<TMP_Text>()
            : null;

        if (image != null)
        {
            image.sprite = data.icon;
            image.raycastTarget = true;
        }

        SerializedObject so = new SerializedObject(view);
        SetObject(so, "canvasGroup", group);
        SetObject(so, "iconImage", image);
        SetObject(so, "iconRect", imageTransform as RectTransform);
        SetObject(so, "stackText", stackText);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureItemGrid(GameObject root, ItemData data)
    {
        Transform gridTransform = root.transform.Find("Grid");
        if (gridTransform == null)
        {
            Debug.LogError($"ItemPrefabFactory: у {data.itemName} нет Grid.");
            return;
        }

        InitializeGrid grid = gridTransform.GetComponent<InitializeGrid>();
        if (grid == null) grid = gridTransform.gameObject.AddComponent<InitializeGrid>();

        SetGridPrefab(grid, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventoryCell.prefab"));
        grid.IsContainerGrid = false;
        grid.LimitColumns = false;
        grid.maxColumns = Mathf.Max(1, data.size.y);
    }

    private static void CreateContainerPanel(GameObject root, ContainerData data)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPaths.ContainerTemplate);
        if (template == null)
        {
            Debug.LogError($"ItemPrefabFactory: не найден {ItemPrefabPaths.ContainerTemplate}");
            return;
        }

        GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(template);
        panel.name = "Conteyner";
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
        }

        InventoryGrid filter = panel.GetComponent<InventoryGrid>();
        if (filter != null)
        {
            filter.allowedTypes = data.grid.allowedCategories;
            filter.isContainerGrid = true;
        }

        Transform gridTransform = panel.transform.Find("Scroll View/Viewport/Grid");
        if (gridTransform == null) gridTransform = panel.transform.Find("Content/Viewport/Grid");
        if (gridTransform == null)
        {
            Debug.LogError($"ItemPrefabFactory: в ContainerTemplate не найден Grid.");
            Object.DestroyImmediate(panel);
            return;
        }

        Vector2Int bounds = data.grid.GetBounds();
        int columns = Mathf.Min(bounds.y, data.grid.maxColumns);
        int rows = bounds.x;

        float gridWidth = columns * 50f;
        float gridHeight = rows * 50f;
        if (data.grid.HasPockets && data.grid.pocketGapPixels > 0)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<Vector2Int> sizes = new List<Vector2Int>();
            foreach (ContainerGridData.Pocket pocket in data.grid.pockets)
            {
                if (pocket == null) continue;
                positions.Add(pocket.position);
                sizes.Add(pocket.size);
            }

            List<float> originX;
            List<float> originY;
            ContainerGridValidator.CalculatePocketOrigins(
                positions, sizes, data.grid.pocketGapPixels, out originX, out originY);

            for (int i = 0; i < positions.Count; i++)
            {
                gridWidth = Mathf.Max(gridWidth, originX[i] + sizes[i].y * 50f);
                gridHeight = Mathf.Max(gridHeight, originY[i] + sizes[i].x * 50f);
            }
        }

        RectTransform gridRect = gridTransform.GetComponent<RectTransform>();
        if (gridRect != null)
            gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

        GridLayoutGroup layout = gridTransform.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.cellSize = new Vector2(50f, 50f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, columns);
        }

        InitializeGrid grid = gridTransform.GetComponent<InitializeGrid>();
        if (grid != null)
        {
            SetGridPrefab(grid, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventoryCell.prefab"));
            grid.IsContainerGrid = true;
            grid.LimitColumns = data.grid.limitColumns;
            grid.maxColumns = data.grid.maxColumns;
        }

        ContainerSizeHandler sizeHandler = panel.GetComponent<ContainerSizeHandler>();
        if (sizeHandler != null)
        {
            sizeHandler.containerRect = panelRect;
            sizeHandler.gridRect = gridRect;
        }

        TMP_Text title = FindTitle(panel.transform);
        if (title != null) title.text = data.itemName;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(
                Mathf.Clamp(gridWidth + 100f, 200f, 1250f),
                Mathf.Clamp(gridHeight + 100f, 200f, 1450f));
        }
    }

    private static TMP_Text FindTitle(Transform panel)
    {
        Transform title = panel.Find("Header/Title");
        if (title == null) title = panel.Find("Header/TarkovContainerTitle");
        return title != null ? title.GetComponent<TMP_Text>() : null;
    }

    private static string ResolvePath(string basePath, PrefabOverwriteMode mode)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(basePath) == null)
            return basePath;

        if (mode == PrefabOverwriteMode.SkipExisting)
            return basePath;

        if (mode == PrefabOverwriteMode.CreateCopy)
        {
            string directory = System.IO.Path.GetDirectoryName(basePath).Replace('\\', '/');
            string name = System.IO.Path.GetFileNameWithoutExtension(basePath);
            string extension = System.IO.Path.GetExtension(basePath);
            for (int i = 2; i < 1000; i++)
            {
                string candidate = directory + "/" + name + "_" + i + extension;
                if (AssetDatabase.LoadAssetAtPath<Object>(candidate) == null)
                    return candidate;
            }
        }

        return basePath;
    }

    private static void SetGridPrefab(InitializeGrid grid, GameObject prefab)
    {
        if (grid == null) return;
        SerializedObject so = new SerializedObject(grid);
        SerializedProperty property = so.FindProperty("prefabGrid");
        if (property != null) property.objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(SerializedObject so, string field, Object value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0) layer = 8;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
