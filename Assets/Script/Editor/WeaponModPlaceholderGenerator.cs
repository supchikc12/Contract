using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Генератор заглушек для системы модификации оружия и разгрузки.
// Меню: Tools/Contract/Generate Weapon Mod Placeholders
public static class WeaponModPlaceholderGenerator
{
    private const string IconsFolder = "Assets/Textures/Placeholders";
    private const string ModDataFolder = "Assets/Data/Items/Mods";
    private const string ModPrefabFolder = "Assets/Prefabs/Items/Mods";
    private const string GridDataFolder = "Assets/Data/Grids";
    private const string RigPrefabRoot = "Assets/Prefabs/Items/Containers";
    private const string RigPrefabFolder = RigPrefabRoot + "/Razgruzka";

    private enum IconShape
    {
        Scope,
        Muzzle,
        Stock,
        Grip,
        Underbarrel,
        Side
    }

    [MenuItem("Tools/Contract/Generate Weapon Mod Placeholders")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets/Textures", "Placeholders");
        EnsureFolder("Assets/Data/Items", "Mods");
        EnsureFolder("Assets/Data", "Grids");
        EnsureFolder("Assets/Prefabs/Items", "Mods");

        CreateMod("Scope_Placeholder", "Прицел (заглушка)", WeaponModSlotType.Scope,
            new Color(0.25f, 0.55f, 0.95f), new Vector2Int(2, 2), IconShape.Scope);
        CreateMod("Muzzle_Placeholder", "Дульный тормоз (заглушка)", WeaponModSlotType.Muzzle,
            new Color(0.95f, 0.55f, 0.2f), new Vector2Int(1, 2), IconShape.Muzzle);
        CreateMod("Stock_Placeholder", "Приклад (заглушка)", WeaponModSlotType.Stock,
            new Color(0.35f, 0.75f, 0.4f), new Vector2Int(2, 2), IconShape.Stock);
        CreateMod("Grip_Placeholder", "Рукоятка (заглушка)", WeaponModSlotType.Grip,
            new Color(0.7f, 0.45f, 0.9f), new Vector2Int(1, 1), IconShape.Grip);
        CreateMod("Underbarrel_Placeholder", "Подствольник (заглушка)", WeaponModSlotType.Underbarrel,
            new Color(0.9f, 0.8f, 0.25f), new Vector2Int(1, 2), IconShape.Underbarrel);
        CreateMod("LaserLeft_Placeholder", "ЛЦУ слева (заглушка)", WeaponModSlotType.SideLeft,
            new Color(0.95f, 0.35f, 0.35f), new Vector2Int(1, 1), IconShape.Side);
        CreateMod("LaserRight_Placeholder", "ЛЦУ справа (заглушка)", WeaponModSlotType.SideRight,
            new Color(0.35f, 0.85f, 0.9f), new Vector2Int(1, 1), IconShape.Side);

        CreateRig();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Weapon mod placeholders generated.");
    }

    private static void CreateMod(string assetName, string displayName, WeaponModSlotType slot,
        Color color, Vector2Int size, IconShape shape)
    {
        string dataPath = ModDataFolder + "/" + assetName + ".asset";
        WeaponModData data = AssetDatabase.LoadAssetAtPath<WeaponModData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WeaponModData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.itemName = displayName;
        data.category = ItemCategory.WeaponMod;
        data.icon = CreateIcon(assetName, color, shape);
        data.size = size;
        data.maxStackSize = 1;
        data.maxDurability = 100f;
        data.slotType = slot;
        data.compatibleWeaponIds.Clear();
        data.modelPrefab2D = null;
        data.modelPrefab3D = null;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        GameObject prefab2D = Generate2DPrefab(data, assetName);
        GameObject prefab3D = Generate3DPrefab(data, prefab2D, assetName, color);

        data.modelPrefab2D = prefab2D;
        data.modelPrefab3D = prefab3D;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private static void CreateRig()
    {
        string layoutPath = GridDataFolder + "/RazgruzkaGrid.asset";
        ContainerGridData layout = AssetDatabase.LoadAssetAtPath<ContainerGridData>(layoutPath);
        if (layout == null)
        {
            layout = ScriptableObject.CreateInstance<ContainerGridData>();
            AssetDatabase.CreateAsset(layout, layoutPath);
        }

        layout.size = new Vector2Int(4, 4);
        layout.limitColumns = true;
        layout.maxColumns = 20;

        layout.pockets.Clear();
        AddPocket(layout, "Основной", 0, 0, 2, 2);
        AddPocket(layout, "Магазины", 0, 2, 1, 2);
        AddPocket(layout, "Магазины", 1, 2, 1, 2);
        AddPocket(layout, "Аптечка", 2, 0, 1, 1);
        AddPocket(layout, "Граната", 2, 1, 1, 1);
        AddPocket(layout, "Документы", 2, 2, 2, 1);
        AddPocket(layout, "Радио", 2, 3, 1, 1);
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();

        string dataPath = "Assets/Data/Items/Containers/Razgruzka.asset";
        RigData data = AssetDatabase.LoadAssetAtPath<RigData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<RigData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.itemName = "Разгрузка (заглушка)";
        data.category = ItemCategory.Rig;
        data.icon = CreateIcon("Razgruzka_Icon", new Color(0.45f, 0.6f, 0.45f), IconShape.Stock);
        data.size = new Vector2Int(4, 5);
        data.maxStackSize = 1;
        data.maxDurability = 100f;
        data.grid = layout;
        data.modelPrefab2D = null;
        data.modelPrefab3D = null;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        GameObject prefab2D = GenerateRig2DPrefab(data, "Razgruzka");
        GameObject prefab3D = Generate3DPrefab(data, prefab2D, "Razgruzka", new Color(0.45f, 0.6f, 0.45f), RigPrefabRoot);

        data.modelPrefab2D = prefab2D;
        data.modelPrefab3D = prefab3D;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        CreateRigSlot(data.icon);
    }

    private static void AddPocket(ContainerGridData layout, string pocketName, int row, int column, int rows, int columns)
    {
        ContainerGridData.Pocket pocket = new ContainerGridData.Pocket();
        pocket.name = pocketName;
        pocket.position = new Vector2Int(row, column);
        pocket.size = new Vector2Int(rows, columns);
        pocket.allowedTypes = ItemCategory.All;
        layout.pockets.Add(pocket);
    }

    private static GameObject Generate2DPrefab(ItemData data, string assetName)
    {
        string folder = ModPrefabFolder + "/" + assetName;
        EnsureFolder(ModPrefabFolder, assetName);
        string path = folder + "/" + assetName + "_2D.prefab";

        GameObject saved = ItemPrefabGenerator.Create2DPrefab(data, assetName + "_2D", path, false, true);
        return saved != null ? saved : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject Generate3DPrefab(ItemData data, GameObject prefab2D, string assetName, Color color, string outputFolder = null)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Templates/Item3DTemplate.prefab");
        if (template == null)
        {
            Debug.LogError("Item3DTemplate.prefab не найден");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
        instance.name = assetName + "_3D";
        instance.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = data;
            state.childrenUI = prefab2D;
        }

        MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = CreateMaterial(assetName, color);
            if (material != null) renderer.sharedMaterial = material;
        }

        string rootFolder = string.IsNullOrEmpty(outputFolder) ? ModPrefabFolder : outputFolder;
        string folder = rootFolder + "/" + assetName;
        EnsureFolder(rootFolder, assetName);
        string path = folder + "/" + assetName + "_3D.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);

        return saved != null ? saved : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject GenerateRig2DPrefab(ItemData data, string assetName)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Containers/KVAZIR/KVAZIR_2D.prefab");
        if (template == null)
        {
            Debug.LogError("KVAZIR_2D.prefab не найден");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
        instance.name = assetName + "_2D";

        ItemState state = instance.GetComponent<ItemState>();
        if (state != null)
        {
            state.data = data;
            state.childrenUI = null;
        }

        Image iconImage = instance.GetComponentInChildren<Image>();
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.preserveAspect = true;
        }

        Transform panel = instance.transform.Find("Conteyner");
        if (panel != null)
        {
            Transform titleTransform = panel.Find("Header/Title");
            if (titleTransform != null)
            {
                TMP_Text title = titleTransform.GetComponent<TMP_Text>();
                if (title != null) title.text = data.itemName;
            }

            InitializeGrid containerGrid = panel.GetComponentInChildren<InitializeGrid>(true);
            if (containerGrid != null)
            {
                containerGrid.IsContainerGrid = true;
                containerGrid.LimitColumns = true;
            }
        }

        RectTransform rootRect = instance.GetComponent<RectTransform>();
        if (rootRect != null) rootRect.sizeDelta = new Vector2(data.size.y * 50f, data.size.x * 50f);

        EnsureFolder(RigPrefabRoot, "Razgruzka");
        string path = RigPrefabFolder + "/" + assetName + "_2D.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);

        return saved != null ? saved : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void CreateRigSlot(Sprite icon)
    {
        string path = "Assets/Data/Equipment/Slots/RigSlotData.asset";
        EquipmentSlotData slot = AssetDatabase.LoadAssetAtPath<EquipmentSlotData>(path);
        if (slot == null)
        {
            slot = ScriptableObject.CreateInstance<EquipmentSlotData>();
            AssetDatabase.CreateAsset(slot, path);
        }

        slot.slotName = "Rig";
        slot.allowedCategories = ItemCategory.Rig;
        slot.defaultSprite = icon;
        slot.anchoredPosition = new Vector2(-277f, 324f);
        slot.size = new Vector2(220f, 250f);
        EditorUtility.SetDirty(slot);
        AssetDatabase.SaveAssets();
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) return null;

        EnsureFolder("Assets/Materials", "Placeholders");
        string path = "Assets/Materials/Placeholders/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Sprite CreateIcon(string name, Color color, IconShape shape)
    {
        EnsureFolder("Assets/Textures", "Placeholders");

        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color border = Color.white;
        Color symbol = new Color(0.08f, 0.09f, 0.11f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < 5 || y < 5 || x >= size - 5 || y >= size - 5;
                Color pixel = isBorder ? border : color;

                if (!isBorder && IsShapePixel(shape, x, y, size))
                    pixel = symbol;

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();

        string path = IconsFolder + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool IsShapePixel(IconShape shape, int x, int y, int size)
    {
        int cx = size / 2;
        int cy = size / 2;

        switch (shape)
        {
            case IconShape.Scope:
            {
                float dx = x - cx;
                float dy = y - cy;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = distance > 30f && distance < 42f;
                bool cross = (Mathf.Abs(dx) < 3f && Mathf.Abs(dy) < 42f) ||
                             (Mathf.Abs(dy) < 3f && Mathf.Abs(dx) < 42f);
                return ring || cross;
            }
            case IconShape.Muzzle:
                return (x > 22 && x < 106 && y > 52 && y < 76) ||
                       (x > 34 && x < 94 && y > 40 && y < 88 && (x % 16 < 4));
            case IconShape.Stock:
                return (x > 20 && x < 108 && y > 40 && y < 88) &&
                       !(x > 34 && x < 94 && y > 52 && y < 76);
            case IconShape.Grip:
                return x > 50 && x < 78 && y > 24 && y < 104;
            case IconShape.Underbarrel:
                return (x > 20 && x < 108 && y > 56 && y < 72) ||
                       (x > 52 && x < 76 && y > 40 && y < 88);
            case IconShape.Side:
            {
                float dx = x - cx;
                float dy = y - cy;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                return (distance > 28f && distance < 40f) || distance < 10f;
            }
        }

        return false;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
