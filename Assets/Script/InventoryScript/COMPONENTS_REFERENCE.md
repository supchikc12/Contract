# 📖 Краткий Справочник по Компонентам

Быстрая шпаргалка для разработчиков системы инвентаря.

---

## 🎯 MonoBehaviour Компоненты

### InventoryManager
**Где**: Canvas (или дочерний объект)  
**Цель**: Центральный управляющий компонент  
**Поля**:
```csharp
[SerializeField] private Canvas targetCanvas;
```
**Ключевые методы**:
- `TryStartDrag()` - начать перетаскивание
- `TryEndDrag()` - завершить перетаскивание
- `GetTargetUnderMouse()` - найти цель под курсором

**Подписывается на**: `Input.GetMouseButtonDown/Up(0)`

---

### ItemInstance
**Где**: Префаб предмета  
**Цель**: Координатор логики предмета  
**Auto-references**:
```csharp
private ItemState itemState;
private ItemView itemView;
private InitializeGrid itemGrid;
```
**Ключевые методы**:
- `OnDragStart()` - начало drag
- `OnDragEnd(target)` - конец drag
- `RequestRotate()` - поворот (клавиша R)
- `CanPlaceInGrid()` - проверка размещения
- `PlaceInGrid()` - размещение
- `ReturnToStart()` - возврат

**Вызывается из**: `InventoryManager`

---

### ItemState
**Где**: Префаб предмета, Контейнер  
**Цель**: Хранение данных runtime  
**Поля**:
```csharp
public ItemData data;
public int currentStack = 1;
public float currentDurability = 100f;
public string uniqueId;
```
**Ключевые методы**:
- `IsValid()` - проверка валидности
- `GetStateData()` - копия данных (struct)
- `SetStateData()` - применить данные

**В Awake**: генерирует `uniqueId`

---

### ItemView
**Где**: Префаб предмета (auto)  
**Цель**: Визуализация  
**Поля**:
```csharp
[SerializeField] private CanvasGroup canvasGroup;
[SerializeField] private Image iconImage;
[SerializeField] private RectTransform iconRect;
[SerializeField] private float moveDuration = 0.15f;
[SerializeField] private float rotationDuration = 0.2f;
[SerializeField] private float shakeDuration = 0.25f;
```
**Ключевые методы**:
- `StartDragVisuals()` - визуал начала drag
- `EndDragVisuals()` - визуал конца drag
- `SmoothMoveTo(pos)` - анимация движения
- `SmoothRotateTo(angle)` - анимация поворота
- `PlayShakeEffect()` - эффект тряски

**Вызывается из**: `ItemInstance`

---

### EquipmentSlot
**Где**: Слот экипировки  
**Цель**: Управление экипировкой  
**Поля**:
```csharp
[SerializeField] private ItemCategory allowedCategories;
[SerializeField] private Image iconImage;
[SerializeField] private GameObject iconObject;
private ItemStateData currentItemData;
private bool hasItem = false;
```
**Ключевые методы**:
- `CanEquip(category)` - проверка категории
- `EquipItem(ItemInstance)` - экипировка
- `UnequipItem()` - снятие (создает ItemInstance)
- `UpdateVisuals()` - обновление иконки
- `AcceptsCategory(category)` - проверка

**Layer**: `7 (Equipment)` ⚠️

---

### InventoryCell (Grid.cs)
**Где**: Ячейки инвентаря и предметов  
**Цель**: Данные одной ячейки  
**Поля**:
```csharp
public GameObject fullness; // ссылка на предмет
public Vector2Int gridSize; // координаты (x, y)
```
**Свойства**:
```csharp
public bool IsOccupied => fullness != null;
```
**Методы**:
- `Clear()` - очистить

**Создается**: `InitializeGrid` в runtime

---

### InitializeGrid
**Где**: GridContainer (дочерний объект контейнера/предмета)  
**Цель**: Генератор сетки  
**Поля**:
```csharp
[SerializeField] private GameObject prefabGrid; // CellPrefab
public List<GameObject> listGrid; // все ячейки
```
**Ключевые методы**:
- `GenerateGrid()` - создание в Awake
- `ClearGrid()` - очистка
- `GetDimensions()` - размер (Vector2Int)
- `GetCellAt(pos)` - GameObject ячейки
- `GetGridAt(pos)` - InventoryCell ячейки

**В Awake**: читает размер из `ItemState.data.size`

---

### InventoryGrid (InventoryContainer.cs)
**Где**: Контейнер инвентаря  
**Цель**: Фильтр типов  
**Поля**:
```csharp
public ItemCategory allowedTypes;
```
**Методы**:
- `CanAcceptItem(category)` - побитовая проверка

---

## 🔧 Вспомогательные Классы

### ItemPlacementValidator
**Тип**: Static класс  
**Цель**: Валидация размещения  
**Методы**:
```csharp
static bool CanPlaceItem(targetCell, itemGrid, inventoryContainer, itemData, out placementData)
static bool TryFindNearbyPosition(initialTarget, ..., out placementData)
static void OccupyCells(cells, item)
static void FreeCells(cells)
```

**Использование**:
```csharp
if (ItemPlacementValidator.CanPlaceItem(cell, grid, container, data, out var placement))
{
    // размещаем
}
```

---

### PlacementData
**Тип**: Класс (данные)  
**Цель**: Результат валидации  
**Поля**:
```csharp
public GameObject targetCell;
public List<InventoryCell> targetGrids;
public List<InventoryCell> itemCells;
public Vector2Int offset;
public InitializeGrid inventoryInit;
```

---

### ItemStateData
**Тип**: Struct  
**Цель**: Передача данных  
**Поля**:
```csharp
public ItemData data;
public int currentStack;
public float currentDurability;
public string uniqueId;
```

**Использование**:
```csharp
ItemStateData stateData = itemState.GetStateData();
equipmentSlot.EquipItem(stateData);
```

---

## 📦 ScriptableObject

### ItemData
**Тип**: ScriptableObject  
**Создание**: `Create → Inventory → Item Data`  
**Поля**:
```csharp
public string itemName;
public Sprite icon;
public ItemCategory category;
public Vector2Int size; // (width, height)
public int maxStackSize;
public float maxDurability;
public GameObject modelPrefab;
```

**Пример**:
```
ItemData_AK47:
- itemName: "AK-47"
- icon: AK47_Icon
- category: Weapon
- size: (2, 3)
- maxStackSize: 1
- maxDurability: 100
- modelPrefab: Item_AK47
```

---

## 🏷️ Enum

### ItemCategory
**Тип**: Enum (Flags)  
**Значения**:
```csharp
[Flags]
public enum ItemCategory
{
    None = 0,
    Weapon = 1 << 0,              // 1
    LightWeapon = 1 << 1,         // 2
    SecondaryWeapons = 1 << 2,    // 4
    Helmet = 1 << 3,              // 8
    Armor = 1 << 4,               // 16
    Container = 1 << 5,           // 32
    Food = 1 << 6,                // 64
    Medicine = 1 << 7,            // 128
    Backpack = 1 << 8,            // 256
    All = ~0                      // все биты
}
```

**Использование**:
```csharp
// Одна категория
category = ItemCategory.Weapon;

// Несколько категорий
allowedTypes = ItemCategory.Weapon | ItemCategory.Food;

// Проверка
if ((allowedTypes & itemCategory) != 0)
    Debug.Log("Разрешено");
```

---

## 🔗 Зависимости Компонентов

### Что требует что:

```
InventoryManager
├── требует: Canvas (GraphicRaycaster)
└── требует: EventSystem

ItemInstance
├── требует: ItemState
├── требует: InitializeGrid (дочерний)
└── создает: ItemView (auto)

ItemView
├── требует: CanvasGroup
├── опционально: Image
└── опционально: RectTransform (иконки)

EquipmentSlot
├── требует: Image (iconImage)
└── требует: GameObject (iconObject)

InitializeGrid
├── требует: GameObject (prefabGrid)
└── читает: ItemState.data.size

ItemState
└── требует: ItemData (ScriptableObject)

InventoryGrid
└── поля: ItemCategory allowedTypes
```

---

## 📋 Типичные Вызовы

### Начало Перетаскивания
```
User Input (mouse down)
    ↓
InventoryManager.TryStartDrag()
    ↓
InventoryManager.GetTargetUnderMouse()
    → GraphicRaycaster.Raycast()
    ↓
ItemInstance.OnDragStart()
    ↓
ItemPlacementValidator.FreeCells()
    ↓
ItemView.StartDragVisuals()
```

### Размещение Предмета
```
User Input (mouse up)
    ↓
InventoryManager.TryEndDrag()
    ↓
InventoryManager.GetTargetUnderMouse()
    → находит InventoryCell
    ↓
ItemPlacementValidator.CanPlaceItem()
    ├── ValidateInputs()
    ├── InventoryGrid.CanAcceptItem()
    ├── GetItemCells()
    ├── ComputeDesiredPositions()
    ├── ArePositionsWithinBounds()
    ├── GetInventoryGridsForPositions()
    └── AreCellsFree()
    ↓
ItemInstance.PlaceInGrid()
    ├── Вычисление targetLocalPos
    ├── ItemView.SmoothMoveTo()
    └── ItemPlacementValidator.OccupyCells()
```

### Экипировка
```
User Input (drop на EquipmentSlot)
    ↓
EquipmentSlot.CanEquip()
    ↓
EquipmentSlot.EquipItem(ItemInstance)
    ├── ItemInstance.GetStateData()
    ├── Сохранение ItemStateData
    ├── UpdateVisuals() - показ иконки
    └── Destroy(ItemInstance)
```

### Снятие Экипировки
```
User Input (click на EquipmentSlot)
    ↓
EquipmentSlot.UnequipItem()
    ├── Instantiate(ItemData.modelPrefab)
    ├── newItem.SetStateData(currentItemData)
    ├── UpdateVisuals() - скрыть иконку
    └── Return newItem (ItemInstance)
```

---

## ⚙️ Настройки Inspector

### Обязательные Ссылки:

**InventoryManager:**
```
Target Canvas: [перетащить Canvas]
```

**EquipmentSlot:**
```
Allowed Categories: [выбрать флаги]
Icon Image: [перетащить Image из IconObject]
Icon Object: [перетащить IconObject]
```

**ItemState:**
```
Data: [перетащить ItemData ScriptableObject]
```

**InitializeGrid:**
```
Prefab Grid: [перетащить CellPrefab]
```

**InventoryGrid:**
```
Allowed Types: [выбрать флаги]
```

---

## 🎨 Layers (Важно!)

```
Layer 6: Items
- Все префабы предметов
- Все дочерние объекты предметов
- ItemGridContainer и его ячейки

Layer 7: Equipment
- Все слоты экипировки
- Дочерние объекты слотов (IconObject)

Default или UI:
- Контейнеры инвентаря
- Ячейки инвентаря
- Панели, фоны
```

---

## 🚨 Частые Ошибки

### ❌ Нет GraphicRaycaster на Canvas
**Симптом**: Клики не работают  
**Решение**: Добавить GraphicRaycaster на Canvas

### ❌ Нет CanvasGroup на предмете
**Симптом**: Не работает прозрачность при drag  
**Решение**: Добавить CanvasGroup на префаб предмета

### ❌ Неправильный Layer
**Симптом**: Drag & drop не работает  
**Решение**: 
- Предметы → Layer 6
- Слоты → Layer 7

### ❌ Raycast Target = false на Image
**Симптом**: Клики проходят сквозь объект  
**Решение**: Включить Raycast Target на Image

### ❌ Не назначен prefabGrid
**Симптом**: Ошибка в Awake() InitializeGrid  
**Решение**: Назначить CellPrefab в Inspector

### ❌ ItemState без ItemData
**Симптом**: NullReferenceException  
**Решение**: Назначить ItemData ScriptableObject

### ❌ EquipmentSlot без iconImage/iconObject
**Симптом**: NullReferenceException при экипировке  
**Решение**: Создать IconObject и назначить ссылки

---

## 📊 Полезные Команды

### Получить размер сетки:
```csharp
Vector2Int size = initializeGrid.GetDimensions();
```

### Проверить занятость ячейки:
```csharp
if (inventoryCell.IsOccupied)
    Debug.Log("Занята");
```

### Получить ячейку по координатам:
```csharp
InventoryCell cell = initializeGrid.GetGridAt(new Vector2Int(2, 3));
```

### Проверить категорию:
```csharp
if (inventoryGrid.CanAcceptItem(ItemCategory.Weapon))
    Debug.Log("Можно поместить");
```

### Экипировать предмет:
```csharp
ItemStateData data = itemInstance.GetStateData();
equipmentSlot.EquipItem(data);
Destroy(itemInstance.gameObject);
```

### Создать предмет:
```csharp
GameObject newItem = Instantiate(itemData.modelPrefab, parent);
ItemInstance instance = newItem.GetComponent<ItemInstance>();
```

---

## 🔍 Отладка

### Проверить состояние drag:
```csharp
Debug.Log($"Dragging: {InventoryManager.instance.isDragging}");
Debug.Log($"Dragged Item: {InventoryManager.instance.draggedItem}");
```

### Проверить занятость ячеек:
```csharp
foreach (var cell in initializeGrid.listGrid)
{
    var invCell = cell.GetComponent<InventoryCell>();
    Debug.Log($"Cell {invCell.gridSize}: {invCell.IsOccupied}");
}
```

### Проверить размер предмета:
```csharp
Vector2Int itemSize = itemState.data.size;
int cellCount = itemGrid.listGrid.Count;
Debug.Log($"Size: {itemSize}, Cells: {cellCount}");
```

---

Этот справочник — ваша шпаргалка для быстрой работы с системой! 📖
