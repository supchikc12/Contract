using UnityEditor;
using UnityEngine;

// Пути и правила именования сгенерированных ассетов.
public static class ItemPrefabPaths
{
    public const string TemplatesRoot = "Assets/Prefabs/Items/Templates";
    public const string ItemTemplate = TemplatesRoot + "/ItemTemplate.prefab";
    public const string Item3DTemplate = TemplatesRoot + "/Item3DTemplate.prefab";
    public const string ContainerTemplate = TemplatesRoot + "/ContainerTemplate.prefab";

    public const string ItemsRoot = "Assets/Prefabs/Items";
    public const string DataRoot = "Assets/Data/Items";
    public const string GridRoot = "Assets/Data/Grids";

    public static string GetCategoryFolder(ItemData data)
    {
        return data != null ? GetCategoryFolder(data.GetType()) : "Misc";
    }

    public static string GetCategoryFolder(System.Type type)
    {
        if (typeof(WeaponData).IsAssignableFrom(type)) return "Weapons";
        if (typeof(WeaponModData).IsAssignableFrom(type)) return "Mods";
        if (typeof(ContainerData).IsAssignableFrom(type)) return "Containers";
        if (typeof(ArmorData).IsAssignableFrom(type)) return "Equipment";
        if (typeof(ClothingData).IsAssignableFrom(type)) return "Equipment";
        if (typeof(ConsumableData).IsAssignableFrom(type)) return "Consumables";
        if (typeof(AmmoData).IsAssignableFrom(type)) return "Ammo";
        return "Misc";
    }

    public static string GetDataFolder(ItemData data)
    {
        return GetDataFolder(data.GetType());
    }

    public static string GetDataFolder(System.Type type)
    {
        return DataRoot + "/" + GetCategoryFolder(type);
    }

    public static string GetDataPath(System.Type type, string itemName)
    {
        return GetDataFolder(type) + "/" + SanitizeFileName(itemName) + ".asset";
    }

    public static string GetPrefabName(ItemData data)
    {
        if (data == null) return "Item";
        return string.IsNullOrWhiteSpace(data.prefabName) ? data.itemName : data.prefabName;
    }

    public static string GetPrefabFolder(ItemData data)
    {
        return ItemsRoot + "/" + GetCategoryFolder(data) + "/" + SanitizeFileName(GetPrefabName(data));
    }

    public static string Get2DPath(ItemData data)
    {
        return GetPrefabFolder(data) + "/" + SanitizeFileName(GetPrefabName(data)) + "_2D.prefab";
    }

    public static string Get3DPath(ItemData data)
    {
        return GetPrefabFolder(data) + "/" + SanitizeFileName(GetPrefabName(data)) + "_3D.prefab";
    }

    public static string GetGridPath(string itemName)
    {
        return GridRoot + "/" + SanitizeFileName(itemName) + "Grid.asset";
    }

    public static string SanitizeFileName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Item";

        string result = value.Trim();
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            result = result.Replace(c, '_');

        return string.IsNullOrEmpty(result) ? "Item" : result;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
