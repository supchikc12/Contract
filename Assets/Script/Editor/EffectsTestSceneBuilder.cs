using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Сборка тестовой площадки эффектов: ассеты, зоны, предметы и БД для сохранения.
public static class EffectsTestSceneBuilder
{
    private const string EffectsRoot = "Assets/Data/Effects";
    private const string MaterialsRoot = "Assets/Materials/Testing";
    private const string ResourcesRoot = "Assets/Resources/Effects";
    private const string TestRootName = "TEST_Effects";

    [MenuItem("Tools/Contract/Build Effects Test Scene")]
    public static void Build()
    {
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Items/Weapons/AK-47/AK-47_icon.psd");
        GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Weapons/AK-47/AK-47_3D.prefab");

        EffectDefinition fracture = CreateEffect(
            "Test_FractureLeg", "Перелом ноги", EffectTargetMask.Legs,
            EffectVisibility.InspectTarget, EffectTag.Fracture | EffectTag.Debuff,
            0f, EffectDefinition.StackPolicy.Refresh, 1,
            new[] { Mod(StatType.MoveSpeed, StatModifier.Mode.Multiply, -0.2f) });

        EffectDefinition bleeding = CreateEffect(
            "Test_Bleeding", "Кровотечение", EffectTargetMask.AllBodyParts,
            EffectVisibility.InspectTarget, EffectTag.Bleeding | EffectTag.Debuff,
            15f, EffectDefinition.StackPolicy.Stack, 3,
            null, new[] { Periodic(DamageType.Bleeding, 1f, 1f, false, true) });

        EffectDefinition burning = CreateEffect(
            "Test_Burning", "Ожог", EffectTargetMask.AllBodyParts,
            EffectVisibility.VisibleOnApply, EffectTag.Burning | EffectTag.Thermal,
            8f, EffectDefinition.StackPolicy.Stack, 3,
            null, new[] { Periodic(DamageType.Thermal, 2f, 1f, false, true) });

        EffectDefinition poison = CreateEffect(
            "Test_Poison", "Отравление", EffectTargetMask.Character,
            EffectVisibility.VisibleOnApply, EffectTag.Poisoned | EffectTag.Chemical,
            20f, EffectDefinition.StackPolicy.Refresh, 1,
            null, new[] { Periodic(DamageType.Chemical, 1f, 1.5f, false, true) });

        EffectDefinition shock = CreateEffect(
            "Test_Shock", "Электрошок", EffectTargetMask.Character,
            EffectVisibility.VisibleOnApply, EffectTag.Shocked | EffectTag.Electrical,
            1f, EffectDefinition.StackPolicy.Refresh, 1,
            null, new[] { Periodic(DamageType.Electrical, 10f, 1f, false, true) });

        EffectDefinition cold = CreateEffect(
            "Test_Cold", "Холод", EffectTargetMask.Character,
            EffectVisibility.VisibleOnApply, EffectTag.Debuff | EffectTag.Thermal,
            10f, EffectDefinition.StackPolicy.Refresh, 1,
            new[]
            {
                Mod(StatType.MoveSpeed, StatModifier.Mode.Multiply, -0.1f),
                Mod(StatType.StaminaDrain, StatModifier.Mode.Add, 0.3f)
            });

        EffectDefinition thermalResist = CreateEffect(
            "Test_ThermalResist", "Терморезист", EffectTargetMask.Item,
            EffectVisibility.KnownFromItem, EffectTag.Buff | EffectTag.Thermal,
            0f, EffectDefinition.StackPolicy.Refresh, 1,
            new[] { Mod(StatType.ThermalResistance, StatModifier.Mode.Add, 0.4f) });

        EffectDefinition pain = CreateEffect(
            "Test_Pain", "Боль", EffectTargetMask.Character,
            EffectVisibility.InspectTarget, EffectTag.Pain | EffectTag.Debuff,
            30f, EffectDefinition.StackPolicy.Refresh, 1,
            new[] { Mod(StatType.MoveSpeed, StatModifier.Mode.Multiply, -0.05f) });

        EffectDefinition painkillerBuff = CreateEffect(
            "Test_PainkillerBuff", "Обезболивание", EffectTargetMask.Character,
            EffectVisibility.VisibleOnApply, EffectTag.Buff,
            20f, EffectDefinition.StackPolicy.Refresh, 1,
            new[] { Mod(StatType.MoveSpeed, StatModifier.Mode.Multiply, 0.1f) });

        EffectDefinition healing = CreateEffect(
            "Test_Healing", "Заживление", EffectTargetMask.AllBodyParts,
            EffectVisibility.VisibleOnApply, EffectTag.Buff,
            5f, EffectDefinition.StackPolicy.Refresh, 1,
            null, new[] { Periodic(DamageType.None, 3f, 1f, true, false) });

        EffectDefinition weaponDamage = CreateEffect(
            "Test_WeaponDamage", "Усиление оружия", EffectTargetMask.Weapon,
            EffectVisibility.VisibleOnApply, EffectTag.Buff,
            30f, EffectDefinition.StackPolicy.Refresh, 1,
            new[] { Mod(StatType.Damage, StatModifier.Mode.Multiply, 0.2f) });

        CreateConsumable(
            "Test_Bandage", "Бинт", icon, sourceModel, ConsumableTarget.BodyPart,
            EffectTag.Bleeding, new List<EffectDefinition>(), 2f, 1, 5);
        CreateConsumable(
            "Test_Painkiller", "Обезболивающее", icon, sourceModel, ConsumableTarget.Self,
            EffectTag.Pain, new List<EffectDefinition> { painkillerBuff }, 2f, 1, 3);
        CreateConsumable(
            "Test_Medkit", "Аптечка", icon, sourceModel, ConsumableTarget.BodyPart,
            EffectTag.Bleeding, new List<EffectDefinition> { healing }, 4f, 2, 1);

        CreateArmor(
            "Test_ThermalSuit", "Термокостюм", icon, sourceModel,
            EffectTargetMask.Torso | EffectTargetMask.Pelvis, 0.3f,
            new List<EffectDefinition> { thermalResist });

        CreateDatabase(new List<EffectDefinition>
        {
            fracture, bleeding, burning, poison, shock, cold,
            thermalResist, pain, painkillerBuff, healing, weaponDamage
        });

        BuildScene(fracture, bleeding, burning, poison, shock, cold);
        AssetDatabase.SaveAssets();
        Debug.Log("Effects test scene built.");
    }

    private static EffectDefinition CreateEffect(
        string name,
        string displayName,
        EffectTargetMask targets,
        EffectVisibility visibility,
        EffectTag tags,
        float duration,
        EffectDefinition.StackPolicy stackPolicy,
        int maxStacks,
        StatModifier[] modifiers,
        PeriodicEffect[] periodic = null)
    {
        string path = EffectsRoot + "/" + name + ".asset";
        ItemPrefabPaths.EnsureFolder(EffectsRoot);

        EffectDefinition effect = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<EffectDefinition>();
            AssetDatabase.CreateAsset(effect, path);
        }

        effect.id = name.ToLowerInvariant();
        effect.effectName = displayName;
        effect.description = displayName;
        effect.allowedTargets = targets;
        effect.visibility = visibility;
        effect.tags = tags;
        effect.duration = duration;
        effect.stackPolicy = stackPolicy;
        effect.maxStacks = maxStacks;
        effect.modifiers = new List<StatModifier>();
        if (modifiers != null) effect.modifiers.AddRange(modifiers);
        effect.periodicEffects = new List<PeriodicEffect>();
        if (periodic != null) effect.periodicEffects.AddRange(periodic);

        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static StatModifier Mod(StatType stat, StatModifier.Mode mode, float value)
    {
        StatModifier modifier = new StatModifier();
        modifier.stat = stat;
        modifier.mode = mode;
        modifier.value = value;
        return modifier;
    }

    private static PeriodicEffect Periodic(
        DamageType type, float amount, float interval, bool healing, bool canKill)
    {
        PeriodicEffect effect = new PeriodicEffect();
        effect.damageType = type;
        effect.amountPerTick = amount;
        effect.interval = interval;
        effect.healing = healing;
        effect.canKill = canKill;
        return effect;
    }

    private static void CreateConsumable(
        string name,
        string displayName,
        Sprite icon,
        GameObject sourceModel,
        ConsumableTarget target,
        EffectTag cureTags,
        List<EffectDefinition> effects,
        float useTime,
        int uses,
        int maxStack)
    {
        string path = ItemPrefabPaths.GetDataPath(typeof(ConsumableData), name);
        ItemPrefabPaths.EnsureFolder(ItemPrefabPaths.GetDataFolder(typeof(ConsumableData)));

        ConsumableData data = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ConsumableData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = name.ToLowerInvariant();
        data.itemName = displayName;
        data.prefabName = name;
        data.category = ItemCategory.Medicine;
        data.size = new Vector2Int(1, 1);
        data.weight = 0.1f;
        data.maxStackSize = maxStack;
        data.maxDurability = 100f;
        data.icon = icon;
        data.sourceModel3D = sourceModel;
        data.consumableType = ConsumableType.Medicine;
        data.target = target;
        data.cureTags = cureTags;
        data.effects = new List<EffectDefinition>(effects);
        data.useTime = useTime;
        data.uses = uses;

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        ItemPrefabFactory.Generate(data, PrefabOverwriteMode.Overwrite);
    }

    private static void CreateArmor(
        string name,
        string displayName,
        Sprite icon,
        GameObject sourceModel,
        EffectTargetMask coverage,
        float damageReduction,
        List<EffectDefinition> effects)
    {
        string path = ItemPrefabPaths.GetDataPath(typeof(ArmorData), name);
        ItemPrefabPaths.EnsureFolder(ItemPrefabPaths.GetDataFolder(typeof(ArmorData)));

        ArmorData data = AssetDatabase.LoadAssetAtPath<ArmorData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ArmorData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = name.ToLowerInvariant();
        data.itemName = displayName;
        data.prefabName = name;
        data.category = ItemCategory.Armor;
        data.size = new Vector2Int(4, 4);
        data.weight = 2f;
        data.maxStackSize = 1;
        data.maxDurability = 100f;
        data.icon = icon;
        data.sourceModel3D = sourceModel;
        data.armorSlot = ArmorSlot.Torso;
        data.armorClass = 2;
        data.damageReduction = damageReduction;
        data.coveredParts = coverage;
        data.effects = new List<EffectDefinition>(effects);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        ItemPrefabFactory.Generate(data, PrefabOverwriteMode.Overwrite);
    }

    private static void CreateDatabase(List<EffectDefinition> effects)
    {
        ItemPrefabPaths.EnsureFolder(ResourcesRoot);
        string path = ResourcesRoot + "/EffectDatabase.asset";
        EffectDatabase database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(path);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<EffectDatabase>();
            AssetDatabase.CreateAsset(database, path);
        }

        database.effects = new List<EffectDefinition>(effects);
        EditorUtility.SetDirty(database);
    }

    private static void BuildScene(
        EffectDefinition fracture,
        EffectDefinition bleeding,
        EffectDefinition burning,
        EffectDefinition poison,
        EffectDefinition shock,
        EffectDefinition cold)
    {
        GameObject oldRoot = GameObject.Find(TestRootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        GameObject root = new GameObject(TestRootName);
        root.transform.position = Vector3.zero;

        Material fire = CreateMaterial("TestZoneFire", new Color(1f, 0.25f, 0.05f));
        Material poisonMat = CreateMaterial("TestZonePoison", new Color(0.2f, 0.9f, 0.2f));
        Material coldMat = CreateMaterial("TestZoneCold", new Color(0.2f, 0.55f, 1f));
        Material electricMat = CreateMaterial("TestZoneElectric", new Color(0.8f, 0.2f, 1f));
        Material anomalyMat = CreateMaterial("TestZoneAnomaly", new Color(1f, 0.1f, 0.55f));

        CreateZone(root.transform, "Zone_Fire_Torso", new Vector3(10f, 4.1f, 20f),
            new Vector3(2.5f, 1.4f, 2.5f), burning, EffectZone.TargetMode.BodyPart,
            false, new[] { BodyPartType.Torso, BodyPartType.Pelvis }, fire, "ОГОНЬ / ТОРС");

        CreateZone(root.transform, "Zone_Fire_Legs", new Vector3(10f, 2.6f, 20f),
            new Vector3(3f, 1.5f, 3f), burning, EffectZone.TargetMode.BodyPart,
            false, new[] { BodyPartType.LeftThigh, BodyPartType.RightThigh },
            fire, "ОГОНЬ / НОГИ");

        CreateZone(root.transform, "Zone_Poison", new Vector3(14f, 3.2f, 20f),
            new Vector3(3f, 2.5f, 3f), poison, EffectZone.TargetMode.Character,
            false, null, poisonMat, "ЯД");

        CreateZone(root.transform, "Zone_Cold", new Vector3(18f, 3.2f, 20f),
            new Vector3(3f, 2.5f, 3f), cold, EffectZone.TargetMode.Character,
            true, null, coldMat, "ХОЛОД");

        CreateZone(root.transform, "Zone_Electric", new Vector3(22f, 3.2f, 20f),
            new Vector3(3f, 2.5f, 3f), shock, EffectZone.TargetMode.Character,
            true, null, electricMat, "ЭЛЕКТРО");

        CreateZone(root.transform, "Anomaly_Fracture_Legs", new Vector3(10f, 2.6f, 24f),
            new Vector3(3f, 1.5f, 3f), fracture, EffectZone.TargetMode.BodyPart,
            false, new[] { BodyPartType.LeftThigh, BodyPartType.RightThigh },
            anomalyMat, "АНОМАЛИЯ / ПЕРЕЛОМ НОГ");

        CreateZone(root.transform, "Trap_Bleeding_Legs", new Vector3(14f, 2.6f, 24f),
            new Vector3(3f, 1.5f, 3f), bleeding, EffectZone.TargetMode.BodyPart,
            false, new[] { BodyPartType.LeftThigh, BodyPartType.RightThigh },
            fire, "ЛОВУШКА / КРОВОТЕЧЕНИЕ");

        SpawnItem(root.transform, "Test_Bandage", new Vector3(18f, 3.6f, 25f));
        SpawnItem(root.transform, "Test_Painkiller", new Vector3(20f, 3.6f, 25f));
        SpawnItem(root.transform, "Test_Medkit", new Vector3(22f, 3.6f, 25f));
        SpawnItem(root.transform, "Test_ThermalSuit", new Vector3(24f, 3.6f, 25f));

        CreateLabel(root.transform, "TEST EFFECTS", new Vector3(16f, 6.5f, 22f), 8f, Color.white);
        CreateLabel(root.transform, "F8 сохранить / F9 загрузить эффекты", new Vector3(16f, 5.8f, 22f), 3.5f, Color.white);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void CreateZone(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 size,
        EffectDefinition effect,
        EffectZone.TargetMode mode,
        bool removeOnExit,
        BodyPartType[] allowedParts,
        Material material,
        string label)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = position;

        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        Rigidbody body = zone.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        EffectZone effectZone = zone.AddComponent<EffectZone>();
        effectZone.Configure(effect, mode, removeOnExit, allowedParts);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.name = "Visual";
        visual.transform.SetParent(zone.transform, false);
        visual.transform.localScale = size * 0.96f;
        visual.GetComponent<MeshRenderer>().sharedMaterial = material;

        CreateLabel(zone.transform, label, new Vector3(0f, size.y * 0.75f, 0f), 3f, Color.white);
    }

    private static void SpawnItem(Transform parent, string itemName, Vector3 position)
    {
        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPrefabPaths.GetDataPath(
            typeof(ConsumableData), itemName));
        if (data == null)
            data = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPrefabPaths.GetDataPath(
                typeof(ArmorData), itemName));

        if (data == null)
        {
            Debug.LogWarning("Не найден ItemData для " + itemName);
            return;
        }

        string path = ItemPrefabPaths.Get3DPath(data);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Не найден 3D-префаб: " + path);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.name = itemName + "_3D";
    }

    private static void CreateLabel(Transform parent, string text, Vector3 localPosition, float size, Color color)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = localPosition;
        labelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(10f, 2f);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        ItemPrefabPaths.EnsureFolder(MaterialsRoot);
        string path = MaterialsRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 0.35f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
