# 📐 Визуальные Диаграммы Системы

Текстовые диаграммы для понимания архитектуры системы инвентаря.

---

## 🎯 Иерархия Зависимостей

```
┌─────────────────────────────────────────────────────┐
│                  InventoryManager                   │
│              (Orchestrator / Manager)               │
│  • Обработка ввода                                  │
│  • Координация drag & drop                          │
│  • Поиск целей размещения                           │
└──────────────────┬──────────────────────────────────┘
                   │ управляет
                   ↓
┌─────────────────────────────────────────────────────┐
│                   ItemInstance                      │
│              (Coordinator / Controller)             │
│  • Координация логики предмета                      │
│  • Делегирование проверок                           │
│  • Управление состоянием                            │
└──┬──────────┬──────────┬──────────┬─────────────────┘
   │          │          │          │
   ↓          ↓          ↓          ↓
┌─────┐  ┌────────┐  ┌────────┐  ┌──────────────┐
│Item │  │Item    │  │Item    │  │Initialize    │
│State│  │View    │  │Place   │  │Grid          │
│     │  │        │  │ment    │  │              │
│Data │  │Visual  │  │Vali    │  │Grid          │
│     │  │        │  │dator   │  │Generator     │
└──┬──┘  └────────┘  └────────┘  └──────────────┘
   │
   ↓
┌─────────────┐
│  ItemData   │
│ (ScriptableO│
│  bject)     │
└─────────────┘
```

---

## 🔄 Процесс Перетаскивания

### Полный Flow Drag & Drop

```
┌─────────────────────────────────────────────────────────────┐
│  ФАЗА 1: НАЧАЛО ПЕРЕТАСКИВАНИЯ                              │
└─────────────────────────────────────────────────────────────┘

User: Mouse Down на предмете
    │
    ↓
InventoryManager.TryStartDrag()
    │
    ├─→ GraphicRaycaster.Raycast()
    │       └─→ Находит ItemInstance
    │
    ├─→ ItemInstance.OnDragStart()
    │       │
    │       ├─→ Сохранить startPosition
    │       ├─→ Сохранить startParent
    │       ├─→ ItemPlacementValidator.FreeCells(occupiedCells)
    │       │       └─→ cell.fullness = null
    │       │
    │       └─→ ItemView.StartDragVisuals()
    │               ├─→ canvasGroup.blocksRaycasts = false
    │               ├─→ canvasGroup.alpha = 0.8
    │               └─→ transform.SetAsLastSibling()
    │
    └─→ draggedItem = ItemInstance


┌─────────────────────────────────────────────────────────────┐
│  ФАЗА 2: ПРОЦЕСС ПЕРЕТАСКИВАНИЯ                             │
└─────────────────────────────────────────────────────────────┘

User: Mouse Move
    │
    ↓
InventoryManager.Update()
    │
    └─→ ItemInstance.UpdateDragPosition(mousePos)
            │
            └─→ transform.position = mousePos


┌─────────────────────────────────────────────────────────────┐
│  ФАЗА 3A: РАЗМЕЩЕНИЕ В ИНВЕНТАРЬ                            │
└─────────────────────────────────────────────────────────────┘

User: Mouse Up над InventoryCell
    │
    ↓
InventoryManager.TryEndDrag()
    │
    ├─→ GraphicRaycaster.Raycast()
    │       └─→ Находит InventoryCell (targetCell)
    │
    ├─→ ItemPlacementValidator.CanPlaceItem()
    │       │
    │       ├─→ ValidateInputs() ✓
    │       ├─→ InventoryGrid.CanAcceptItem(category) ✓
    │       ├─→ GetItemCells() → itemCells[]
    │       ├─→ ComputeDesiredPositions() → positions[]
    │       ├─→ ArePositionsWithinBounds() ✓
    │       ├─→ GetInventoryGridsForPositions() → targetGrids[]
    │       ├─→ AreCellsFree() ✓
    │       └─→ Return PlacementData
    │
    ├─→ ItemInstance.PlaceInGrid(targetCell)
    │       │
    │       ├─→ Вычислить targetLocalPos
    │       ├─→ ItemView.SmoothMoveTo(targetLocalPos)
    │       │       └─→ Coroutine: lerp position
    │       │
    │       └─→ ItemPlacementValidator.OccupyCells(targetGrids)
    │               └─→ foreach: cell.fullness = thisItem
    │
    └─→ draggedItem = null ✓ SUCCESS


┌─────────────────────────────────────────────────────────────┐
│  ФАЗА 3B: ЭКИПИРОВКА                                        │
└─────────────────────────────────────────────────────────────┘

User: Mouse Up над EquipmentSlot
    │
    ↓
InventoryManager.TryEndDrag()
    │
    ├─→ GraphicRaycaster.Raycast()
    │       └─→ Находит EquipmentSlot (Layer 7)
    │
    ├─→ EquipmentSlot.CanEquip(ItemInstance.category)
    │       └─→ (allowedCategories & category) != 0 ✓
    │
    ├─→ EquipmentSlot.EquipItem(ItemInstance)
    │       │
    │       ├─→ ItemInstance.GetStateData() → ItemStateData
    │       ├─→ currentItemData = ItemStateData
    │       ├─→ hasItem = true
    │       ├─→ UpdateVisuals()
    │       │       ├─→ iconImage.sprite = data.icon
    │       │       └─→ iconObject.SetActive(true)
    │       │
    │       └─→ Destroy(ItemInstance.gameObject)
    │
    └─→ draggedItem = null ✓ SUCCESS


┌─────────────────────────────────────────────────────────────┐
│  ФАЗА 3C: НЕУДАЧНОЕ РАЗМЕЩЕНИЕ                              │
└─────────────────────────────────────────────────────────────┘

User: Mouse Up над недопустимой целью
    │
    ↓
InventoryManager.TryEndDrag()
    │
    ├─→ GraphicRaycaster.Raycast()
    │       └─→ Находит что-то НЕподходящее
    │
    ├─→ ItemPlacementValidator.CanPlaceItem() ✗
    │       └─→ Return false (причина: ...)
    │
    ├─→ ItemInstance.ReturnToStart()
    │       │
    │       ├─→ ItemView.PlayShakeEffect()
    │       │       └─→ Coroutine: тряска
    │       │
    │       ├─→ ItemView.SmoothMoveTo(startPosition)
    │       │       └─→ Coroutine: lerp обратно
    │       │
    │       └─→ ItemPlacementValidator.OccupyCells(occupiedCells)
    │               └─→ Восстановить занятость
    │
    └─→ draggedItem = null ✗ FAILED
```

---

## 🏗️ Архитектура Классов

### MonoBehaviour Компоненты

```
┌──────────────────────┐
│  InventoryManager    │  Canvas/GameObject
├──────────────────────┤
│ - targetCanvas       │
│ - draggedItem        │
│ - isDragging         │
├──────────────────────┤
│ + TryStartDrag()     │
│ + TryEndDrag()       │
│ + GetTargetUnder..() │
└──────────────────────┘

┌──────────────────────┐
│   ItemInstance       │  Префаб предмета
├──────────────────────┤
│ - itemState          │──┐
│ - itemView           │  │
│ - itemGrid           │  │
│ - startPosition      │  │
│ - occupiedCells      │  │
├──────────────────────┤  │
│ + OnDragStart()      │  │
│ + OnDragEnd()        │  │
│ + RequestRotate()    │  │
│ + PlaceInGrid()      │  │
│ + ReturnToStart()    │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│     ItemState        │←─┘  Префаб предмета / Контейнер
├──────────────────────┤
│ + data: ItemData     │──┐
│ + currentStack       │  │
│ + currentDurability  │  │
│ + uniqueId           │  │
├──────────────────────┤  │
│ + IsValid()          │  │
│ + GetStateData()     │  │
│ + SetStateData()     │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│     ItemView         │  │  Префаб предмета
├──────────────────────┤  │
│ - canvasGroup        │  │
│ - iconImage          │  │
│ - iconRect           │  │
├──────────────────────┤  │
│ + StartDragVisuals() │  │
│ + EndDragVisuals()   │  │
│ + SmoothMoveTo()     │  │
│ + SmoothRotateTo()   │  │
│ + PlayShakeEffect()  │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│   InitializeGrid     │  │  GridContainer
├──────────────────────┤  │
│ - prefabGrid         │  │
│ + listGrid           │  │
├──────────────────────┤  │
│ + GenerateGrid()     │  │
│ + GetCellAt()        │  │
│ + GetGridAt()        │  │
│ + GetDimensions()    │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│   InventoryGrid      │  │  Контейнер
├──────────────────────┤  │
│ + allowedTypes       │  │
├──────────────────────┤  │
│ + CanAcceptItem()    │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│   EquipmentSlot      │  │  Слот экипировки
├──────────────────────┤  │
│ - allowedCategories  │  │
│ - iconImage          │  │
│ - iconObject         │  │
│ - currentItemData    │  │
│ - hasItem            │  │
├──────────────────────┤  │
│ + CanEquip()         │  │
│ + EquipItem()        │  │
│ + UnequipItem()      │  │
│ + UpdateVisuals()    │  │
└──────────────────────┘  │
                          │
┌──────────────────────┐  │
│   InventoryCell      │  │  Ячейка (Grid.cs)
├──────────────────────┤  │
│ + fullness: GameObj  │  │
│ + gridSize: Vector2  │  │
├──────────────────────┤  │
│ + IsOccupied         │  │
│ + Clear()            │  │
└──────────────────────┘  │
                          │
                          │
┌──────────────────────┐  │
│      ItemData        │←─┘  ScriptableObject
├──────────────────────┤
│ + itemName           │
│ + icon: Sprite       │
│ + category           │
│ + size: Vector2Int   │
│ + maxStackSize       │
│ + maxDurability      │
│ + modelPrefab        │
└──────────────────────┘
```

### Static Helpers & Structs

```
┌──────────────────────────┐
│ ItemPlacementValidator   │  Static Class
├──────────────────────────┤
│ + CanPlaceItem()         │
│ + TryFindNearbyPosition()│
│ + OccupyCells()          │
│ + FreeCells()            │
│ - ValidateInputs()       │
│ - GetItemCells()         │
│ - ComputeDesiredPos()    │
│ - ArePositionsWithin...()│
│ - GetInventoryGridsFor() │
│ - AreCellsFree()         │
└──────────────────────────┘
         │ возвращает
         ↓
┌──────────────────────────┐
│     PlacementData        │  Class
├──────────────────────────┤
│ + targetCell             │
│ + targetGrids[]          │
│ + itemCells[]            │
│ + offset                 │
│ + inventoryInit          │
└──────────────────────────┘

┌──────────────────────────┐
│    ItemStateData         │  Struct
├──────────────────────────┤
│ + data: ItemData         │
│ + currentStack           │
│ + currentDurability      │
│ + uniqueId               │
└──────────────────────────┘

┌──────────────────────────┐
│     ItemCategory         │  Enum (Flags)
├──────────────────────────┤
│ None = 0                 │
│ Weapon = 1               │
│ LightWeapon = 2          │
│ SecondaryWeapons = 4     │
│ Helmet = 8               │
│ Armor = 16               │
│ Container = 32           │
│ Food = 64                │
│ Medicine = 128           │
│ Backpack = 256           │
│ All = ~0                 │
└──────────────────────────┘
```

---

## 🎨 Структура Сцены (Иерархия)

```
Scene
├── Canvas
│   ├── [Canvas]
│   ├── [CanvasScaler]
│   ├── [GraphicRaycaster] ⚠️ ОБЯЗАТЕЛЬНО
│   ├── [InventoryManager]
│   │
│   ├── InventoryPanel
│   │   │
│   │   └── BackpackContainer
│   │       ├── [ItemState] ⚠️ (data = ContainerData)
│   │       ├── [InventoryGrid] (allowedTypes)
│   │       ├── [Image] (фон)
│   │       │
│   │       └── GridContainer
│   │           ├── [RectTransform]
│   │           ├── [InitializeGrid] (prefabGrid)
│   │           ├── [GridLayoutGroup] (auto)
│   │           │
│   │           └── [Cells created at runtime]
│   │               ├── Cell_0_0
│   │               │   ├── [InventoryCell]
│   │               │   └── [Image]
│   │               ├── Cell_0_1
│   │               │   └── ...
│   │               └── Cell_4_6
│   │
│   └── EquipmentPanel
│       │
│       ├── WeaponSlot (Layer 7) ⚠️
│       │   ├── [RectTransform]
│       │   ├── [EquipmentSlot]
│       │   ├── [Image] (фон слота)
│       │   │
│       │   └── IconObject
│       │       ├── [RectTransform]
│       │       └── [Image] (иконка предмета)
│       │
│       ├── HelmetSlot (Layer 7) ⚠️
│       │   └── [структура как WeaponSlot]
│       │
│       └── ArmorSlot (Layer 7) ⚠️
│           └── [структура как WeaponSlot]
│
├── EventSystem ⚠️ ОДИН В СЦЕНЕ
│   ├── [EventSystem]
│   └── [StandaloneInputModule]
│
└── [Items spawned at runtime]
    └── Item_AK47_Instance (Layer 6) ⚠️
        ├── [RectTransform]
        ├── [ItemState] (data = ItemData_AK47)
        ├── [ItemInstance]
        ├── [ItemView]
        ├── [CanvasGroup] ⚠️ ОБЯЗАТЕЛЬНО
        ├── [Image] (иконка)
        │
        └── ItemGridContainer (Layer 6) ⚠️
            ├── [RectTransform]
            ├── [InitializeGrid] (prefabGrid)
            ├── [GridLayoutGroup] (auto)
            │
            └── [Cells created at runtime]
                ├── Cell_0_0 (invisible)
                ├── Cell_0_1 (invisible)
                ├── Cell_0_2 (invisible)
                ├── Cell_1_0 (invisible)
                ├── Cell_1_1 (invisible)
                └── Cell_1_2 (invisible)
```

---

## 📦 Структура Префабов

### CellPrefab (ячейка)

```
CellPrefab
├── [RectTransform]
│   ├── Width: 50
│   └── Height: 50
│
├── [InventoryCell]
│   ├── fullness: null
│   └── gridSize: (0,0)
│
└── [Image]
    ├── Sprite: None
    ├── Color: (1,1,1,0.1) прозрачный
    └── Raycast Target: ☑ true ⚠️
```

### Item_AK47 (предмет 2x3)

```
Item_AK47 (Layer 6) ⚠️
├── [RectTransform]
│   ├── Width: 100
│   ├── Height: 150
│   └── Pivot: (0.5, 0.5)
│
├── [ItemState]
│   ├── data: ItemData_AK47 ⚠️
│   ├── currentStack: 1
│   ├── currentDurability: 100
│   └── uniqueId: ""
│
├── [ItemInstance]
│   └── (все поля private, авто-заполняются)
│
├── [ItemView]
│   ├── canvasGroup: (авто)
│   ├── moveDuration: 0.15
│   ├── rotationDuration: 0.2
│   └── shakeDuration: 0.25
│
├── [CanvasGroup] ⚠️ ОБЯЗАТЕЛЬНО
│   ├── Alpha: 1
│   ├── Interactable: true
│   └── Block Raycasts: true
│
├── [Image]
│   ├── Sprite: AK47_Icon
│   ├── Color: (1,1,1,1)
│   ├── Raycast Target: ☑ true ⚠️
│   └── Preserve Aspect: true
│
└── ItemGridContainer (Layer 6) ⚠️
    ├── [RectTransform]
    ├── [InitializeGrid]
    │   ├── prefabGrid: CellPrefab ⚠️
    │   └── listGrid: (заполняется в runtime)
    │
    ├── [GridLayoutGroup] (добавляется авто)
    │   ├── Cell Size: (50, 50)
    │   ├── Spacing: (0, 0)
    │   └── Child Alignment: Middle Center
    │
    └── [Runtime Generated]
        ├── Cell_0_0 (InventoryCell + Image alpha=0)
        ├── Cell_0_1
        ├── Cell_0_2
        ├── Cell_1_0
        ├── Cell_1_1
        └── Cell_1_2
```

---

## 🔀 Диаграмма Размещения Предмета

### Вычисление Позиций

```
Предмет 2x3:
┌────┬────┐
│ 0,0│ 1,0│  Item cells (relative coords)
├────┼────┤
│ 0,1│ 1,1│  Middle cell = (1,1) ← center
├────┼────┤
│ 0,2│ 1,2│
└────┴────┘

Инвентарь 5x7:
┌───┬───┬───┬───┬───┐
│0,0│1,0│2,0│3,0│4,0│
├───┼───┼───┼───┼───┤
│0,1│1,1│2,1│3,1│4,1│  Target cell = (3,2) ← mouse drop
├───┼───┼───┼───┼───┤
│0,2│1,2│2,2│3,2│4,2│
├───┼───┼───┼───┼───┤
│0,3│1,3│2,3│3,3│4,3│
├───┼───┼───┼───┼───┤
│0,4│1,4│2,4│3,4│4,4│
├───┼───┼───┼───┼───┤
│0,5│1,5│2,5│3,5│4,5│
├───┼───┼───┼───┼───┤
│0,6│1,6│2,6│3,6│4,6│
└───┴───┴───┴───┴───┘

Вычисление:
middleCell = itemCells[3] = (1,1)  // центр предмета
targetCell = (3,2)                 // где мышка
offset = targetCell - middleCell = (3,2) - (1,1) = (2,1)

Позиции предмета в инвентаре:
Cell (0,0) → (0,0) + (2,1) = (2,1) ✓
Cell (1,0) → (1,0) + (2,1) = (3,1) ✓
Cell (0,1) → (0,1) + (2,1) = (2,2) ✓
Cell (1,1) → (1,1) + (2,1) = (3,2) ✓ (target)
Cell (0,2) → (0,2) + (2,1) = (2,3) ✓
Cell (1,2) → (1,2) + (2,1) = (3,3) ✓

Результат в инвентаре:
┌───┬───┬───┬───┬───┐
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │ X │ X │   │  X = занято предметом
├───┼───┼───┼───┼───┤
│   │   │ X │ X │   │
├───┼───┼───┼───┼───┤
│   │   │ X │ X │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
└───┴───┴───┴───┴───┘
```

---

## 🎛️ Состояния Компонентов

### ItemInstance State Machine

```
┌──────────────┐
│   IDLE       │  Предмет лежит в инвентаре
└──────┬───────┘
       │ OnDragStart()
       ↓
┌──────────────┐
│  DRAGGING    │  Предмет перетаскивается
└──────┬───────┘
       │ OnDragEnd()
       ↓
   ┌───┴────┐
   │        │
   ↓        ↓
┌──────┐ ┌──────┐
│PLACE │ │EQUIP │ или RETURN
└──────┘ └──────┘
```

### EquipmentSlot State Machine

```
┌───────────┐
│   EMPTY   │  Нет предмета (hasItem = false)
└─────┬─────┘
      │ EquipItem()
      ↓
┌───────────┐
│  EQUIPPED │  Предмет экипирован (hasItem = true)
└─────┬─────┘
      │ UnequipItem()
      ↓
┌───────────┐
│   EMPTY   │  Предмет создан в инвентаре
└───────────┘
```

### InventoryCell States

```
Free (fullness = null)
  ↓ OccupyCells()
Occupied (fullness = GameObject)
  ↓ FreeCells()
Free (fullness = null)
```

---

## 🧩 Паттерны Проектирования

### 1. MVC (Model-View-Controller)

```
Model (Data):
  - ItemState (runtime data)
  - ItemData (static data)

View (Presentation):
  - ItemView (animations, visuals)

Controller (Logic):
  - ItemInstance (coordinates)
  - InventoryManager (orchestrates)
```

### 2. Strategy Pattern

```
ItemPlacementValidator ← Strategy для проверки размещения
  - CanPlaceItem() ← разные стратегии размещения
  - TryFindNearbyPosition() ← альтернативная стратегия
```

### 3. Observer (потенциально)

```
События для расширяемости:
  - OnItemEquipped
  - OnItemUnequipped
  - OnItemPlaced
  - OnItemRemoved
```

### 4. Data Transfer Object (DTO)

```
ItemStateData ← DTO для передачи данных между компонентами
PlacementData ← DTO для результатов валидации
```

### 5. Facade

```
InventoryManager ← Facade для всей системы инвентаря
  - Скрывает сложность взаимодействия компонентов
  - Предоставляет простой API
```

---

## 🎨 Layers & Raycasting

### Layer Configuration

```
Layer 0: Default
  - Фоны
  - Панели

Layer 5: UI
  - Обычные UI элементы

Layer 6: Items ⚠️
  - Все префабы предметов
  - ItemGridContainer
  - Все дочерние объекты

Layer 7: Equipment ⚠️
  - Слоты экипировки
  - IconObject

Layer 8: InventoryCells (опционально)
  - Ячейки инвентаря
```

### Raycast Order

```
InventoryManager.GetTargetUnderMouse():

GraphicRaycaster.Raycast()
  ↓
1. Проверка Layer 6 (Items)
   └─→ Находит ItemInstance → можно перетаскивать
   
2. Проверка Layer 7 (Equipment)
   └─→ Находит EquipmentSlot → можно экипировать
   
3. Проверка Default/UI (InventoryCells)
   └─→ Находит InventoryCell → можно разместить

4. Ничего не найдено
   └─→ Return null → неудача
```

---

## ⚙️ Coroutines Flow

### SmoothMoveTo Coroutine

```
ItemView.SmoothMoveTo(targetPos)
  ↓
StartCoroutine(SmoothMove())
  ↓
  ├─ elapsed = 0
  ├─ startPos = transform.localPosition
  │
  └─ while (elapsed < moveDuration):
      ├─ t = elapsed / moveDuration
      ├─ newPos = Vector3.Lerp(startPos, targetPos, t)
      ├─ transform.localPosition = newPos
      ├─ elapsed += Time.deltaTime
      └─ yield return null
  ↓
Done (предмет на месте)
```

### PlayShakeEffect Coroutine

```
ItemView.PlayShakeEffect()
  ↓
StartCoroutine(ShakeEffect())
  ↓
  ├─ originalPos = transform.localPosition
  ├─ elapsed = 0
  │
  └─ while (elapsed < shakeDuration):
      ├─ t = elapsed / shakeDuration
      ├─ offset = Random.insideUnitCircle * shake * (1 - t)
      ├─ transform.localPosition = originalPos + offset
      ├─ elapsed += Time.deltaTime
      └─ yield return null
  ↓
transform.localPosition = originalPos (вернуть)
Done
```

---

## 🔍 Отладочные Точки

### Breakpoint Locations

```csharp
// Начало drag
InventoryManager.TryStartDrag()
  → ItemInstance.OnDragStart() ← BREAKPOINT

// Проверка размещения
ItemPlacementValidator.CanPlaceItem()
  → if (!ValidateInputs(...)) ← BREAKPOINT
  → if (!InventoryGrid.CanAcceptItem(...)) ← BREAKPOINT
  → if (!ArePositionsWithinBounds(...)) ← BREAKPOINT
  → if (!AreCellsFree(...)) ← BREAKPOINT

// Размещение
ItemInstance.PlaceInGrid()
  → ItemPlacementValidator.OccupyCells(...) ← BREAKPOINT

// Экипировка
EquipmentSlot.EquipItem()
  → currentItemData = stateData ← BREAKPOINT
  → UpdateVisuals() ← BREAKPOINT
```

---

Эти диаграммы помогут визуально понять работу системы! 📐
