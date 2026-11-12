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
    private int pocketGapPixels = 1;
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

    [MenuItem("Tools/Contract/Мастер создания предмета")]
    public static void OpenFromMenu()
    {
        OpenWindow();
    }

    public static void OpenWindow()
    {
        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.editMode = false;
        window.editingData = null;
        window.editingGrid = null;
        window.Show();
    }

    public static void OpenForEdit(ItemData data)
    {
        if (data == null) return;

        ItemCreationWizard window = GetWindow<ItemCreationWizard>("Мастер предметов");
        window.minSize = new Vector2(520f, 720f);
        window.editMode = true;
        window.editingData = data;
        window.LoadFromData(data);
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
        pocketGapPixels = Mathf.Clamp(
            EditorGUILayout.IntField("Зазор между карманами, px", pocketGapPixels),
            ContainerGridValidator.MinPocketGapPixels,
            ContainerGridValidator.MaxPocketGapPixels);
        EditorGUILayout.HelpBox(
            "Зазор добавляется между соседними карманами и не меняет размер ячейки.",
            MessageType.None);

        int newCount = Mathf.Max(1, EditorGUILayout.IntField("Количество карманов", pocketCount));
        bool countChanged = newCount != pocketCount;
        pocketCount = newCount;

        while (pocketSizes.Count < pocketCount) pocketSizes.Add(new Vector2Int(2, 2));
        while (pocketSizes.Count > pocketCount) pocketSizes.RemoveAt(pocketSizes.Count - 1);
        while (pocketPositions.Count < pocketCount) pocketPositions.Add(Vector2Int.zero);
        while (pocketPositions.Count > pocketCount) pocketPositions.RemoveAt(pocketPositions.Count - 1);
        while (pocketFoldouts.Count < pocketCount) pocketFoldouts.Add(true);
        while (pocketFoldouts.Count > pocketCount) pocketFoldouts.RemoveAt(pocketFoldouts.Count - 1);

        if (countChanged) ApplyAutoLayout();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Свернуть все")) SetAllFoldouts(false);
        if (GUILayout.Button("Развернуть все")) SetAllFoldouts(true);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < pocketSizes.Count; i++)
        {
            string title = "Карман " + (i + 1)
                + "   поз. " + pocketPositions[i].x + "x" + pocketPositions[i].y
                + "   разм. " + pocketSizes[i].x + "x" + pocketSizes[i].y;

            pocketFoldouts[i] = EditorGUILayout.Foldout(pocketFoldouts[i], title, true);
            if (!pocketFoldouts[i]) continue;

            EditorGUI.indentLevel++;
            pocketPositions[i] = EditorGUILayout.Vector2IntField(
                "Позиция (строки, столбцы)", pocketPositions[i]);
            pocketSizes[i] = EditorGUILayout.Vector2IntField(
                "Размер (строки, столбцы)", pocketSizes[i]);

            if (GUILayout.Button("Копировать", GUILayout.Width(130f)))
                CopyPocket(i);
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("Авто-раскладка"))
            ApplyAutoLayout();

        Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
        EditorGUILayout.LabelField("Итоговая сетка", bounds.x + "x" + bounds.y);
    }

    private void ApplyAutoLayout()
    {
        ContainerGridPacker.LayoutResult layout = ContainerGridPacker.Pack(pocketSizes);
        pocketPositions.Clear();
        pocketPositions.AddRange(layout.Positions);
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
        Vector2Int position = FindFreePocketPosition(size, pocketPositions[index]);

        pocketSizes.Insert(index + 1, size);
        pocketPositions.Insert(index + 1, position);
        pocketFoldouts.Insert(index + 1, true);
        pocketCount = pocketSizes.Count;
    }

    private Vector2Int FindFreePocketPosition(Vector2Int size, Vector2Int preferred)
    {
        // В логических ячейках карманы могут стоять вплотную:
        // визуальный промежуток задаётся в пикселях.
        Vector2Int right = new Vector2Int(preferred.x, preferred.y + size.y);
        if (right.y + size.y <= ContainerGridValidator.MaxColumns && IsPocketPositionFree(right, size))
            return right;

        for (int row = 0; row + size.x <= ContainerGridValidator.MaxRows; row++)
        {
            for (int col = 0; col + size.y <= ContainerGridValidator.MaxColumns; col++)
            {
                Vector2Int candidate = new Vector2Int(row, col);
                if (IsPocketPositionFree(candidate, size))
                    return candidate;
            }
        }

        // Запасной вариант — исходная позиция со сдвигом; валидатор подсветит ошибку.
        return new Vector2Int(preferred.x, preferred.y + size.y);
    }

    private bool IsPocketPositionFree(Vector2Int position, Vector2Int size)
    {
        for (int i = 0; i < pocketSizes.Count; i++)
        {
            Vector2Int otherPosition = pocketPositions[i];
            Vector2Int otherSize = pocketSizes[i];

            bool overlaps =
                position.x < otherPosition.x + otherSize.x &&
                otherPosition.x < position.x + size.x &&
                position.y < otherPosition.y + otherSize.y &&
                otherPosition.y < position.y + size.y;

            if (overlaps) return false;
        }

        return true;
    }

    private class PreviewPocket
    {
        public Vector2Int Position;
        public Vector2Int Size;
    }

    private class PreviewGrid
    {
        public Vector2Int Size;
        public int GapPixels;
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

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Предпросмотр сетки", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Сетка " + preview.Size.x + "x" + preview.Size.y +
            "   ячеек: " + cells +
            "   карманов: " + preview.Pockets.Count +
            "   доступно: " + available +
            "   зазор: " + preview.GapPixels + " px");

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
            preview.Size = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
            preview.GapPixels = Mathf.Clamp(
                pocketGapPixels,
                ContainerGridValidator.MinPocketGapPixels,
                ContainerGridValidator.MaxPocketGapPixels);
            for (int i = 0; i < pocketSizes.Count; i++)
            {
                preview.Pockets.Add(new PreviewPocket
                {
                    Position = pocketPositions[i],
                    Size = new Vector2Int(Mathf.Max(1, pocketSizes[i].x), Mathf.Max(1, pocketSizes[i].y))
                });
            }
            FillInvalidPockets(preview, preview.Size, preview.Size.y);
            return preview;
        }

        if (gridTemplate != null)
        {
            preview.Size = gridTemplate.GetBounds();
            preview.GapPixels = Mathf.Clamp(
                gridTemplate.pocketGapPixels,
                ContainerGridValidator.MinPocketGapPixels,
                ContainerGridValidator.MaxPocketGapPixels);
            foreach (ContainerGridData.Pocket pocket in gridTemplate.pockets)
            {
                if (pocket == null) continue;
                preview.Pockets.Add(new PreviewPocket
                {
                    Position = pocket.position,
                    Size = new Vector2Int(Mathf.Max(1, pocket.size.x), Mathf.Max(1, pocket.size.y))
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
                Size = preview.Size
            });
        }

        return preview;
    }

    private void FillInvalidPockets(PreviewGrid preview, Vector2Int gridSize, int maxColumns)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (PreviewPocket pocket in preview.Pockets)
        {
            positions.Add(pocket.Position);
            sizes.Add(pocket.Size);
        }

        HashSet<int> invalid;
        ContainerGridValidator.ValidateLayout(gridSize, positions, sizes, Mathf.Max(1, maxColumns), out invalid);
        preview.InvalidPockets.Clear();
        foreach (int index in invalid) preview.InvalidPockets.Add(index);
    }

    private void DrawPreviewGrid(Rect rect, PreviewGrid preview)
    {
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.17f, 0.16f, 1f));

        Rect inner = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);

        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (PreviewPocket pocket in preview.Pockets)
        {
            positions.Add(pocket.Position);
            sizes.Add(pocket.Size);
        }

        int gapPixels = Mathf.Clamp(
            preview.GapPixels,
            ContainerGridValidator.MinPocketGapPixels,
            ContainerGridValidator.MaxPocketGapPixels);

        List<float> originX;
        List<float> originY;
        ContainerGridValidator.CalculatePocketOrigins(
            positions, sizes, gapPixels, out originX, out originY);

        float gridColumns = preview.Size.y;
        float gridRows = preview.Size.x;
        for (int i = 0; i < positions.Count; i++)
        {
            gridColumns = Mathf.Max(gridColumns, (originX[i] + sizes[i].y * 50f) / 50f);
            gridRows = Mathf.Max(gridRows, (originY[i] + sizes[i].x * 50f) / 50f);
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
            float pocketX = startX + originX[index] / 50f * cellSize;
            float pocketY = startY + originY[index] / 50f * cellSize;

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

        editingGrid = null;
        gridTemplate = null;
        pocketSizes.Clear();
        pocketPositions.Clear();
        pocketFoldouts.Clear();
        pocketCount = 0;

        ContainerData container = data as ContainerData;
        if (container != null && container.grid != null)
        {
            editingGrid = container.grid;
            containerSize = container.grid.GetBounds();
            gridAllowedCategories = container.grid.allowedCategories;
            pocketGapPixels = Mathf.Clamp(
                container.grid.pocketGapPixels,
                ContainerGridValidator.MinPocketGapPixels,
                ContainerGridValidator.MaxPocketGapPixels);

            if (type == ItemCreationType.Rig)
            {
                foreach (ContainerGridData.Pocket pocket in container.grid.pockets)
                {
                    if (pocket == null) continue;
                    pocketSizes.Add(pocket.size);
                    pocketPositions.Add(pocket.position);
                    pocketFoldouts.Add(true);
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
                Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
                grid.size = bounds;
                grid.limitColumns = true;
                grid.maxColumns = Mathf.Max(1, bounds.y);
                grid.allowedCategories = ItemCategory.All;
                grid.pocketGapPixels = Mathf.Clamp(
                    pocketGapPixels,
                    ContainerGridValidator.MinPocketGapPixels,
                    ContainerGridValidator.MaxPocketGapPixels);
                grid.pockets.Clear();

                for (int i = 0; i < pocketSizes.Count; i++)
                {
                    ContainerGridData.Pocket pocket = new ContainerGridData.Pocket();
                    pocket.name = "Карман " + (i + 1);
                    pocket.position = pocketPositions[i];
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
            if (pocketGapPixels < ContainerGridValidator.MinPocketGapPixels ||
                pocketGapPixels > ContainerGridValidator.MaxPocketGapPixels)
            {
                errors.Add("Зазор между карманами должен быть от " +
                    ContainerGridValidator.MinPocketGapPixels + " до " +
                    ContainerGridValidator.MaxPocketGapPixels + " px.");
            }

            if (pocketSizes.Count == 0)
            {
                errors.Add("Добавь хотя бы один карман.");
            }
            else
            {
                Vector2Int bounds = ContainerGridValidator.CalculateBounds(pocketPositions, pocketSizes);
                HashSet<int> invalid;
                errors.AddRange(ContainerGridValidator.ValidateLayout(
                    bounds, pocketPositions, pocketSizes, Mathf.Max(1, bounds.y), out invalid));
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
