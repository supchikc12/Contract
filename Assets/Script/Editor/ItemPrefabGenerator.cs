using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(ItemData))]
public class ItemPrefabGenerator : Editor
{
    public override void OnInspectorGUI()
    {
        // Рисуем стандартный инспектор
        DrawDefaultInspector();

        ItemData itemData = (ItemData)target;

        GUILayout.Space(10);

        // 2D-префаб
        GUI.enabled = itemData.modelPrefab2D != null;
        if (GUILayout.Button("Generate 2D Prefab", GUILayout.Height(30)))
        {
            GeneratePrefab(itemData);
        }
        if (itemData.modelPrefab2D == null)
            EditorGUILayout.HelpBox("modelPrefab2D не назначен!", MessageType.Warning);

        GUILayout.Space(5);

        // 3D-префаб
        GUI.enabled = itemData.modelPrefab3D != null;
        if (GUILayout.Button("Generate 3D Prefab", GUILayout.Height(30)))
        {
            Generate3DPrefab(itemData);
        }
        if (itemData.modelPrefab3D == null)
            EditorGUILayout.HelpBox("modelPrefab3D не назначен!", MessageType.Warning);

        GUILayout.Space(5);

        // Оба префаба
        GUI.enabled = itemData.modelPrefab2D != null && itemData.modelPrefab3D != null;
        if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(30)))
        {
            GeneratePrefab(itemData);
            Generate3DPrefab(itemData);
        }
        if (itemData.modelPrefab2D == null || itemData.modelPrefab3D == null)
            EditorGUILayout.HelpBox("Для генерации обоих префабов нужны обе модели!", MessageType.Warning);

        GUI.enabled = true;
    }

    private void GeneratePrefab(ItemData itemData)
    {
        // 1. Загружаем шаблон
        string templatePath = "Assets/Resources/Templates/ItemTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

        if (template == null)
        {
            Debug.LogError($"ItemTemplate не найден по пути: {templatePath}");
            return;
        }

        // 2. Создаём экземпляр
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = itemData.itemName + "_2D";

        // 3. Заполняем ItemState
        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = itemData;
            state.childrenUI = null;  // Заполним позже
        }

        // 4. Заполняем ItemInstance
        ItemInstance itemInstance = instance.GetComponent<ItemInstance>();
        if (itemInstance != null)
        {
            // Настройки, если нужно
        }

        // 5. Настраиваем Grid (верхний, для размера предмета)
        Transform gridTransform = instance.transform.Find("Grid");
        if (gridTransform != null)
        {
            InitializeGrid initGrid = gridTransform.GetComponent<InitializeGrid>();
            if (initGrid != null)
            {
                initGrid.isContainerGrid = false;
                initGrid.LimitColumns = false;
            }

            if (itemData != null)
            {
                Vector2Int gridSize = itemData.size;
                int columns = gridSize.y;  // столбцы
                Vector2 gridRectSize = new Vector2(columns * 50f, gridSize.x * 50f);

                // 1. Размер Grid
                RectTransform gridRect = gridTransform.GetComponent<RectTransform>();
                if (gridRect != null)
                {
                    gridRect.sizeDelta = gridRectSize;
                }

                // 2. Размер ItemTemplate (родителя) + padding
                RectTransform selfRect = instance.GetComponent<RectTransform>();
                if (selfRect != null)
                {
                    selfRect.sizeDelta = gridRectSize + new Vector2(10, 10);
                }
            }
        }

        // 6. Создаём папку для сохранения
        string savePath = "Assets/Resources/Generated/";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 7. Сохраняем префаб
        string prefabPath = savePath + itemData.itemName + "_2D.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        // 8. Удаляем экземпляр из сцены
        DestroyImmediate(instance);

        // 9. Обновляем AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Префаб создан: {prefabPath}");
    }

    private void Generate3DPrefab(ItemData itemData)
    {
        // 1. Загружаем шаблон
        string templatePath = "Assets/Resources/Templates/Item3DTemplate.prefab";
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

        if (template == null)
        {
            Debug.LogError($"Item3DTemplate не найден по пути: {templatePath}");
            return;
        }

        // 2. Проверяем, есть ли 3D-модель
        if (itemData.modelPrefab3D == null)
        {
            Debug.LogWarning($"У {itemData.itemName} нет modelPrefab3D!");
            return;
        }

        // 3. Создаём экземпляр
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = itemData.itemName + "_3D";

        // 4. Заменяем меш на модель из ItemData
        MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();

        if (itemData.modelPrefab3D != null)
        {
            // Берём меш из модели
            MeshFilter sourceMesh = itemData.modelPrefab3D.GetComponentInChildren<MeshFilter>();
            MeshRenderer sourceRenderer = itemData.modelPrefab3D.GetComponentInChildren<MeshRenderer>();

            if (sourceMesh != null) meshFilter.mesh = sourceMesh.sharedMesh;
            if (sourceRenderer != null) meshRenderer.materials = sourceRenderer.sharedMaterials;

            // Подгоняем коллайдер под размер меша
            if (meshFilter.sharedMesh != null)
            {
                boxCollider.size = meshFilter.sharedMesh.bounds.size;
                boxCollider.center = meshFilter.sharedMesh.bounds.center;
            }
        }

        // 5. Заполняем ItemState
        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = itemData;
            // childrenUI заполним после создания 2D-префаба
            string prefab2DPath = $"Assets/Resources/Generated/{itemData.itemName}_2D.prefab";
            GameObject prefab2D = AssetDatabase.LoadAssetAtPath<GameObject>(prefab2DPath);
            if (prefab2D != null)
            {
                state.childrenUI = prefab2D;
            }
        }

        // 6. Сохраняем
        string savePath = "Assets/Resources/Generated/";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string prefabPath = savePath + itemData.itemName + "_3D.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        // 7. Удаляем экземпляр
        DestroyImmediate(instance);

        // 8. Обновляем AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"3D-префаб создан: {prefabPath}");
    }
}