# 📚 Полная Документация Системы Инвентаря

## 📋 Навигация по Документации

### 🏠 [README.md](README.md) - Главная страница документации

### 📖 Основные Документы:
- **[ARCHITECTURE.md](ARCHITECTURE.md)** ← Вы здесь - Полное описание файлов и архитектуры
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Гайд по миграции старого кода
- **[SETUP_EXAMPLES.md](SETUP_EXAMPLES.md)** - Пошаговые примеры настройки объектов
- **[COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md)** - Краткий справочник по компонентам
- **[VISUAL_DIAGRAMS.md](VISUAL_DIAGRAMS.md)** - Визуальные диаграммы и flow-схемы
- **[FAQ.md](FAQ.md)** - Частые вопросы и решения проблем

### 🎯 Быстрые Ссылки:
- [Описание Файлов](#описание-файлов) - Что делает каждый файл
- [Состояние Объектов](#состояние-объектов) - Как должны быть настроены объекты
- [Архитектура Системы](#архитектура-системы) - Как компоненты взаимодействуют
- [Примеры Настройки](SETUP_EXAMPLES.md) - Пошаговые инструкции
- [Справочник Компонентов](COMPONENTS_REFERENCE.md) - Быстрая шпаргалка

---

## 📄 Описание Файлов

### Основные Компоненты (MonoBehaviour)

#### 1. **InventoryManager.cs**
**Путь**: `Assets/Script/InventoryScript/InventoryManager.cs`

**Тип**: MonoBehaviour (Manager)

**Ответственность**:
- Единый центр управления всей системой инвентаря
- Обработка пользовательского ввода (мышь, клавиатура)
- Координация процесса перетаскивания предметов
- Определение целей размещения (ячейки, слоты экипировки)
- Управление состоянием drag & drop

**Ключевые Методы**:
- `TryStartDrag()` - начало перетаскивания
- `TryEndDrag()` - завершение перетаскивания
- `UpdateDragPosition()` - обновление позиции во время перетаскивания
- `GetTargetUnderMouse()` - поиск объекта под курсором

**Использует**:
- `GraphicRaycaster` - для UI raycast
- `EventSystem` - для работы с событиями Unity UI
- `ItemInstance` - для управления перетаскиваемым предметом

**Должен быть на**: Canvas или дочернем объекте Canvas

---

#### 2. **ItemInstance.cs**
**Путь**: `Assets/Script/InventoryScript/ItemInstance.cs`

**Тип**: MonoBehaviour (Координатор)

**Ответственность**:
- Координация между данными предмета (ItemState) и визуализацией (ItemView)
- Управление логикой предмета (поворот, размещение)
- Делегирование проверок размещения в ItemPlacementValidator
- Управление занятыми ячейками
- Обработка команд от InventoryManager

**Ключевые Методы**:
- `OnDragStart()` - начало перетаскивания предмета
- `OnDragEnd()` - завершение перетаскивания
- `UpdateDragPosition()` - обновление позиции при drag
- `RequestRotate()` - поворот предмета (клавиша R)
- `CanPlaceInGrid()` - проверка возможности размещения
- `PlaceInGrid()` - размещение в сетке
- `ReturnToStart()` - возврат на исходную позицию
- `GetStateData()` - получение данных для экипировки

**Использует**:
- `ItemState` - данные предмета
- `ItemView` - визуализация
- `InitializeGrid` - сетка ячеек предмета
- `ItemPlacementValidator` - валидация размещения

**Должен быть на**: Префабе предмета

---

#### 3. **ItemState.cs**
**Путь**: `Assets/Script/InventoryScript/ItemState.cs`

**Тип**: MonoBehaviour (Data Container)

**Ответственность**:
- Хранение изменяемых данных предмета в runtime
- Управление текущим состоянием (стак, прочность)
- Генерация уникальных ID
- Предоставление данных для сохранения/загрузки

**Поля**:
- `ItemData data` - ссылка на ScriptableObject с неизменяемыми данными
- `int currentStack` - текущее количество в стаке
- `float currentDurability` - текущая прочность
- `string uniqueId` - уникальный идентификатор экземпляра

**Ключевые Методы**:
- `IsValid()` - проверка валидности состояния
- `GetStateData()` - создание копии данных (структура)
- `SetStateData()` - применение сохраненных данных

**Использует**:
- `ItemData` - ScriptableObject
- `ItemStateData` - структура для передачи данных

**Должен быть на**: 
- Префабе предмета
- Контейнере инвентаря (Backpack)

---

#### 4. **ItemView.cs**
**Путь**: `Assets/Script/InventoryScript/ItemView.cs`

**Тип**: MonoBehaviour (View/Presenter)

**Ответственность**:
- Визуальное представление предмета
- Воспроизведение анимаций (движение, поворот, тряска)
- Управление прозрачностью при перетаскивании
- Управление иконкой предмета
- Управление CanvasGroup для Raycast

**Поля**:
- `CanvasGroup canvasGroup` - для управления прозрачностью
- `Image iconImage` - иконка предмета
- `RectTransform iconRect` - для поворота иконки
- `float moveDuration` - длительность анимации движения
- `float rotationDuration` - длительность анимации поворота
- `float shakeDuration` - длительность анимации тряски

**Ключевые Методы**:
- `UpdateIcon()` - обновление иконки
- `StartDragVisuals()` - визуальное начало перетаскивания
- `EndDragVisuals()` - визуальное окончание перетаскивания
- `SmoothMoveTo()` - плавное перемещение
- `SmoothRotateTo()` - плавный поворот
- `PlayShakeEffect()` - эффект тряски

**Должен быть на**: Префабе предмета (добавляется автоматически ItemInstance)

---

#### 5. **EquipmentSlot.cs**
**Путь**: `Assets/Script/InventoryScript/EquipmentSlot.cs`

**Тип**: MonoBehaviour (Slot Manager)

**Ответственность**:
- Управление слотами экипировки
- Проверка совместимости категорий предметов
- Хранение данных экипированного предмета
- Создание/уничтожение экземпляров предметов
- Обновление визуализации слота

**Поля**:
- `ItemCategory allowedCategories` - разрешенные категории (флаги)
- `Image iconImage` - компонент для отображения иконки
- `GameObject iconObject` - объект с иконкой
- `ItemStateData currentItemData` - данные экипированного предмета
- `bool hasItem` - флаг наличия предмета

**Ключевые Методы**:
- `CanEquip()` - проверка совместимости категории
- `EquipItem()` - экипировка предмета
- `UnequipItem()` - снятие предмета (создает ItemInstance)
- `UpdateVisuals()` - обновление иконки слота
- `AcceptsCategory()` - проверка категории

**Должен быть на**: GameObject слота экипировки

---

#### 6. **InventoryCell.cs** (ранее Grid.cs)
**Путь**: `Assets/Script/InventoryScript/Grid.cs`

**Тип**: MonoBehaviour (Cell Data)

**Ответственность**:
- Представление одной ячейки инвентаря (1x1)
- Хранение ссылки на занимающий предмет
- Хранение координат в сетке
- Предоставление информации о занятости

**Поля**:
- `GameObject fullness` - ссылка на предмет, занимающий ячейку
- `Vector2Int gridSize` - координаты ячейки в сетке (x, y)

**Свойства**:
- `bool IsOccupied` - быстрая проверка занятости (fullness != null)

**Ключевые Методы**:
- `Clear()` - очистка ячейки

**Должен быть на**: 
- Каждой ячейке инвентаря (генерируется InitializeGrid)
- Каждой ячейке предмета (генерируется InitializeGrid)

---

#### 7. **InitializeGrid.cs**
**Путь**: `Assets/Script/InventoryScript/InitializeGrid.cs`

**Тип**: MonoBehaviour (Grid Generator)

**Ответственность**:
- Генерация сетки ячеек для контейнеров и предметов
- Создание "коврика" из ячеек 1x1
- Управление списком созданных ячеек
- Предоставление утилит для поиска ячеек по координатам

**Поля**:
- `GameObject prefabGrid` - префаб одной ячейки
- `Vector2Int gridSize` - размер сетки (читается из ItemData)
- `List<GameObject> listGrid` - список всех созданных ячеек

**Ключевые Методы**:
- `GenerateGrid()` - создание сетки в Awake()
- `ClearGrid()` - очистка сетки
- `GetDimensions()` - получение размеров сетки
- `GetCellAt()` - получение ячейки по координатам (GameObject)
- `GetGridAt()` - получение ячейки по координатам (InventoryCell)

**Должен быть на**:
- Дочернем объекте контейнера (GridContainer)
- Дочернем объекте предмета (ItemGridContainer)

---

#### 8. **InventoryGrid.cs**
**Путь**: `Assets/Script/InventoryScript/InventoryContainer.cs`

**Тип**: MonoBehaviour (Filter)

**Ответственность**:
- Фильтрация типов предметов для контейнеров
- Определение, какие категории предметов можно поместить
- Побитовая проверка категорий

**Поля**:
- `ItemCategory allowedTypes` - разрешенные типы (флаги)

**Ключевые Методы**:
- `CanAcceptItem()` - проверка, можно ли поместить предмет данной категории

**Должен быть на**: Контейнере инвентаря (Backpack, Chest, etc.)

---

### Вспомогательные Классы (Non-MonoBehaviour)

#### 9. **ItemPlacementValidator.cs**
**Путь**: `Assets/Script/InventoryScript/ItemPlacementValidator.cs`

**Тип**: Static Helper Class

**Ответственность**:
- Проверка возможности размещения предметов в сетке
- Вычисление позиций предмета с учетом его размера
- Поиск альтернативных позиций для размещения
- Управление занятостью ячеек (OccupyCells/FreeCells)

**Ключевые Методы**:
- `CanPlaceItem()` - проверка возможности размещения
- `TryFindNearbyPosition()` - поиск ближайшей свободной позиции
- `OccupyCells()` - пометить ячейки как занятые
- `FreeCells()` - освободить ячейки
- `ValidateInputs()` - валидация входных данных
- `GetItemCells()` - получение ячеек предмета
- `ComputeDesiredPositions()` - вычисление желаемых позиций
- `ArePositionsWithinBounds()` - проверка границ
- `GetInventoryGridsForPositions()` - получение ячеек инвентаря
- `AreCellsFree()` - проверка свободности ячеек

**Используется**:
- `ItemInstance` - для проверок размещения
- `PlacementData` - для возврата результатов

---

#### 10. **PlacementData.cs**
**Путь**: `Assets/Script/InventoryScript/ItemPlacementValidator.cs` (в том же файле)

**Тип**: Class (Data Container)

**Ответственность**:
- Хранение данных о размещении предмета
- Передача информации между валидатором и ItemInstance

**Поля**:
- `GameObject targetCell` - целевая ячейка
- `List<InventoryCell> targetGrids` - все занимаемые ячейки инвентаря
- `List<InventoryCell> itemCells` - ячейки предмета
- `Vector2Int offset` - смещение
- `InitializeGrid inventoryInit` - ссылка на сетку инвентаря

---

### Данные (ScriptableObject & Structs)

#### 11. **ItemData.cs**
**Путь**: `Assets/Script/InventoryScript/ItemData.cs`

**Тип**: ScriptableObject

**Ответственность**:
- Хранение неизменяемых данных предмета (шаблон)
- Определение характеристик предмета
- Используется как ассет в Unity Project

**Поля**:
- `string itemName` - название предмета
- `Sprite icon` - иконка для UI
- `ItemCategory category` - категория предмета
- `Vector2Int size` - размер в ячеках (ширина, высота)
- `int maxStackSize` - максимальный размер стака
- `float maxDurability` - максимальная прочность
- `GameObject modelPrefab` - префаб 3D модели

**Создание**: `Right Click → Create → Inventory → Item Data`

---

#### 12. **ItemStateData.cs**
**Путь**: `Assets/Script/InventoryScript/ItemState.cs` (в том же файле)

**Тип**: Struct (Serializable)

**Ответственность**:
- Передача данных состояния между компонентами
- Сохранение/загрузка данных предмета
- Копирование состояния при экипировке

**Поля**:
- `ItemData data` - ссылка на ScriptableObject
- `int currentStack` - текущий стак
- `float currentDurability` - текущая прочность
- `string uniqueId` - уникальный ID

---

#### 13. **ItemCategory.cs**
**Путь**: `Assets/Script/InventoryScript/ItemCategory.cs`

**Тип**: Enum (Flags)

**Ответственность**:
- Определение категорий предметов
- Побитовые операции для фильтрации

**Значения**:
```csharp
None = 0
Weapon = 1 << 0
LightWeapon = 1 << 1
SecondaryWeapons = 1 << 2
Helmet = 1 << 3
Armor = 1 << 4
Container = 1 << 5
Food = 1 << 6
Medicine = 1 << 7
Backpack = 1 << 8
All = ~0
```

---

## 🏗️ Архитектура Системы

### Иерархия Зависимостей

```
InventoryManager (Orchestrator)
    ↓ управляет
ItemInstance (Coordinator)
    ↓ использует
┌───┴────┬────────────┬──────────────┐
↓        ↓            ↓              ↓
ItemState  ItemView  ItemPlacement  InitializeGrid
    ↓                 Validator
ItemData
```

### Паттерны Проектирования

1. **MVC (Model-View-Controller)**
   - Model: `ItemState`, `ItemData`
   - View: `ItemView`
   - Controller: `ItemInstance`, `InventoryManager`

2. **Strategy Pattern**
   - `ItemPlacementValidator` - стратегия размещения

3. **Observer Pattern** (потенциально)
   - Можно добавить события для расширяемости

4. **Data Transfer Object (DTO)**
   - `ItemStateData`, `PlacementData`

---

## 🎨 Состояние Объектов До Старта

### 1. **Scene Root**

```
Scene
├── Canvas
├── EventSystem
├── [Prefabs будут созданы в runtime]
└── [Другие игровые объекты]
```

---

### 2. **Canvas** ⭐

**Компоненты**:
```
✅ Canvas
   - Render Mode: Screen Space - Overlay (или Camera)
   - Pixel Perfect: false

✅ CanvasScaler
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920x1080

✅ GraphicRaycaster ⚠️ ОБЯЗАТЕЛЬНО
   - Ignore Reversed Graphics: true
   - Blocking Objects: None

✅ InventoryManager (опционально на Canvas или дочернем объекте)
   [SerializeField] Canvas targetCanvas = this
```
**Дочерние объекты**: InventoryPanel, EquipmentPanel, и т.д.

---

### 3. **EventSystem** ⭐

**Компоненты**:
```
✅ EventSystem
   - First Selected: None

✅ StandaloneInputModule
   - Horizontal Axis: Horizontal
   - Vertical Axis: Vertical
   - Submit Button: Submit
   - Cancel Button: Cancel
```
⚠️ **Должен быть ОДИН в сцене!**

---

### 4. **InventoryPanel** (пример контейнера)

```
InventoryPanel (GameObject)
├── BackpackContainer
│   ├── ItemState ⚠️
│   ├── InventoryGrid
│   ├── Image (фон)
│   └── GridContainer
│       ├── InitializeGrid
│       └── GridLayoutGroup (auto)
└── [другие контейнеры]
```

#### **BackpackContainer**

**Компоненты**:
```
✅ RectTransform
   - Anchors: центр или по дизайну
   - Width/Height: по дизайну

✅ ItemState ⚠️ ВАЖНО
   [Public] ItemData data = ContainerData (ScriptableObject с size)
   [Public] int currentStack = 1
   [Public] float currentDurability = 100

✅ InventoryGrid
   [Public] ItemCategory allowedTypes = All (или Weapon | Food | ...)

✅ Image (опционально)
   - Sprite: фон рюкзака
   - Color: по дизайну

✅ CanvasGroup (опционально)
   - Alpha: 1
   - Interactable: true
   - Block Raycasts: true
```
**Layer**: Default или UI

---

#### **GridContainer** (дочерний объект BackpackContainer)

**Компоненты**:
```
✅ RectTransform
   - Anchors: stretch
   - Offsets: по дизайну (отступы от краев)

✅ InitializeGrid
   [SerializeField] GameObject prefabGrid = CellPrefab
   [Public] List<GameObject> listGrid = [] (заполняется в runtime)

✅ GridLayoutGroup (добавляется автоматически)
   - Cell Size: (50, 50) или по дизайну
   - Spacing: (2, 2)
   - Start Corner: Upper Left
   - Start Axis: Horizontal
   - Child Alignment: Upper Left
   - Constraint: Fixed Column Count (или Fixed Row Count)
```
**Дочерние объекты**: Создаются автоматически в `Awake()` InitializeGrid

---

### 5. **EquipmentPanel** (слоты экипировки)

```
EquipmentPanel (GameObject)
├── WeaponSlot
├── HelmetSlot
├── ArmorSlot
└── BackpackSlot
```

#### **WeaponSlot** (пример)

**Компоненты**:
```
✅ RectTransform
   - Width/Height: (100, 100) или по дизайну

✅ EquipmentSlot
   [SerializeField] ItemCategory allowedCategories = Weapon | LightWeapon
   [SerializeField] Image iconImage = ссылка на Image в IconObject
   [SerializeField] GameObject iconObject = ссылка на IconObject
   [Private] ItemStateData currentItemData = default
   [Private] bool hasItem = false

✅ Image (фон слота)
   - Sprite: рамка слота
   - Color: по дизайну

✅ CanvasGroup (опционально)
   - Alpha: 1
   - Interactable: true
   - Block Raycasts: true
```
**Layer**: `7 (Equipment)` ⚠️ ОБЯЗАТЕЛЬНО

**Дочерние объекты**:
```
└── IconObject
    ├── RectTransform
    │   - Anchors: stretch
    │   - Offsets: (5, -5, 5, -5) отступы
    └── Image (иконка)
        - Sprite: None (назначается в runtime)
        - Color: (1, 1, 1, 1)
        - Raycast Target: false
        - Preserve Aspect: true
```
---

### 6. **Префаб Ячейки (CellPrefab)**

**Путь**: `Assets/Prefabs/UI/CellPrefab.prefab`

**Компоненты**:
```
✅ RectTransform
   - Width: 50
   - Height: 50

✅ InventoryCell
   [Public] GameObject fullness = null
   [Public] Vector2Int gridSize = (0, 0) (назначается в runtime)

✅ Image (опционально, для визуализации)
   - Sprite: None или клетка
   - Color: (1, 1, 1, 0.1) - почти прозрачный
   - Raycast Target: true ⚠️ ВАЖНО

✅ CanvasGroup (опционально)
   - Alpha: 1
   - Interactable: true
   - Block Raycasts: true
```
**Layer**: Наследуется от родителя (6 для предметов, Default для инвентаря)

---

### 7. **Префаб Предмета (Item_XXX)**

**Путь**: `Assets/Prefabs/Items/Item_AK47.prefab`

```
Item_AK47 (GameObject)
├── ItemState
├── ItemInstance
├── ItemView (auto)
├── CanvasGroup
├── Image
└── ItemGridContainer
    ├── InitializeGrid
    └── [ячейки создаются в runtime]
```

#### **Item_AK47** (корневой объект)

**Компоненты**:
```
✅ RectTransform
   - Width/Height: определяется спрайтом
   - Pivot: (0.5, 0.5)

✅ ItemState
   [Public] ItemData data = ItemData_AK47 (ScriptableObject)
   [Public] int currentStack = 1
   [Public] float currentDurability = 100
   [Public] string uniqueId = "" (генерируется в Awake)

✅ ItemInstance
   [Private] ItemState itemState = (авто)
   [Private] ItemView itemView = (авто)
   [Private] InitializeGrid itemGrid = (авто)
   [Private] bool isRotated = false
   [Private] List<InventoryCell> occupiedCells = []


✅ ItemView (добавляется автоматически, если отсутствует)
   [SerializeField] CanvasGroup canvasGroup = (авто)
   [SerializeField] Image iconImage = null (опционально)
   [SerializeField] RectTransform iconRect = null
   [SerializeField] float moveDuration = 0.15
   [SerializeField] float rotationDuration = 0.2
   [SerializeField] float shakeDuration = 0.25

✅ CanvasGroup ⚠️ ОБЯЗАТЕЛЬНО
   - Alpha: 1
   - Interactable: true
   - Block Raycasts: true

✅ Image (иконка предмета)
   - Sprite: AK47_Icon (из ItemData.icon)
   - Color: (1, 1, 1, 1)
   - Raycast Target: true ⚠️
   - Preserve Aspect: true
```
**Layer**: `6 (Items)` ⚠️ ОБЯЗАТЕЛЬНО (и все дочерние тоже!)

---

#### **ItemGridContainer** (дочерний объект)

**Компоненты**:
```
✅ RectTransform
   - Anchors: center
   - Pivot: (0.5, 0.5)

✅ InitializeGrid
   [SerializeField] GameObject prefabGrid = CellPrefab
   [Private] Vector2Int gridSize = (2, 3) (читается из ItemData)
   [Public] List<GameObject> listGrid = [] (заполняется в runtime)

✅ GridLayoutGroup (добавляется автоматически)
   - Cell Size: (50, 50)
   - Spacing: (0, 0)
   - Child Alignment: Middle Center
```
**Layer**: `6 (Items)` ⚠️

**Дочерние ячейки** (создаются в runtime):
```
├── Cell_0_0 (InventoryCell, Image с alpha=0)
├── Cell_0_1
├── Cell_0_2
├── Cell_1_0
├── Cell_1_1
└── Cell_1_2
```
Все ячейки предмета должны быть **невидимыми** (Image.color.a = 0)

---

### 8. **ScriptableObject: ItemData**

**Путь**: `Assets/Data/Items/ItemData_AK47.asset`

**Создание**: `Right Click → Create → Inventory → Item Data`

**Настройка**:
```
Item Name: "AK-47"
Icon: [Sprite] AK47_Icon
Category: Weapon
Size: (2, 3)
Max Stack Size: 1
Max Durability: 100
Model Prefab: Item_AK47 (ссылка на префаб)
```

**Для контейнеров** (Backpack):
```
Item Name: "Military Backpack"
Icon: [Sprite] Backpack_Icon
Category: Container | Backpack
Size: (5, 7) ⚠️ Размер сетки инвентаря
Max Stack Size: 1
Max Durability: 100
Model Prefab: None (контейнеры не имеют модели)
```
---

## 📊 Диаграмма Состояний До Старта

### Canvas
```
Canvas
├── Components:
│   ├── Canvas (Render Mode: Screen Space - Overlay)
│   ├── CanvasScaler (UI Scale Mode: Scale With Screen Size)
│   ├── GraphicRaycaster ⚠️
│   └── InventoryManager (опционально)
│
└── Children:
    ├── InventoryPanel
    │   └── BackpackContainer
    │       ├── ItemState ⚠️ (data = ContainerData)
    │       ├── InventoryGrid (allowedTypes = All)
    │       └── GridContainer
    │           └── InitializeGrid (prefabGrid = CellPrefab)
    │
    └── EquipmentPanel
        ├── WeaponSlot
        │   ├── EquipmentSlot (allowedCategories = Weapon)
        │   ├── Image (Layer 7)
        │   └── IconObject → Image
        │
        └── [другие слоты...]
```
### EventSystem
```
EventSystem (один в сцене)
├── EventSystem
└── StandaloneInputModule
```
### Prefabs (не в сцене, в Project)
```
Assets/Prefabs/
├── UI/
│   └── CellPrefab.prefab
│       ├── InventoryCell
│       └── Image (alpha = 0 для предметов, 0.1 для инвентаря)
│
└── Items/
    └── Item_AK47.prefab (Layer 6)
        ├── ItemState (data = ItemData_AK47)
        ├── ItemInstance
        ├── CanvasGroup ⚠️
        ├── Image (sprite = AK47_Icon)
        └── ItemGridContainer (Layer 6)
            └── InitializeGrid (prefabGrid = CellPrefab)
```
### ScriptableObjects (ассеты)
```
Assets/Data/Items/
├── ItemData_AK47.asset
│   - Category: Weapon
│   - Size: (2, 3)
│   - Model Prefab: Item_AK47
│
├── ItemData_Backpack.asset
│   - Category: Container
│   - Size: (5, 7) ⚠️ размер сетки
│   - Model Prefab: None
│
└── [другие предметы...]
```
---

## ⚡ Runtime Состояния

### Когда предмет создается (Instantiate)

```
newItem = Instantiate(ItemData.modelPrefab, parent)
├── ItemState.Awake()
│   └── Генерирует uniqueId
│
├── ItemInstance.Awake()
│   ├── Получает ItemState
│   ├── Создает ItemView (если нет)
│   └── Получает InitializeGrid
│
├── ItemView.Awake()
│   └── Получает/создает CanvasGroup
│
└── InitializeGrid.Awake()
    ├── Читает size из ItemState.data
    └── Генерирует сетку ячеек
        ├── Cell_0_0 (InventoryCell, gridSize=(0,0))
        ├── Cell_0_1 (InventoryCell, gridSize=(0,1))
        └── ...
```
### Когда игрок начинает перетаскивание

```
InventoryManager.TryStartDrag()
├── Находит ItemInstance под курсором
├── Вызывает ItemInstance.OnDragStart()
│   ├── Сохраняет startPosition, startParent
│   ├── Освобождает ячейки (FreeCells)
│   └── Вызывает ItemView.StartDragVisuals()
│       ├── canvasGroup.blocksRaycasts = false
│       ├── canvasGroup.alpha = 0.8
│       └── transform.SetAsLastSibling()
│
└── Устанавливает draggedItem = ItemInstance
```
### Когда игрок отпускает предмет

```
InventoryManager.TryEndDrag()
├── Находит цель под курсором
│
├── Если цель = EquipmentSlot
│   ├── Проверяет CanEquip()
│   ├── Вызывает EquipSlot.EquipItem(ItemInstance)
│   │   ├── Сохраняет ItemStateData
│   │   ├── Обновляет iconImage
│   │   └── Destroy(ItemInstance)
│   └── Success!
│
├── Если цель = InventoryCell
│   ├── ItemPlacementValidator.CanPlaceItem()
│   │   ├── Проверяет категорию
│   │   ├── Проверяет размер
│   │   ├── Проверяет границы
│   │   ├── Проверяет свободность ячеек
│   │   └── Возвращает PlacementData
│   │
│   ├── ItemInstance.PlaceInGrid(target)
│   │   ├── Вычисляет targetLocalPos
│   │   ├── ItemView.SmoothMoveTo(targetLocalPos)
│   │   └── OccupyCells(occupiedCells)
│   └── Success!
│
└── Иначе (неудача)
    └── ItemInstance.ReturnToStart()
        ├── ItemView.PlayShakeEffect()
        ├── ItemView.SmoothMoveTo(startPosition)
        └── OccupyCells (на старом месте)
```
---

## 📋 Checklist Полной Настройки

### Scene Setup
- [ ] Canvas с GraphicRaycaster
- [ ] InventoryManager на Canvas
- [ ] EventSystem в сцене (один)
- [ ] InventoryPanel с контейнерами
- [ ] EquipmentPanel со слотами

### Контейнеры (Backpack, Chest, ...)

- [ ] ItemState с ContainerData (ScriptableObject)
- [ ] InventoryGrid с allowedTypes
- [ ] GridContainer с InitializeGrid
- [ ] InitializeGrid.prefabGrid = CellPrefab
- [ ] Layer = Default или UI

### Слоты Экипировки
- [ ] EquipmentSlot компонент
- [ ] allowedCategories настроены
- [ ] iconImage и iconObject назначены
- [ ] Image (фон) есть
- [ ] IconObject → Image (иконка) есть
- [ ] Layer = 7 (Equipment) ⚠️

### Префабы Предметов
- [ ] ItemState с ItemData
- [ ] ItemInstance компонент
- [ ] CanvasGroup компонент ⚠️
- [ ] Image с иконкой
- [ ] ItemGridContainer → InitializeGrid
- [ ] Layer = 6 (Items) ⚠️ (включая всех детей!)

### ScriptableObjects
- [ ] ItemData создан для каждого предмета
- [ ] Icon назначен
- [ ] Category установлена
- [ ] Size корректный
- [ ] modelPrefab ссылается на префаб

### Префаб Ячейки
- [ ] InventoryCell компонент
- [ ] Image компонент
- [ ] Raycast Target = true ⚠️

---

## 🎯 Важные Моменты

### ⚠️ Критически Важно:
1. **GraphicRaycaster** на Canvas
2. **EventSystem** в сцене
3. **CanvasGroup** на каждом предмете
4. **Layer 6** на предметах
5. **Layer 7** на слотах экипировки
6. **ItemState** на контейнерах (с данными о размере)
7. **prefabGrid** назначен в InitializeGrid

### 💡 Рекомендации:
1. Создавайте ItemData перед префабами
2. Используйте именование: `Item_XXX`, `ItemData_XXX`
3. Храните префабы в `Assets/Prefabs/Items/`
4. Храните данные в `Assets/Data/Items/`
5. Тестируйте по одному предмету за раз

---

Теперь у вас есть полная документация всех файлов, их ответственности и состояний объектов! 🚀
