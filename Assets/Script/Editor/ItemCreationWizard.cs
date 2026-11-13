using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum ItemCreationType
{
    Weapon,
    Armor,
    Clothing,
    Container,
    Backpack,
    Rig,
    Consumable,
    Ammo,
    WeaponMod,
    Generic
}

// Мастер создания предмета: данные, сетка контейнера и prefabs в одном окне.
public class ItemCreationWizard : EditorWindow
{
    private ItemCreationType type = ItemCreationType.Weapon;
    private string itemName = "Новый предмет";
    private string prefabName = "";
    private string id = "";
    private ItemCategory category = ItemCategory.Weapon;
    private Vector2Int size = new Vector2Int(5, 10);
    private float weight = 1f;
    private int maxStackSize = 1;
    private float maxDurability = 100f;
    private Sprite icon;
    private GameObject sourceModel3D;

    private PrefabOverwriteMode overwriteMode = PrefabOverwriteMode.Overwrite;

    [Header("Контейнер")]
    private Vector2Int containerSize = new Vector2Int(10, 10);
    private ItemCategory gridAllowedCategories = ItemCategory.All;
    private ContainerGridData gridTemplate;

    [Header("Карманы разгрузки")]
    private int pocketCount = 4;
    private List<Vector2Int> pocketSizes = new List<Vector2Int>
    {
        new Vector2Int(3, 3),
        new Vector2Int(2, 2),
        new Vector2Int(2, 1),
        new Vector2Int(2, 3)
    };

    private List<Vector2Int> pocketPositions = new List<Vector2Int>
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 3),
        new Vector2Int(3, 0),
        new Vector2Int(3, 1)
    };

    private List<Vector2Int> pocketPixelPositions = new List<Vector2Int>
    {
        new Vector2Int(0, 0),
        new Vector2Int(150, 0),
        new Vector2Int(0, 150),
        new Vector2Int(50, 150)
    };

    private List<bool> pocketFoldouts = new List<bool> { true, true, true, true };

    [Header("Оружие")]
    private WeaponClass weaponClass = WeaponClass.Primary;
    private float damage = 30f;
    private float fireRate = 600f;
    private string caliber = "";
    private float recoil = 1f;
    private float accuracy = 1f;

    [Header("Броня")]
    private ArmorSlot armorSlot = ArmorSlot.Torso;
    private int armorClass = 1;
    private float damageReduction;

    [Header("Одежда")]
    private ClothingSlot clothingSlot = ClothingSlot.Torso;
    private float insulation;

    [Header("Расходуемое")]
    private ConsumableType consumableType = ConsumableType.Food;
    private float useTime = 1f;
    private int uses = 1;

    [Header("Патроны")]
    private string ammoCaliber = "";
    private string bulletType = "FMJ";
    private float damageMultiplier = 1f;
    private float penetration = 1f;

    [Header("Мод")]
    private WeaponModSlotType modSlotType = WeaponModSlotType.None;
    private string compatibleWeaponIds = "";

    private string status;
    private Vector2 scroll;

    private bool editMode;
    private ItemData editingData;
    private ContainerGridData editingGrid;

    private enum WizardMode
    {
        Item,
        Effect
    }

    private WizardMode wizardMode = WizardMode.Item;

    private EffectDefinition editingEffect;
    private string effectName = "Новый эффект";
    private string effectId = "";
    private string effectDescription = "";
    private Sprite effectIcon;
    private float effectDuration = 10f;
    private EffectDefinition.StackPolicy effectStackPolicy = EffectDefinition.StackPolicy.Refresh;
    private int effectMaxStacks = 1;
    private List<PeriodicEffect> effectPeriodicEffects = new List<PeriodicEffect>();
    private EffectTargetMask effectAllowedTargets = EffectTargetMask.Character;
    private EffectVisibility effectVisibility = EffectVisibility.InspectTarget;
    private EffectTag effectTags = EffectTag.None;
    private List<StatModifier> effectModifiers = new List<StatModifier>
    {
        new StatModifier
        {
            stat = StatType.MoveSpeed,
            mode = StatModifier.Mode.Multiply,
            value = 0.25f
        }
    };

    private List<EffectDefinition> consumableEffects = new List<EffectDefinition>();
    private List<EffectDefinition> equippableEffects = new List<EffectDefinition>();

    private const string EffectRoot = "Assets/Data/Effects";

    [MenuItem("Tools/Contract/Мастер создания предмета")]
    public static void OpenFromMenu()
    {
        OpenWindow();
    }

    [MenuItem("Tools/Contract/Мастер эффектов")]
    public static void OpenEffectFromMenu()
    {
        OpenEffectCreation();
    }

    public static void OpenWindow()
    {
        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.wizardMode = WizardMode.Item;
        window.editMode = false;
        window.editingData = null;
        window.editingGrid = null;
        window.ResetEffectFields();
        window.Show();
    }

    public static void OpenForEdit(ItemData data)
    {
        if (data == null) return;

        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.wizardMode = WizardMode.Item;
        window.editMode = true;
        window.editingData = data;
        window.LoadFromData(data);
        window.Show();
        window.Repaint();
    }

    public static void OpenEffectCreation()
    {
        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.wizardMode = WizardMode.Effect;
        window.ResetEffectFields();
        window.Show();
        window.Repaint();
    }

    public static void OpenForEffectEdit(EffectDefinition effect)
    {
        if (effect == null) return;

        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.wizardMode = WizardMode.Effect;
        window.LoadEffectFromAsset(effect);
        window.Show();
        window.Repaint();
    }

    private void OnEnable()
    {
        ApplyDefaults();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        WizardMode newMode = (WizardMode)GUILayout.Toolbar(
            (int)wizardMode,
            new string[] { "Предмет", "Эффект" });
        if (newMode != wizardMode)
        {
            wizardMode = newMode;
            status = null;
        }

        EditorGUILayout.Space(4);

        if (wizardMode == WizardMode.Effect)
        {
            DrawEffectWizard();
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField(editMode ? "Редактирование предмета" : "Создание предмета", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        GUI.enabled = !editMode;
        ItemCreationType newType = (ItemCreationType)EditorGUILayout.EnumPopup("Тип", type);
        GUI.enabled = true;
        if (!editMode && newType != type)
        {
            type = newType;
            ApplyDefaults();
        }

        itemName = EditorGUILayout.TextField("Имя", itemName);
        prefabName = EditorGUILayout.TextField("Имя prefabs", prefabName);
        EditorGUILayout.BeginHorizontal();
        id = EditorGUILayout.TextField("ID", id);
        if (GUILayout.Button("Сгенерировать", GUILayout.Width(120f)))
            id = Slugify(itemName);
        EditorGUILayout.EndHorizontal();

        category = (ItemCategory)EditorGUILayout.EnumFlagsField("Категория", category);
        size = EditorGUILayout.Vector2IntField("Размер (строки, столбцы)", size);
        weight = EditorGUILayout.FloatField("Вес", weight);
        maxStackSize = EditorGUILayout.IntField("Макс. стак", maxStackSize);
        maxDurability = EditorGUILayout.FloatField("Прочность", maxDurability);

        icon = (Sprite)EditorGUILayout.ObjectField("Иконка", icon, typeof(Sprite), false);
        sourceModel3D = (GameObject)EditorGUILayout.ObjectField("Исходная 3D-модель", sourceModel3D, typeof(GameObject), false);

        DrawTypeFields();
        DrawContainerFields();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Куда сохранять", EditorStyles.boldLabel);
        if (editMode && editingData != null)
            EditorGUILayout.LabelField("Данные", AssetDatabase.GetAssetPath(editingData));
        else
            EditorGUILayout.LabelField("Данные", ItemPrefabPaths.GetDataPath(GetDataType(), itemName));
        EditorGUILayout.LabelField("Prefabs", ItemPrefabPaths.ItemsRoot + "/" + ItemPrefabPaths.GetCategoryFolder(GetDataType()) + "/" + ItemPrefabPaths.SanitizeFileName(string.IsNullOrWhiteSpace(prefabName) ? itemName : prefabName));

        if (editMode)
            EditorGUILayout.HelpBox("Существующий ItemData обновляется на месте.", MessageType.Info);
        else
            overwriteMode = (PrefabOverwriteMode)EditorGUILayout.EnumPopup("Если файлы уже есть", overwriteMode);

        EditorGUILayout.Space(8);
        foreach (string error in ValidateWizard())
            EditorGUILayout.HelpBox(error, MessageType.Warning);

        if (GUILayout.Button(editMode ? "Сохранить изменения" : "Создать предмет", GUILayout.Height(36)))
            CreateItem();

        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void DrawTypeFields()
    {
        switch (type)
        {
            case ItemCreationType.Weapon:
                weaponClass = (WeaponClass)EditorGUILayout.EnumPopup("Класс оружия", weaponClass);
                damage = EditorGUILayout.FloatField("Урон", damage);
                fireRate = EditorGUILayout.FloatField("Темп (выстр/мин)", fireRate);
                caliber = EditorGUILayout.TextField("Калибр", caliber);
                recoil = EditorGUILayout.FloatField("Отдача", recoil);
                accuracy = EditorGUILayout.FloatField("Точность", accuracy);
                break;

            case ItemCreationType.Armor:
                armorSlot = (ArmorSlot)EditorGUILayout.EnumPopup("Слот брони", armorSlot);
                armorClass = EditorGUILayout.IntField("Класс брони", armorClass);
                damageReduction = EditorGUILayout.Slider("Снижение урона", damageReduction, 0f, 1f);
                break;

            case ItemCreationType.Clothing:
                clothingSlot = (ClothingSlot)EditorGUILayout.EnumPopup("Слот одежды", clothingSlot);
                insulation = EditorGUILayout.FloatField("Утеплитель", insulation);
                break;

            case ItemCreationType.Consumable:
                consumableType = (ConsumableType)EditorGUILayout.EnumPopup("Тип", consumableType);
                useTime = EditorGUILayout.FloatField("Время использования", useTime);
                uses = EditorGUILayout.IntField("Количество применений", uses);
                DrawConsumableEffects();
                break;

            case ItemCreationType.Ammo:
                ammoCaliber = EditorGUILayout.TextField("Калибр", ammoCaliber);
                bulletType = EditorGUILayout.TextField("Тип пули", bulletType);
                damageMultiplier = EditorGUILayout.FloatField("Множитель урона", damageMultiplier);
                penetration = EditorGUILayout.FloatField("Пробитие", penetration);
                break;

            case ItemCreationType.WeaponMod:
                modSlotType = (WeaponModSlotType)EditorGUILayout.EnumPopup("Слот мода", modSlotType);
                compatibleWeaponIds = EditorGUILayout.TextField("ID оружия (через запятую)", compatibleWeaponIds);
                break;
        }

        if (IsEquippableType(type))
            DrawEquippableEffects();
    }

    private static bool IsEquippableType(ItemCreationType itemType)
    {
        return itemType == ItemCreationType.Weapon
            || itemType == ItemCreationType.Armor
            || itemType == ItemCreationType.Clothing
            || itemType == ItemCreationType.Container
            || itemType == ItemCreationType.Backpack
            || itemType == ItemCreationType.Rig;
    }

    private void DrawEquippableEffects()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Эффекты при экипировке", EditorStyles.boldLabel);

        if (equippableEffects == null)
            equippableEffects = new List<EffectDefinition>();

        for (int i = 0; i < equippableEffects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            equippableEffects[i] = (EffectDefinition)EditorGUILayout.ObjectField(
                equippableEffects[i], typeof(EffectDefinition), false);
            if (GUILayout.Button("Удалить", GUILayout.Width(80f)))
            {
                equippableEffects.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (equippableEffects.Count == 0)
            EditorGUILayout.HelpBox("Эффекты при экипировке не назначены.", MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Добавить эффект"))
            equippableEffects.Add(null);
        if (GUILayout.Button("Создать новый эффект"))
            OpenEffectCreation();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawConsumableEffects()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Эффекты", EditorStyles.boldLabel);

        if (consumableEffects == null)
            consumableEffects = new List<EffectDefinition>();

        for (int i = 0; i < consumableEffects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            consumableEffects[i] = (EffectDefinition)EditorGUILayout.ObjectField(
                consumableEffects[i], typeof(EffectDefinition), false);
            if (GUILayout.Button("Удалить", GUILayout.Width(80f)))
            {
                consumableEffects.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (consumableEffects.Count == 0)
            EditorGUILayout.HelpBox("Эффекты пока не назначены.", MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Добавить эффект"))
            consumableEffects.Add(null);
        if (GUILayout.Button("Создать новый эффект"))
            OpenEffectCreation();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEffectWizard()
    {
        EditorGUILayout.LabelField(
            editingEffect != null ? "Редактирование эффекта" : "Создание эффекта",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        bool resetRequested;
        EditorGUILayout.BeginHorizontal();
        EffectDefinition picked = (EffectDefinition)EditorGUILayout.ObjectField(
            "Редактировать эффект", editingEffect, typeof(EffectDefinition), false);
        resetRequested = GUILayout.Button("Новый эффект", GUILayout.Width(120f));
        EditorGUILayout.EndHorizontal();

        if (resetRequested)
        {
            ResetEffectFields();
        }
        else if (picked != editingEffect)
        {
            if (picked != null)
                LoadEffectFromAsset(picked);
            else
                ResetEffectFields();
        }

        effectName = EditorGUILayout.TextField("Имя", effectName);

        EditorGUILayout.BeginHorizontal();
        effectId = EditorGUILayout.TextField("ID", effectId);
        if (GUILayout.Button("Сгенерировать", GUILayout.Width(120f)))
            effectId = Slugify(effectName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Описание");
        effectDescription = EditorGUILayout.TextArea(effectDescription, GUILayout.Height(56f));
        effectIcon = (Sprite)EditorGUILayout.ObjectField("Иконка", effectIcon, typeof(Sprite), false);

        effectDuration = Mathf.Max(0f, EditorGUILayout.FloatField(
            "Длительность, сек (0 = постоянно)", effectDuration));
        effectStackPolicy = (EffectDefinition.StackPolicy)EditorGUILayout.EnumPopup(
            "Политика стаков", effectStackPolicy);
        effectMaxStacks = Mathf.Max(1, EditorGUILayout.IntField("Макс. стаков", effectMaxStacks));

        effectAllowedTargets = (EffectTargetMask)EditorGUILayout.EnumFlagsField(
            "Допустимые цели", effectAllowedTargets);
        effectVisibility = (EffectVisibility)EditorGUILayout.EnumPopup(
            "Видимость", effectVisibility);
        effectTags = (EffectTag)EditorGUILayout.EnumFlagsField(
            "Теги", effectTags);

        if ((effectAllowedTargets & EffectTargetMask.Legs) != 0)
            EditorGUILayout.HelpBox("Выбраны ноги: эффект можно повесить только на их части.", MessageType.None);

        DrawEffectModifiers();
        DrawPeriodicEffects();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Куда сохранять", EditorStyles.boldLabel);
        if (editingEffect != null)
            EditorGUILayout.LabelField("Данные", AssetDatabase.GetAssetPath(editingEffect));
        else
            EditorGUILayout.LabelField("Данные", GetEffectPath());

        if (editingEffect == null)
            overwriteMode = (PrefabOverwriteMode)EditorGUILayout.EnumPopup("Если ассет уже есть", overwriteMode);
        else
            EditorGUILayout.HelpBox("Существующий EffectDefinition обновляется на месте.", MessageType.Info);

        if (effectModifiers == null || effectModifiers.Count == 0)
            EditorGUILayout.HelpBox("Без модификаторов эффект ничего не меняет.", MessageType.Info);

        EditorGUILayout.Space(8);
        foreach (string error in ValidateEffect())
            EditorGUILayout.HelpBox(error, MessageType.Warning);

        if (GUILayout.Button(
            editingEffect != null ? "Сохранить изменения" : "Создать эффект",
            GUILayout.Height(36f)))
        {
            CreateEffect();
        }

        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private void DrawEffectModifiers()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Модификаторы", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Add - плоская прибавка. Multiply - доля: 0.25 = +25%.",
            MessageType.None);

        if (effectModifiers == null)
            effectModifiers = new List<StatModifier>();

        for (int i = 0; i < effectModifiers.Count; i++)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            bool remove;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Модификатор " + (i + 1), EditorStyles.boldLabel);
            remove = GUILayout.Button("Удалить", GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            if (remove)
            {
                effectModifiers.RemoveAt(i);
                i--;
                EditorGUILayout.EndVertical();
                continue;
            }

            StatModifier modifier = effectModifiers[i];
            modifier.stat = (StatType)EditorGUILayout.EnumPopup("Стат", modifier.stat);
            modifier.mode = (StatModifier.Mode)EditorGUILayout.EnumPopup("Режим", modifier.mode);
            modifier.value = EditorGUILayout.FloatField("Значение", modifier.value);
            effectModifiers[i] = modifier;

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Добавить модификатор"))
        {
            effectModifiers.Add(new StatModifier
            {
                stat = StatType.MoveSpeed,
                mode = StatModifier.Mode.Multiply,
                value = 0f
            });
        }
    }

    private void DrawPeriodicEffects()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Периодический эффект", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Кровотечение: Bleeding, 1 HP каждые 1 сек. canKill=false оставляет минимум 1 HP. " +
            "healing=true лечит вместо урона.",
            MessageType.None);

        if (effectPeriodicEffects == null)
            effectPeriodicEffects = new List<PeriodicEffect>();

        for (int i = 0; i < effectPeriodicEffects.Count; i++)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            bool remove;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Тик " + (i + 1), EditorStyles.boldLabel);
            remove = GUILayout.Button("Удалить", GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            if (remove)
            {
                effectPeriodicEffects.RemoveAt(i);
                i--;
                EditorGUILayout.EndVertical();
                continue;
            }

            PeriodicEffect periodic = effectPeriodicEffects[i];
            periodic.damageType = (DamageType)EditorGUILayout.EnumPopup(
                "Тип урона", periodic.damageType);
            periodic.amountPerTick = Mathf.Max(0f, EditorGUILayout.FloatField(
                "Количество за тик", periodic.amountPerTick));
            periodic.interval = Mathf.Max(0.1f, EditorGUILayout.FloatField(
                "Интервал, сек", periodic.interval));
            periodic.healing = EditorGUILayout.Toggle("Лечение", periodic.healing);
            periodic.canKill = EditorGUILayout.Toggle("Может убить", periodic.canKill);
            effectPeriodicEffects[i] = periodic;

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Добавить периодический эффект"))
        {
            effectPeriodicEffects.Add(new PeriodicEffect
            {
                damageType = DamageType.Bleeding,
                amountPerTick = 1f,
                interval = 1f,
                canKill = true
            });
        }
    }

    private void ResetEffectFields()
    {
        editingEffect = null;
        effectName = "Новый эффект";
        effectId = "";
        effectDescription = "";
        effectIcon = null;
        effectDuration = 10f;
        effectStackPolicy = EffectDefinition.StackPolicy.Refresh;
        effectMaxStacks = 1;
        effectAllowedTargets = EffectTargetMask.Character;
        effectVisibility = EffectVisibility.InspectTarget;
        effectTags = EffectTag.None;
        effectPeriodicEffects = new List<PeriodicEffect>();
        effectModifiers = new List<StatModifier>
        {
            new StatModifier
            {
                stat = StatType.MoveSpeed,
                mode = StatModifier.Mode.Multiply,
                value = 0.25f
            }
        };
        status = null;
    }

    private void LoadEffectFromAsset(EffectDefinition effect)
    {
        editingEffect = effect;
        effectName = effect.effectName;
        effectId = effect.id;
        effectDescription = effect.description;
        effectIcon = effect.icon;
        effectDuration = Mathf.Max(0f, effect.duration);
        effectStackPolicy = effect.stackPolicy;
        effectMaxStacks = Mathf.Max(1, effect.maxStacks);
        effectAllowedTargets = effect.allowedTargets;
        effectVisibility = effect.visibility;
        effectTags = effect.tags;
        effectPeriodicEffects = effect.periodicEffects != null
            ? new List<PeriodicEffect>(effect.periodicEffects)
            : new List<PeriodicEffect>();
        effectModifiers = effect.modifiers != null
            ? new List<StatModifier>(effect.modifiers)
            : new List<StatModifier>();
        status = null;
    }

    private string GetEffectPath()
    {
        string fileName = string.IsNullOrWhiteSpace(effectName) ? effectId : effectName;
        return EffectRoot + "/" + ItemPrefabPaths.SanitizeFileName(fileName) + ".asset";
    }

    private List<string> ValidateEffect()
    {
        List<string> errors = new List<string>();
        if (string.IsNullOrWhiteSpace(effectName)) errors.Add("Укажи имя эффекта.");
        if (string.IsNullOrWhiteSpace(effectId)) errors.Add("Укажи id эффекта.");
        if (effectAllowedTargets == EffectTargetMask.None) errors.Add("Выбери хотя бы одну цель эффекта.");
        if (effectDuration < 0f) errors.Add("Длительность не может быть отрицательной.");
        if (effectMaxStacks < 1) errors.Add("Максимум стаков должен быть не меньше 1.");

        if (effectPeriodicEffects != null)
        {
            for (int i = 0; i < effectPeriodicEffects.Count; i++)
            {
                PeriodicEffect periodic = effectPeriodicEffects[i];
                if (periodic != null && periodic.interval <= 0f)
                    errors.Add("Интервал периодического эффекта " + (i + 1) + " должен быть больше нуля.");
            }
        }

        return errors;
    }

    private void CreateEffect()
    {
        status = null;

        List<string> errors = ValidateEffect();
        if (errors.Count > 0)
        {
            status = errors[0];
            return;
        }

        if (IsEffectIdTaken(effectId, editingEffect))
        {
            status = $"ID эффекта '{effectId}' уже используется.";
            return;
        }

        EffectDefinition effect;

        if (editingEffect != null)
        {
            effect = editingEffect;
        }
        else
        {
            string path = GetEffectPath();
            EffectDefinition existing = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);

            if (existing != null && overwriteMode == PrefabOverwriteMode.SkipExisting)
            {
                status = "Эффект уже существует. Выбрано «Пропустить существующие».";
                return;
            }

            if (existing != null && overwriteMode == PrefabOverwriteMode.CreateCopy)
                path = GetUniqueAssetPath(path);

            effect = existing;
            if (effect == null || overwriteMode == PrefabOverwriteMode.CreateCopy)
            {
                ItemPrefabPaths.EnsureFolder(EffectRoot);
                effect = ScriptableObject.CreateInstance<EffectDefinition>();
                AssetDatabase.CreateAsset(effect, path);
                AssetDatabase.SaveAssets();
            }
        }

        effect.id = effectId;
        effect.effectName = effectName;
        effect.description = effectDescription;
        effect.icon = effectIcon;
        effect.duration = Mathf.Max(0f, effectDuration);
        effect.stackPolicy = effectStackPolicy;
        effect.maxStacks = Mathf.Max(1, effectMaxStacks);
        effect.allowedTargets = effectAllowedTargets;
        effect.visibility = effectVisibility;
        effect.tags = effectTags;
        effect.periodicEffects = new List<PeriodicEffect>(
            effectPeriodicEffects != null ? effectPeriodicEffects : new List<PeriodicEffect>());
        effect.modifiers = new List<StatModifier>(
            effectModifiers != null ? effectModifiers : new List<StatModifier>());

        EditorUtility.SetDirty(effect);
        AssetDatabase.SaveAssets();

        Selection.activeObject = effect;
        EditorGUIUtility.PingObject(effect);
        status = editingEffect != null
            ? $"Обновлено: {effect.effectName}"
            : $"Готово: {effect.effectName}";
    }

    private static bool IsEffectIdTaken(string id, EffectDefinition except)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:EffectDefinition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EffectDefinition other = AssetDatabase.LoadAssetAtPath<EffectDefinition>(path);
            if (other != null && other != except &&
                string.Equals(other.id, id, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawContainerFields()
    {
        if (type != ItemCreationType.Container && type != ItemCreationType.Backpack && type != ItemCreationType.Rig)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Контейнер", EditorStyles.boldLabel);

        if (type == ItemCreationType.Rig)
        {
            DrawRigPockets();
            DrawContainerPreview();
            return;
        }

        gridTemplate = (ContainerGridData)EditorGUILayout.ObjectField("Шаблон сетки", gridTemplate, typeof(ContainerGridData), false);
        containerSize = EditorGUILayout.Vector2IntField("Размер сетки (строки, столбцы)", containerSize);
        gridAllowedCategories = (ItemCategory)EditorGUILayout.EnumFlagsField("Что можно класть", gridAllowedCategories);
        DrawContainerPreview();
    }

    private void DrawRigPockets()
    {
        EditorGUILayout.LabelField("Карманы", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Позиция кармана задаётся в пикселях от левого верхнего угла сетки. " +
            "Размер — в ячейках 50x50 px.",
            MessageType.None);

        int oldCount = pocketCount;
        int newCount = Mathf.Max(1, EditorGUILayout.IntField("Количество карманов", pocketCount));
        bool countChanged = newCount != pocketCount;
        pocketCount = newCount;

        while (pocketSizes.Count < pocketCount) pocketSizes.Add(new Vector2Int(2, 2));
        while (pocketSizes.Count > pocketCount) pocketSizes.RemoveAt(pocketSizes.Count - 1);
        while (pocketPositions.Count < pocketCount) pocketPositions.Add(Vector2Int.zero);
        while (pocketPositions.Count > pocketCount) pocketPositions.RemoveAt(pocketPositions.Count - 1);
        while (pocketPixelPositions.Count < pocketCount) pocketPixelPositions.Add(Vector2Int.zero);
        while (pocketPixelPositions.Count > pocketCount) pocketPixelPositions.RemoveAt(pocketPixelPositions.Count - 1);
        while (pocketFoldouts.Count < pocketCount) pocketFoldouts.Add(true);
        while (pocketFoldouts.Count > pocketCount) pocketFoldouts.RemoveAt(pocketFoldouts.Count - 1);

        if (countChanged && newCount > oldCount)
        {
            for (int i = oldCount; i < newCount; i++)
            {
                pocketPixelPositions[i] = new Vector2Int(-1, -1);
                pocketPositions[i] = Vector2Int.zero;
            }

            for (int i = oldCount; i < newCount; i++)
            {
                Vector2Int pixel = FindFreePixelPosition(pocketSizes[i], Vector2Int.zero, i);
                pocketPixelPositions[i] = pixel;
                pocketPositions[i] = PixelToLogicalPosition(pixel);
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Свернуть все")) SetAllFoldouts(false);
        if (GUILayout.Button("Развернуть все")) SetAllFoldouts(true);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < pocketSizes.Count; i++)
        {
            Vector2Int pixel = pocketPixelPositions[i];
            string title = "Карман " + (i + 1)
                + "   X: " + pixel.x + " px"
                + "   Y: " + pixel.y + " px"
                + "   разм. " + pocketSizes[i].x + "x" + pocketSizes[i].y;

            pocketFoldouts[i] = EditorGUILayout.Foldout(pocketFoldouts[i], title, true);
            if (!pocketFoldouts[i]) continue;

            EditorGUI.indentLevel++;
            pocketPixelPositions[i] = EditorGUILayout.Vector2IntField(
                "Позиция (X, Y), px", pocketPixelPositions[i]);
            pocketPositions[i] = PixelToLogicalPosition(pocketPixelPositions[i]);
            pocketSizes[i] = EditorGUILayout.Vector2IntField(
                "Размер (строки, столбцы)", pocketSizes[i]);

            if (GUILayout.Button("Копировать", GUILayout.Width(130f)))
                CopyPocket(i);
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("Авто-раскладка"))
            ApplyAutoLayout();

        SyncLogicalPositions();
        Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
        Vector2Int pixelSize = CalculatePixelSize(pocketPixelPositions, pocketSizes);
        EditorGUILayout.LabelField(
            "Итоговая сетка",
            bounds.x + "x" + bounds.y + "   " + pixelSize.x + "x" + pixelSize.y + " px");
    }

    private void ApplyAutoLayout()
    {
        ContainerGridPacker.LayoutResult layout = ContainerGridPacker.Pack(pocketSizes);
        pocketPositions.Clear();
        pocketPixelPositions.Clear();

        for (int i = 0; i < layout.Positions.Count; i++)
        {
            Vector2Int logical = layout.Positions[i];
            pocketPositions.Add(logical);
            pocketPixelPositions.Add(new Vector2Int(logical.y * 50, logical.x * 50));
        }
    }

    private void SetAllFoldouts(bool value)
    {
        for (int i = 0; i < pocketFoldouts.Count; i++)
            pocketFoldouts[i] = value;
    }

    private void CopyPocket(int index)
    {
        if (index < 0 || index >= pocketSizes.Count) return;

        Vector2Int size = pocketSizes[index];
        Vector2Int pixel = FindFreePixelPosition(size, pocketPixelPositions[index]);

        pocketSizes.Insert(index + 1, size);
        pocketPositions.Insert(index + 1, PixelToLogicalPosition(pixel));
        pocketPixelPositions.Insert(index + 1, pixel);
        pocketFoldouts.Insert(index + 1, true);
        pocketCount = pocketSizes.Count;
    }

    private Vector2Int FindFreePixelPosition(Vector2Int size, Vector2Int preferred, int ignoreIndex = -1)
    {
        int width = Mathf.Max(1, size.y) * 50;
        int height = Mathf.Max(1, size.x) * 50;

        if (IsPixelPositionFree(preferred, size, ignoreIndex))
            return preferred;

        Vector2Int right = new Vector2Int(preferred.x + width, preferred.y);
        if (IsPixelPositionFree(right, size, ignoreIndex))
            return right;

        for (int y = 0; y + height <= ContainerGridValidator.MaxRows * 50; y += 50)
        {
            for (int x = 0; x + width <= ContainerGridValidator.MaxColumns * 50; x += 50)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                if (IsPixelPositionFree(candidate, size, ignoreIndex))
                    return candidate;
            }
        }

        return right;
    }

    private bool IsPixelPositionFree(Vector2Int pixelPosition, Vector2Int size, int ignoreIndex = -1)
    {
        int width = Mathf.Max(1, size.y) * 50;
        int height = Mathf.Max(1, size.x) * 50;

        for (int i = 0; i < pocketSizes.Count; i++)
        {
            if (i == ignoreIndex) continue;

            Vector2Int other = pocketPixelPositions[i];
            if (other.x < 0 || other.y < 0) continue;

            int otherWidth = Mathf.Max(1, pocketSizes[i].y) * 50;
            int otherHeight = Mathf.Max(1, pocketSizes[i].x) * 50;

            bool overlaps =
                pixelPosition.x < other.x + otherWidth &&
                other.x < pixelPosition.x + width &&
                pixelPosition.y < other.y + otherHeight &&
                other.y < pixelPosition.y + height;

            if (overlaps) return false;
        }

        return true;
    }

    private static Vector2Int PixelToLogicalPosition(Vector2Int pixelPosition)
    {
        return new Vector2Int(
            Mathf.Max(0, pixelPosition.y) / 50,
            Mathf.Max(0, pixelPosition.x) / 50);
    }

    private void SyncLogicalPositions()
    {
        int count = Mathf.Min(pocketPositions.Count, pocketPixelPositions.Count);
        for (int i = 0; i < count; i++)
            pocketPositions[i] = PixelToLogicalPosition(pocketPixelPositions[i]);
    }

    private static Vector2Int CalculatePixelSize(List<Vector2Int> pixelPositions, List<Vector2Int> sizes)
    {
        int width = 0;
        int height = 0;
        for (int i = 0; i < pixelPositions.Count && i < sizes.Count; i++)
        {
            width = Mathf.Max(width, pixelPositions[i].x + Mathf.Max(1, sizes[i].y) * 50);
            height = Mathf.Max(height, pixelPositions[i].y + Mathf.Max(1, sizes[i].x) * 50);
        }

        return new Vector2Int(Mathf.Max(0, width), Mathf.Max(0, height));
    }

    private class PreviewPocket
    {
        public Vector2Int Position;
        public Vector2Int PixelPosition;
        public Vector2Int Size;
    }

    private class PreviewGrid
    {
        public Vector2Int Size;
        public readonly List<PreviewPocket> Pockets = new List<PreviewPocket>();
        public readonly HashSet<int> InvalidPockets = new HashSet<int>();
    }

    private void DrawContainerPreview()
    {
        PreviewGrid preview = BuildPreviewGrid();
        int cells = preview.Size.x * preview.Size.y;
        int available = 0;
        foreach (PreviewPocket pocket in preview.Pockets)
            available += pocket.Size.x * pocket.Size.y;

        int pixelWidth = preview.Size.y * 50;
        int pixelHeight = preview.Size.x * 50;
        foreach (PreviewPocket pocket in preview.Pockets)
        {
            pixelWidth = Mathf.Max(pixelWidth, pocket.PixelPosition.x + Mathf.Max(1, pocket.Size.y) * 50);
            pixelHeight = Mathf.Max(pixelHeight, pocket.PixelPosition.y + Mathf.Max(1, pocket.Size.x) * 50);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Предпросмотр сетки", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Сетка " + preview.Size.x + "x" + preview.Size.y +
            "   ячеек: " + cells +
            "   карманов: " + preview.Pockets.Count +
            "   доступно: " + available +
            "   размер: " + pixelWidth + "x" + pixelHeight + " px");

        EditorGUILayout.BeginVertical(GUI.skin.box);
        Rect previewRect = EditorGUILayout.GetControlRect(false, 240f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            DrawPreviewGrid(previewRect, preview);
        EditorGUILayout.EndVertical();
    }

    private PreviewGrid BuildPreviewGrid()
    {
        PreviewGrid preview = new PreviewGrid();

        if (type == ItemCreationType.Rig)
        {
            SyncLogicalPositions();
            preview.Size = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
            for (int i = 0; i < pocketSizes.Count; i++)
            {
                preview.Pockets.Add(new PreviewPocket
                {
                    Position = pocketPositions[i],
                    PixelPosition = pocketPixelPositions[i],
                    Size = new Vector2Int(Mathf.Max(1, pocketSizes[i].x), Mathf.Max(1, pocketSizes[i].y))
                });
            }
            FillInvalidPockets(preview, preview.Size, preview.Size.y);
            return preview;
        }

        if (gridTemplate != null)
        {
            preview.Size = gridTemplate.GetBounds();

            List<ContainerGridData.Pocket> pockets = new List<ContainerGridData.Pocket>();
            List<Vector2Int> positions = new List<Vector2Int>();
            List<Vector2Int> sizes = new List<Vector2Int>();
            foreach (ContainerGridData.Pocket pocket in gridTemplate.pockets)
            {
                if (pocket == null) continue;
                pockets.Add(pocket);
                positions.Add(gridTemplate.GetPocketPosition(pocket));
                sizes.Add(new Vector2Int(Mathf.Max(1, pocket.size.x), Mathf.Max(1, pocket.size.y)));
            }

            List<float> originX = new List<float>();
            List<float> originY = new List<float>();
            if (gridTemplate.usePixelPositions)
            {
                foreach (ContainerGridData.Pocket pocket in pockets)
                {
                    originX.Add(pocket.pixelPosition.x);
                    originY.Add(pocket.pixelPosition.y);
                }
            }
            else
            {
                List<float> calculatedX;
                List<float> calculatedY;
                ContainerGridValidator.CalculatePocketOrigins(
                    positions, sizes, gridTemplate.pocketGapPixels, out calculatedX, out calculatedY);
                originX.AddRange(calculatedX);
                originY.AddRange(calculatedY);
            }

            for (int i = 0; i < pockets.Count; i++)
            {
                preview.Pockets.Add(new PreviewPocket
                {
                    Position = positions[i],
                    PixelPosition = new Vector2Int(Mathf.RoundToInt(originX[i]), Mathf.RoundToInt(originY[i])),
                    Size = sizes[i]
                });
            }
        }
        else
        {
            preview.Size = new Vector2Int(Mathf.Max(1, containerSize.x), Mathf.Max(1, containerSize.y));
        }

        if (preview.Pockets.Count == 0)
        {
            preview.Pockets.Add(new PreviewPocket
            {
                Position = Vector2Int.zero,
                PixelPosition = Vector2Int.zero,
                Size = preview.Size
            });
        }

        FillInvalidPockets(preview, preview.Size, Mathf.Max(1, preview.Size.y));
        return preview;
    }

    private void FillInvalidPockets(PreviewGrid preview, Vector2Int gridSize, int maxColumns)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> pixelPositions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (PreviewPocket pocket in preview.Pockets)
        {
            positions.Add(pocket.Position);
            pixelPositions.Add(pocket.PixelPosition);
            sizes.Add(pocket.Size);
        }

        HashSet<int> invalid;
        ContainerGridValidator.ValidateLayout(gridSize, positions, sizes, Mathf.Max(1, maxColumns), out invalid);

        HashSet<int> invalidPixels;
        ContainerGridValidator.ValidatePixelLayout(pixelPositions, sizes, out invalidPixels);

        preview.InvalidPockets.Clear();
        foreach (int index in invalid) preview.InvalidPockets.Add(index);
        foreach (int index in invalidPixels) preview.InvalidPockets.Add(index);
    }

    private void DrawPreviewGrid(Rect rect, PreviewGrid preview)
    {
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.17f, 0.16f, 1f));

        Rect inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);

        float gridColumns = preview.Size.y;
        float gridRows = preview.Size.x;
        foreach (PreviewPocket pocket in preview.Pockets)
        {
            gridColumns = Mathf.Max(
                gridColumns,
                (pocket.PixelPosition.x + Mathf.Max(1, pocket.Size.y) * 50f) / 50f);
            gridRows = Mathf.Max(
                gridRows,
                (pocket.PixelPosition.y + Mathf.Max(1, pocket.Size.x) * 50f) / 50f);
        }

        float cellSize = Mathf.Min(
            inner.width / Mathf.Max(1f, gridColumns),
            inner.height / Mathf.Max(1f, gridRows));
        cellSize = Mathf.Max(2f, cellSize);

        float gridWidth = gridColumns * cellSize;
        float gridHeight = gridRows * cellSize;
        float startX = inner.x + (inner.width - gridWidth) * 0.5f;
        float startY = inner.y + (inner.height - gridHeight) * 0.5f;

        Color cellColor = new Color(0.58f, 0.60f, 0.56f, 1f);
        Color invalidColor = new Color(0.78f, 0.25f, 0.22f, 1f);
        for (int index = 0; index < preview.Pockets.Count; index++)
        {
            PreviewPocket pocket = preview.Pockets[index];
            Color color = preview.InvalidPockets.Contains(index) ? invalidColor : cellColor;
            float pocketX = startX + pocket.PixelPosition.x / 50f * cellSize;
            float pocketY = startY + pocket.PixelPosition.y / 50f * cellSize;

            for (int row = 0; row < pocket.Size.x; row++)
            {
                for (int col = 0; col < pocket.Size.y; col++)
                {
                    float x = pocketX + col * cellSize;
                    float y = pocketY + row * cellSize;
                    EditorGUI.DrawRect(
                        new Rect(x, y, Mathf.Max(1f, cellSize - 1f), Mathf.Max(1f, cellSize - 1f)),
                        color);
                }
            }
        }
    }

    private void ApplyDefaults()
    {
        switch (type)
        {
            case ItemCreationType.Weapon:
                category = ItemCategory.Weapon;
                size = new Vector2Int(5, 10);
                maxStackSize = 1;
                maxDurability = 100f;
                break;
            case ItemCreationType.Armor:
                category = ItemCategory.Armor;
                size = new Vector2Int(4, 4);
                break;
            case ItemCreationType.Clothing:
                category = ItemCategory.Clothing;
                size = new Vector2Int(3, 3);
                break;
            case ItemCreationType.Container:
                category = ItemCategory.Container;
                size = new Vector2Int(3, 3);
                containerSize = new Vector2Int(10, 10);
                break;
            case ItemCreationType.Backpack:
                category = ItemCategory.Backpack;
                size = new Vector2Int(5, 5);
                containerSize = new Vector2Int(50, 15);
                break;
            case ItemCreationType.Rig:
                category = ItemCategory.Rig;
                size = new Vector2Int(4, 5);
                pocketCount = 4;
                if (pocketSizes.Count == 0)
                {
                    pocketSizes.Add(new Vector2Int(3, 3));
                    pocketSizes.Add(new Vector2Int(2, 2));
                    pocketSizes.Add(new Vector2Int(2, 1));
                    pocketSizes.Add(new Vector2Int(2, 3));

                    pocketPositions.Add(new Vector2Int(0, 0));
                    pocketPositions.Add(new Vector2Int(0, 3));
                    pocketPositions.Add(new Vector2Int(3, 0));
                    pocketPositions.Add(new Vector2Int(3, 1));

                    pocketPixelPositions.Add(new Vector2Int(0, 0));
                    pocketPixelPositions.Add(new Vector2Int(150, 0));
                    pocketPixelPositions.Add(new Vector2Int(0, 150));
                    pocketPixelPositions.Add(new Vector2Int(50, 150));
                }
                break;
            case ItemCreationType.Consumable:
                category = ItemCategory.Food;
                size = new Vector2Int(1, 1);
                maxStackSize = 5;
                break;
            case ItemCreationType.Ammo:
                category = ItemCategory.Ammo;
                size = new Vector2Int(1, 1);
                maxStackSize = 60;
                break;
            case ItemCreationType.WeaponMod:
                category = ItemCategory.WeaponMod;
                size = new Vector2Int(1, 2);
                break;
            case ItemCreationType.Generic:
                category = ItemCategory.None;
                size = new Vector2Int(1, 1);
                break;
        }

        if (string.IsNullOrEmpty(id)) id = Slugify(itemName);
    }

    private void CreateItem()
    {
        status = null;
        List<string> errors = ValidateWizard();
        if (errors.Count > 0)
        {
            status = errors[0];
            return;
        }

        ItemData data;

        if (editMode)
        {
            if (editingData == null)
            {
                status = "Нет ассета для редактирования.";
                return;
            }

            data = editingData;
        }
        else
        {
            Type dataType = GetDataType();
            string path = ItemPrefabPaths.GetDataPath(dataType, itemName);
            ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (existing != null && overwriteMode == PrefabOverwriteMode.SkipExisting)
            {
                status = "Предмет уже существует. Выбрано «Пропустить существующие».";
                return;
            }

            if (existing != null && existing.GetType() != dataType)
            {
                status = $"По пути {path} уже лежит {existing.GetType().Name}. Выбери другой ID/имя или режим копии.";
                return;
            }

            if (existing != null && overwriteMode == PrefabOverwriteMode.CreateCopy)
                path = GetUniqueAssetPath(path);

            data = existing;
            if (data == null || overwriteMode == PrefabOverwriteMode.CreateCopy)
            {
                data = ScriptableObject.CreateInstance(dataType) as ItemData;
                if (data == null)
                {
                    status = "Не удалось создать данные предмета.";
                    return;
                }
                ItemPrefabPaths.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
            }
        }

        FillData(data);
        data.id = id;
        data.itemName = itemName;
        data.prefabName = string.IsNullOrWhiteSpace(prefabName) ? itemName : prefabName;
        data.category = category;
        data.size = size;
        data.weight = weight;
        data.maxStackSize = maxStackSize;
        data.maxDurability = maxDurability;
        data.icon = icon;
        data.sourceModel3D = sourceModel3D;

        ContainerData container = data as ContainerData;
        if (container != null)
        {
            ContainerGridData grid = CreateOrUpdateGrid(data);
            if (grid == null)
            {
                status = "Не удалось создать ContainerGridData.";
                return;
            }
            container.grid = grid;
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        PrefabOverwriteMode mode = editMode ? PrefabOverwriteMode.Overwrite : overwriteMode;
        if (!ItemPrefabFactory.Generate(data, mode))
        {
            status = "Данные обновлены, но prefabs не сгенерированы. Проверь консоль.";
            return;
        }

        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
        status = editMode ? $"Обновлено: {data.itemName}" : $"Готово: {data.itemName}";
    }

    private void LoadFromData(ItemData data)
    {
        type = GetTypeForData(data);
        editMode = true;
        editingData = data;

        itemName = data.itemName;
        prefabName = data.prefabName;
        id = data.id;
        category = data.category;
        size = data.size;
        weight = data.weight;
        maxStackSize = data.maxStackSize;
        maxDurability = data.maxDurability;
        icon = data.icon;
        sourceModel3D = data.sourceModel3D != null ? data.sourceModel3D : data.modelPrefab3D;

        if (data is WeaponData weapon)
        {
            weaponClass = weapon.weaponClass;
            damage = weapon.damage;
            fireRate = weapon.fireRate;
            caliber = weapon.caliber;
            recoil = weapon.recoil;
            accuracy = weapon.accuracy;
        }
        else if (data is ArmorData armor)
        {
            armorSlot = armor.armorSlot;
            armorClass = armor.armorClass;
            damageReduction = armor.damageReduction;
        }
        else if (data is ClothingData clothing)
        {
            clothingSlot = clothing.clothingSlot;
            insulation = clothing.insulation;
        }
        else if (data is ConsumableData consumable)
        {
            consumableType = consumable.consumableType;
            useTime = consumable.useTime;
            uses = consumable.uses;
            consumableEffects = consumable.effects != null
                ? new List<EffectDefinition>(consumable.effects)
                : new List<EffectDefinition>();
        }
        else if (data is AmmoData ammo)
        {
            ammoCaliber = ammo.caliber;
            bulletType = ammo.bulletType;
            damageMultiplier = ammo.damageMultiplier;
            penetration = ammo.penetration;
        }
        else if (data is WeaponModData mod)
        {
            modSlotType = mod.slotType;
            compatibleWeaponIds = string.Join(", ", mod.compatibleWeaponIds);
        }

        EquippableData equippable = data as EquippableData;
        equippableEffects = equippable != null && equippable.effects != null
            ? new List<EffectDefinition>(equippable.effects)
            : new List<EffectDefinition>();

        editingGrid = null;
        gridTemplate = null;
        if (consumableEffects == null) consumableEffects = new List<EffectDefinition>();
        else consumableEffects.Clear();
        if (equippableEffects == null) equippableEffects = new List<EffectDefinition>();
        else equippableEffects.Clear();
        pocketSizes.Clear();
        pocketPositions.Clear();
        pocketPixelPositions.Clear();
        pocketFoldouts.Clear();
        pocketCount = 0;

        ContainerData container = data as ContainerData;
        if (container != null && container.grid != null)
        {
            editingGrid = container.grid;
            containerSize = container.grid.GetBounds();
            gridAllowedCategories = container.grid.allowedCategories;

            if (type == ItemCreationType.Rig)
            {
                List<Vector2Int> pixelPositions = new List<Vector2Int>();
                if (container.grid.usePixelPositions)
                {
                    foreach (ContainerGridData.Pocket pocket in container.grid.pockets)
                    {
                        if (pocket == null) continue;
                        pixelPositions.Add(pocket.pixelPosition);
                    }
                }
                else
                {
                    List<Vector2Int> positions = new List<Vector2Int>();
                    List<Vector2Int> sizes = new List<Vector2Int>();
                    foreach (ContainerGridData.Pocket pocket in container.grid.pockets)
                    {
                        if (pocket == null) continue;
                        positions.Add(pocket.position);
                        sizes.Add(pocket.size);
                    }

                    List<float> originX;
                    List<float> originY;
                    ContainerGridValidator.CalculatePocketOrigins(
                        positions, sizes, container.grid.pocketGapPixels, out originX, out originY);

                    for (int i = 0; i < originX.Count; i++)
                    {
                        pixelPositions.Add(new Vector2Int(
                            Mathf.RoundToInt(originX[i]),
                            Mathf.RoundToInt(originY[i])));
                    }
                }

                int index = 0;
                foreach (ContainerGridData.Pocket pocket in container.grid.pockets)
                {
                    if (pocket == null) continue;

                    Vector2Int pixel = index < pixelPositions.Count ? pixelPositions[index] : Vector2Int.zero;
                    pocketSizes.Add(pocket.size);
                    pocketPixelPositions.Add(pixel);
                    pocketPositions.Add(PixelToLogicalPosition(pixel));
                    pocketFoldouts.Add(true);
                    index++;
                }

                pocketCount = pocketSizes.Count;
            }
        }

        status = null;
    }

    private static ItemCreationType GetTypeForData(ItemData data)
    {
        if (data is WeaponData) return ItemCreationType.Weapon;
        if (data is ArmorData) return ItemCreationType.Armor;
        if (data is ClothingData) return ItemCreationType.Clothing;
        if (data is RigData) return ItemCreationType.Rig;
        if (data is BackpackData) return ItemCreationType.Backpack;
        if (data is ContainerData) return ItemCreationType.Container;
        if (data is ConsumableData) return ItemCreationType.Consumable;
        if (data is AmmoData) return ItemCreationType.Ammo;
        if (data is WeaponModData) return ItemCreationType.WeaponMod;
        return ItemCreationType.Generic;
    }

    private void FillData(ItemData data)
    {
        switch (type)
        {
            case ItemCreationType.Weapon:
                WeaponData weapon = data as WeaponData;
                if (weapon == null) return;
                weapon.weaponClass = weaponClass;
                weapon.damage = damage;
                weapon.fireRate = fireRate;
                weapon.caliber = caliber;
                weapon.recoil = recoil;
                weapon.accuracy = accuracy;
                break;

            case ItemCreationType.Armor:
                ArmorData armor = data as ArmorData;
                if (armor == null) return;
                armor.armorSlot = armorSlot;
                armor.armorClass = armorClass;
                armor.damageReduction = damageReduction;
                break;

            case ItemCreationType.Clothing:
                ClothingData clothing = data as ClothingData;
                if (clothing == null) return;
                clothing.clothingSlot = clothingSlot;
                clothing.insulation = insulation;
                break;

            case ItemCreationType.Consumable:
                ConsumableData consumable = data as ConsumableData;
                if (consumable == null) return;
                consumable.consumableType = consumableType;
                consumable.useTime = useTime;
                consumable.uses = uses;
                consumable.effects = new List<EffectDefinition>();
                if (consumableEffects != null)
                {
                    foreach (EffectDefinition effect in consumableEffects)
                    {
                        if (effect != null) consumable.effects.Add(effect);
                    }
                }
                break;

            case ItemCreationType.Ammo:
                AmmoData ammo = data as AmmoData;
                if (ammo == null) return;
                ammo.caliber = ammoCaliber;
                ammo.bulletType = bulletType;
                ammo.damageMultiplier = damageMultiplier;
                ammo.penetration = penetration;
                break;

            case ItemCreationType.WeaponMod:
                WeaponModData mod = data as WeaponModData;
                if (mod == null) return;
                mod.slotType = modSlotType;
                mod.compatibleWeaponIds = ParseIds(compatibleWeaponIds);
                break;
        }

        EquippableData equippable = data as EquippableData;
        if (equippable != null)
        {
            equippable.effects = new List<EffectDefinition>();
            if (equippableEffects != null)
            {
                foreach (EffectDefinition effect in equippableEffects)
                {
                    if (effect != null) equippable.effects.Add(effect);
                }
            }
        }
    }

    private ContainerGridData CreateOrUpdateGrid(ItemData data)
    {
        ContainerData container = data as ContainerData;
        ContainerGridData grid = editMode && container != null ? container.grid : null;

        if (grid == null)
        {
            string path = ItemPrefabPaths.GetGridPath(itemName);
            grid = AssetDatabase.LoadAssetAtPath<ContainerGridData>(path);
            if (grid == null)
            {
                ItemPrefabPaths.EnsureFolder(ItemPrefabPaths.GridRoot);
                grid = ScriptableObject.CreateInstance<ContainerGridData>();
                AssetDatabase.CreateAsset(grid, path);
            }
        }

        if (editMode || overwriteMode != PrefabOverwriteMode.SkipExisting)
        {
            if (type == ItemCreationType.Rig)
            {
                SyncLogicalPositions();
                Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
                grid.size = bounds;
                grid.limitColumns = true;
                grid.maxColumns = Mathf.Max(1, bounds.y);
                grid.allowedCategories = ItemCategory.All;
                grid.usePixelPositions = true;
                grid.pocketGapPixels = 0;
                grid.pockets.Clear();

                for (int i = 0; i < pocketSizes.Count; i++)
                {
                    ContainerGridData.Pocket pocket = new ContainerGridData.Pocket();
                    pocket.name = "Карман " + (i + 1);
                    pocket.position = PixelToLogicalPosition(pocketPixelPositions[i]);
                    pocket.pixelPosition = pocketPixelPositions[i];
                    pocket.size = pocketSizes[i];
                    grid.pockets.Add(pocket);
                }
            }
            else if (!editMode && gridTemplate != null)
            {
                grid.size = gridTemplate.size;
                grid.limitColumns = gridTemplate.limitColumns;
                grid.maxColumns = gridTemplate.maxColumns;
                grid.allowedCategories = gridTemplate.allowedCategories;
                grid.usePixelPositions = gridTemplate.usePixelPositions;
                grid.pocketGapPixels = Mathf.Clamp(
                    gridTemplate.pocketGapPixels,
                    ContainerGridValidator.MinPocketGapPixels,
                    ContainerGridValidator.MaxPocketGapPixels);
                grid.pockets.Clear();
                foreach (ContainerGridData.Pocket pocket in gridTemplate.pockets)
                {
                    if (pocket == null) continue;
                    ContainerGridData.Pocket copy = new ContainerGridData.Pocket();
                    copy.name = pocket.name;
                    copy.position = pocket.position;
                    copy.pixelPosition = pocket.pixelPosition;
                    copy.size = pocket.size;
                    grid.pockets.Add(copy);
                }
            }
            else if (!(editMode && type == ItemCreationType.Container && grid.pockets.Count > 0))
            {
                grid.size = containerSize;
                grid.limitColumns = true;
                grid.maxColumns = Mathf.Max(1, containerSize.y);
                grid.allowedCategories = gridAllowedCategories;
                grid.usePixelPositions = false;
                grid.pocketGapPixels = 0;
                grid.pockets.Clear();
            }
        }

        EditorUtility.SetDirty(grid);
        AssetDatabase.SaveAssets();
        return grid;
    }

    private List<string> ValidateWizard()
    {
        List<string> errors = new List<string>();
        if (string.IsNullOrWhiteSpace(itemName)) errors.Add("Укажи имя предмета.");
        if (string.IsNullOrWhiteSpace(id)) errors.Add("Укажи id предмета.");
        if (size.x <= 0 || size.y <= 0) errors.Add("Размер должен быть больше нуля.");
        if (icon == null) errors.Add("Назначь иконку.");
        if (sourceModel3D == null) errors.Add("Назначь исходную 3D-модель.");
        if (category == ItemCategory.None) errors.Add("Выбери категорию.");

        if (type == ItemCreationType.Container || type == ItemCreationType.Backpack)
        {
            if (containerSize.x <= 0 || containerSize.y <= 0)
                errors.Add("Размер сетки контейнера должен быть больше нуля.");
        }

        if (type == ItemCreationType.Rig)
        {
            if (pocketSizes.Count == 0)
            {
                errors.Add("Добавь хотя бы один карман.");
            }
            else
            {
                SyncLogicalPositions();
                Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
                HashSet<int> invalid;
                errors.AddRange(ContainerGridValidator.ValidateLayout(
                    bounds, pocketPositions, pocketSizes, Mathf.Max(1, bounds.y), out invalid));

                HashSet<int> invalidPixels;
                errors.AddRange(ContainerGridValidator.ValidatePixelLayout(
                    pocketPixelPositions, pocketSizes, out invalidPixels));
            }
        }

        return errors;
    }

    private Type GetDataType()
    {
        switch (type)
        {
            case ItemCreationType.Weapon: return typeof(WeaponData);
            case ItemCreationType.Armor: return typeof(ArmorData);
            case ItemCreationType.Clothing: return typeof(ClothingData);
            case ItemCreationType.Container: return typeof(ContainerData);
            case ItemCreationType.Backpack: return typeof(BackpackData);
            case ItemCreationType.Rig: return typeof(RigData);
            case ItemCreationType.Consumable: return typeof(ConsumableData);
            case ItemCreationType.Ammo: return typeof(AmmoData);
            case ItemCreationType.WeaponMod: return typeof(WeaponModData);
            default: return typeof(ItemData);
        }
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "item_" + DateTime.UtcNow.Ticks;

        string result = value.Trim().ToLowerInvariant().Replace(' ', '_');
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            result = result.Replace(c, '_');

        return string.IsNullOrEmpty(result) ? "item_" + DateTime.UtcNow.Ticks : result;
    }

    private static string GetUniqueAssetPath(string path)
    {
        string directory = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        string extension = System.IO.Path.GetExtension(path);

        for (int i = 2; i < 1000; i++)
        {
            string candidate = directory + "/" + name + "_" + i + extension;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidate) == null)
                return candidate;
        }

        return path;
    }

    private static List<string> ParseIds(string value)
    {
        List<string> ids = new List<string>();
        if (string.IsNullOrWhiteSpace(value)) return ids;

        foreach (string part in value.Split(','))
        {
            string id = part.Trim();
            if (!string.IsNullOrEmpty(id)) ids.Add(id);
        }

        return ids;
    }
}
