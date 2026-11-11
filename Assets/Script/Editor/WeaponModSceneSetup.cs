using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Настройка сцены для тестирования модификаций:
// добавляет окно модификации, слот разгрузки и заглушки предметов.
// Меню: Tools/Contract/Setup Weapon Mod Scene
public static class WeaponModSceneSetup
{
    private const string RigSlotPath = "Assets/Data/Equipment/Slots/RigSlotData.asset";

    [MenuItem("Tools/Contract/Setup Weapon Mod Scene")]
    public static void Setup()
    {
        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        if (inventory == null)
        {
            Debug.LogError("InventoryManager не найден в открытой сцене");
            return;
        }

        if (inventory.GetComponent<WeaponModdingUI>() == null)
            inventory.gameObject.AddComponent<WeaponModdingUI>();

        AddRigSlot();

        GameObject oldRoot = GameObject.Find("WeaponMods_Placeholders");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        GameObject root = new GameObject("WeaponMods_Placeholders");

        Transform player = GameObject.Find("Player") != null ? GameObject.Find("Player").transform : null;
        Vector3 basePosition = player != null
            ? player.position + player.forward * 2f
            : new Vector3(5f, 1f, 16f);
        basePosition.y = player != null ? player.position.y : 1f;

        Spawn("Assets/Prefabs/Items/Weapons/AK-47/AK-47_3D.prefab", root.transform,
            basePosition + new Vector3(0f, 0.6f, 0f), "AK-47_3D_Placeholder");

        Spawn("Assets/Prefabs/Items/Containers/Razgruzka/Razgruzka_3D.prefab", root.transform,
            basePosition + new Vector3(2f, 0.6f, 0f), "Razgruzka_3D_Placeholder");

        string[] modPaths = new string[]
        {
            "Assets/Prefabs/Items/Mods/Scope_Placeholder/Scope_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/Muzzle_Placeholder/Muzzle_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/Stock_Placeholder/Stock_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/Grip_Placeholder/Grip_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/Underbarrel_Placeholder/Underbarrel_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/LaserLeft_Placeholder/LaserLeft_Placeholder_3D.prefab",
            "Assets/Prefabs/Items/Mods/LaserRight_Placeholder/LaserRight_Placeholder_3D.prefab"
        };

        for (int i = 0; i < modPaths.Length; i++)
        {
            Spawn(modPaths[i], root.transform,
                basePosition + new Vector3(-1.5f + i * 1.1f, 0.6f, 2f),
                "Mod_Placeholder_" + i);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("Weapon mod scene setup complete. Test items spawned near the player.");
    }

    private static void AddRigSlot()
    {
        EquipmentSlotsManager manager = Object.FindAnyObjectByType<EquipmentSlotsManager>();
        EquipmentSlotData rigSlot = AssetDatabase.LoadAssetAtPath<EquipmentSlotData>(RigSlotPath);

        if (manager == null || rigSlot == null)
        {
            Debug.LogWarning("Не удалось добавить слот разгрузки: нет EquipmentSlotsManager или RigSlotData");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty slotDatas = serializedManager.FindProperty("slotDatas");
        if (slotDatas == null) return;

        for (int i = 0; i < slotDatas.arraySize; i++)
        {
            if (slotDatas.GetArrayElementAtIndex(i).objectReferenceValue == rigSlot)
                return;
        }

        slotDatas.arraySize++;
        slotDatas.GetArrayElementAtIndex(slotDatas.arraySize - 1).objectReferenceValue = rigSlot;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void Spawn(string prefabPath, Transform parent, Vector3 position, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Не найден префаб: " + prefabPath);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
    }
}
