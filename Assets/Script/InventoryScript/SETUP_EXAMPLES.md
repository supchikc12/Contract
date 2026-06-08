# 🎨 Примеры Настройки Объектов

## 📋 Содержание
1. [Пример 1: Настройка Canvas](#пример-1-настройка-canvas)
2. [Пример 2: Создание Контейнера](#пример-2-создание-контейнера)
3. [Пример 3: Создание Слота Экипировки](#пример-3-создание-слота-экипировки)
4. [Пример 4: Создание Предмета](#пример-4-создание-предмета)
5. [Пример 5: Настройка Категорий](#пример-5-настройка-категорий)

---

## Пример 1: Настройка Canvas

### Шаг за Шагом

#### 1. Создание Canvas
```
Hierarchy → Right Click → UI → Canvas
```

#### 2. Настройка компонентов Canvas

**Canvas Component:**
```
Inspector → Canvas
├── Render Mode: Screen Space - Overlay
├── Pixel Perfect: ☐ (false)
└── Sort Order: 0
```

**CanvasScaler Component:**
```
Inspector → Canvas Scaler
├── UI Scale Mode: Scale With Screen Size
├── Reference Resolution: 1920 x 1080
├── Screen Match Mode: Match Width Or Height
├── Match: 0.5
└── Reference Pixels Per Unit: 100
```

**GraphicRaycaster Component:** (должен быть автоматически)
```
Inspector → Graphic Raycaster
├── Ignore Reversed Graphics: ☑ (true)
├── Blocking Objects: None
└── Blocking Mask: Everything
```

#### 3. Добавление InventoryManager

**Вариант A: На сам Canvas**
```
Canvas → Add Component → InventoryManager

Inspector → InventoryManager
└── Target Canvas: перетащите Canvas сюда
```

**Вариант B: На дочерний объект**
```
Canvas → Right Click → Create Empty
└── Rename to "InventorySystem"
    └── Add Component → InventoryManager
        └── Target Canvas: перетащите Canvas сюда
```

---

## Пример 2: Создание Контейнера

### Сценарий: Военный Рюкзак (5x7 ячеек)

#### Шаг 1: Создать ItemData для контейнера

```
Project → Right Click → Create → Inventory → Item Data
Rename: "ItemData_MilitaryBackpack"

Inspector:
├── Item Name: "Military Backpack"
├── Icon: [Перетащите спрайт рюкзака]
├── Category: Container | Backpack
├── Size: (5, 7) ⚠️ Это размер СЕТКИ внутри
├── Max Stack Size: 1
├── Max Durability: 100
└── Model Prefab: None (пока)
```

#### Шаг 2: Создать объект контейнера в UI

```
Canvas → Right Click → UI → Panel
Rename: "InventoryPanel"
├── Anchors: center
├── Width: 600
├── Height: 600
└── Color: (0.2, 0.2, 0.2, 0.9) темный полупрозрачный

InventoryPanel → Right Click → UI → Panel
Rename: "BackpackContainer"
├── Anchors: stretch (заполняет родителя)
├── Left: 10, Right: -10, Top: -10, Bottom: 10
└── Color: (0.1, 0.1, 0.1, 1) темный
```

#### Шаг 3: Добавить компоненты на BackpackContainer

**ItemState:**
```
Add Component → ItemState

Inspector:
├── Data: ItemData_MilitaryBackpack ⚠️ ВАЖНО
├── Current Stack: 1
├── Current Durability: 100
└── Unique Id: (пусто, генерируется автоматически)
```

**InventoryGrid:**
```
Add Component → InventoryGrid

Inspector:
└── Allowed Types: 
    ☑ Weapon
    ☑ LightWeapon  
    ☑ SecondaryWeapons
    ☑ Food
    ☑ Medicine
    ☐ Container (нельзя класть рюкзак в рюкзак!)
    или просто выбрать "All"
```

#### Шаг 4: Создать GridContainer

```
BackpackContainer → Right Click → Create Empty
Rename: "GridContainer"

Inspector → RectTransform:
├── Anchors: stretch
├── Left: 10, Right: -10, Top: -10, Bottom: 10
└── Pivot: (0.5, 0.5)
```

**Добавить InitializeGrid:**
```
GridContainer → Add Component → InitializeGrid

Inspector:
└── Prefab Grid: CellPrefab ⚠️ (перетащите из Project)
    (Если нет CellPrefab, создайте - см. далее)
```

#### Шаг 5: Создать CellPrefab (если нет)

```
Hierarchy → Create Empty
Rename: "CellPrefab"

Add Component → RectTransform
├── Width: 50
└── Height: 50

Add Component → InventoryCell
├── Fullness: None
└── Grid Size: (0, 0)

Add Component → Image
├── Sprite: None или квадрат
├── Color: (1, 1, 1, 0.1) почти прозрачный
└── Raycast Target: ☑ true ⚠️

Project → Перетащите CellPrefab в Assets/Prefabs/UI/
Delete из Hierarchy
```

#### Результат

```
Canvas
└── InventoryPanel
    └── BackpackContainer
        ├── ItemState (data = ItemData_MilitaryBackpack)
        ├── InventoryGrid (allowedTypes = All)
        └── GridContainer
            ├── InitializeGrid (prefabGrid = CellPrefab)
            └── [35 ячеек будут созданы в runtime]
                ├── Cell_0_0
                ├── Cell_0_1
                ├── ...
                └── Cell_4_6
```

---

## Пример 3: Создание Слота Экипировки

### Сценарий: Слот для Оружия

#### Шаг 1: Создать панель экипировки

```
Canvas → Right Click → UI → Panel
Rename: "EquipmentPanel"
├── Anchors: top-right
├── Pos X: -150, Pos Y: -100
├── Width: 250
├── Height: 600
└── Color: (0.15, 0.15, 0.15, 0.95)
```

#### Шаг 2: Создать слот

```
EquipmentPanel → Right Click → UI → Image
Rename: "WeaponSlot"
├── Width: 100
├── Height: 100
├── Color: (0.3, 0.3, 0.3, 1) серый фон
└── Sprite: слот рамка (опционально)

⚠️ ВАЖНО: Change Layer to "Equipment" (7)
```

#### Шаг 3: Добавить компонент EquipmentSlot

```
WeaponSlot → Add Component → EquipmentSlot

Inspector:
└── Allowed Categories:
    ☑ Weapon
    ☑ LightWeapon
    ☐ SecondaryWeapons
    ☐ все остальные
```

#### Шаг 4: Создать объект для иконки

```
WeaponSlot → Right Click → UI → Image
Rename: "IconObject"

Inspector → RectTransform:
├── Anchors: stretch
├── Left: 5, Right: -5, Top: -5, Bottom: 5
└── Pivot: (0.5, 0.5)

Inspector → Image:
├── Sprite: None (будет назначаться в runtime)
├── Color: (1, 1, 1, 1)
├── Raycast Target: ☐ false
└── Preserve Aspect: ☑ true
```

#### Шаг 5: Связать иконку со слотом

```
WeaponSlot → Inspector → EquipmentSlot
├── Icon Image: перетащите Image из IconObject
└── Icon Object: перетащите сам IconObject
```

#### Результат

```
EquipmentPanel (Layer: Default)
└── WeaponSlot (Layer: 7 Equipment) ⚠️
    ├── EquipmentSlot
    │   ├── allowedCategories = Weapon | LightWeapon
    │   ├── iconImage = IconObject.Image
    │   └── iconObject = IconObject
    ├── Image (фон)
    └── IconObject
        └── Image (иконка)
```

---

## Пример 4: Создание Предмета

### Сценарий: AK-47 (размер 2x3)

#### Шаг 1: Создать ItemData

```
Project → Right Click → Create → Inventory → Item Data
Rename: "ItemData_AK47"

Inspector:
├── Item Name: "AK-47"
├── Icon: AK47_Icon (спрайт)
├── Category: Weapon
├── Size: (2, 3) ⚠️ 2 ширина, 3 высота
├── Max Stack Size: 1
├── Max Durability: 100
└── Model Prefab: None (назначим позже)
```

#### Шаг 2: Создать GameObject в сцене

```
Hierarchy → Create Empty
Rename: "Item_AK47"

Inspector → RectTransform:
├── Width: 100 (по размеру спрайта)
├── Height: 150
└── Pivot: (0.5, 0.5)

⚠️ ВАЖНО: Change Layer to "Items" (6)
```

#### Шаг 3: Добавить компоненты

**ItemState:**
```
Add Component → ItemState

Inspector:
├── Data: ItemData_AK47 ⚠️
├── Current Stack: 1
├── Current Durability: 100
└── Unique Id: (пусто)
```

**ItemInstance:**
```
Add Component → ItemInstance
(Все поля private, настроек нет)
```

**CanvasGroup:** ⚠️ **КРИТИЧЕСКИ ВАЖНО**
```
Add Component → CanvasGroup

Inspector:
├── Alpha: 1
├── Interactable: ☑ true
└── Block Raycasts: ☑ true
```

**Image:**
```
Add Component → Image

Inspector:
├── Sprite: AK47_Icon (тот же, что в ItemData)
├── Color: (1, 1, 1, 1)
├── Raycast Target: ☑ true ⚠️
└── Preserve Aspect: ☑ true
```

#### Шаг 4: Создать сетку предмета

```
Item_AK47 → Right Click → Create Empty
Rename: "ItemGridContainer"

Inspector → RectTransform:
├── Anchors: center
├── Pos: (0, 0, 0)
└── Scale: (1, 1, 1)

⚠️ ВАЖНО: Layer должен быть "Items" (6)
```

**Добавить InitializeGrid:**
```
ItemGridContainer → Add Component → InitializeGrid

Inspector:
└── Prefab Grid: CellPrefab ⚠️
```

#### Шаг 5: Сохранить как префаб

```
Item_AK47 (в Hierarchy) → Перетащите в Project
Папка: Assets/Prefabs/Items/Item_AK47.prefab

Delete Item_AK47 из Hierarchy
```

#### Шаг 6: Связать с ItemData

```
Project → ItemData_AK47 → Inspector
└── Model Prefab: Item_AK47 (перетащите префаб)
```

#### Результат - Префаб

```
Item_AK47.prefab (Layer: 6 Items) ⚠️
├── ItemState (data = ItemData_AK47)
├── ItemInstance
├── CanvasGroup ⚠️
├── Image (sprite = AK47_Icon)
└── ItemGridContainer (Layer: 6) ⚠️
    └── InitializeGrid (prefabGrid = CellPrefab)
        └── [6 ячеек создаются в runtime]
            ├── Cell_0_0 (invisible)
            ├── Cell_0_1 (invisible)
            ├── Cell_0_2 (invisible)
            ├── Cell_1_0 (invisible)
            ├── Cell_1_1 (invisible)
            └── Cell_1_2 (invisible)
```

---

## Пример 5: Настройка Категорий

### Сценарий: Разные типы слотов

#### Слот для Основного Оружия

```
WeaponSlot → EquipmentSlot
└── Allowed Categories:
    ☑ Weapon (автоматы, винтовки)
    ☐ LightWeapon
    ☐ SecondaryWeapons
```

**Подходят**: AK-47, M4A1, Sniper Rifle  
**НЕ подходят**: Пистолеты, Ножи, Еда

---

#### Слот для Вторичного Оружия

```
SecondaryWeaponSlot → EquipmentSlot
└── Allowed Categories:
    ☐ Weapon
    ☑ LightWeapon (пистолеты)
    ☑ SecondaryWeapons (ножи)
```

**Подходят**: Glock, Desert Eagle, Knife  
**НЕ подходят**: Автоматы, Еда

---

#### Универсальный Слот

```
UniversalSlot → EquipmentSlot
└── Allowed Categories:
    ☑ All (выбрать все)
```

**Подходят**: Все предметы

---

#### Слот Шлема

```
HelmetSlot → EquipmentSlot
└── Allowed Categories:
    ☐ Weapon
    ☐ все остальное
    ☑ Helmet (только)
```

**Подходят**: Helmet_Military, Helmet_Civilian  
**НЕ подходят**: Все остальное

---

### Настройка ItemData для разных категорий

#### Оружие

```
ItemData_AK47
└── Category: Weapon

ItemData_Glock
└── Category: LightWeapon

ItemData_Knife
└── Category: SecondaryWeapons
```

#### Броня

```
ItemData_HelmetMilitary
└── Category: Helmet

ItemData_VestMilitary
└── Category: Armor
```

#### Предметы

```
ItemData_MedKit
└── Category: Medicine

ItemData_Bread
└── Category: Food
```

#### Контейнеры

```
ItemData_MilitaryBackpack
└── Category: Container | Backpack

ItemData_MedicalBag
└── Category: Container (только)
```

---

## 🎯 Готовые Шаблоны

### Шаблон: Слот для Любого Типа

```yaml
GameObject: "GenericSlot"
Layer: 7 (Equipment)
Components:
  - RectTransform:
      Width: 80
      Height: 80
  - EquipmentSlot:
      allowedCategories: [выберите нужные]
      iconImage: IconObject.Image
      iconObject: IconObject
  - Image:
      Color: (0.3, 0.3, 0.3, 1)
Children:
  - IconObject:
      - Image (sprite = None, Preserve Aspect = true)
```

### Шаблон: Предмет Размером 1x1

```yaml
GameObject: "Item_SmallItem"
Layer: 6 (Items)
Components:
  - ItemState:
      data: [ItemData с size=(1,1)]
  - ItemInstance
  - CanvasGroup
  - Image:
      Sprite: [иконка]
      Raycast Target: true
Children:
  - ItemGridContainer (Layer 6):
      - InitializeGrid:
          prefabGrid: CellPrefab
```

### Шаблон: Большой Предмет (2x3)

```yaml
GameObject: "Item_LargeItem"
Layer: 6 (Items)
Components:
  - ItemState:
      data: [ItemData с size=(2,3)]
  - ItemInstance
  - CanvasGroup
  - Image:
      Sprite: [большая иконка]
      Raycast Target: true
Children:
  - ItemGridContainer (Layer 6):
      - InitializeGrid:
          prefabGrid: CellPrefab
      [Runtime: создаст 6 ячеек 2x3]
```

---

## 📋 Checklist для Каждого Объекта

### ✅ Canvas
- [ ] Canvas component
- [ ] CanvasScaler
- [ ] GraphicRaycaster ⚠️
- [ ] InventoryManager (на Canvas или дочернем)

### ✅ Контейнер (Backpack)
- [ ] ItemState с ContainerData
- [ ] InventoryGrid с allowedTypes
- [ ] GridContainer с InitializeGrid
- [ ] prefabGrid назначен

### ✅ Слот Экипировки
- [ ] Layer = 7 (Equipment) ⚠️
- [ ] EquipmentSlot компонент
- [ ] allowedCategories настроены
- [ ] iconImage назначен
- [ ] iconObject назначен
- [ ] IconObject существует

### ✅ Префаб Предмета
- [ ] Layer = 6 (Items) ⚠️ (всё дерево!)
- [ ] ItemState с ItemData
- [ ] ItemInstance
- [ ] CanvasGroup ⚠️
- [ ] Image с Raycast Target = true
- [ ] ItemGridContainer → InitializeGrid
- [ ] prefabGrid назначен

### ✅ ItemData (ScriptableObject)
- [ ] Item Name заполнено
- [ ] Icon назначен
- [ ] Category выбрана
- [ ] Size корректный
- [ ] modelPrefab ссылается на префаб

### ✅ CellPrefab
- [ ] InventoryCell компонент
- [ ] Image с Raycast Target = true
- [ ] Размер 50x50 (или ваш размер)

---

## 🚀 Быстрый Старт

### Минимальная Настройка (5 минут)

1. **Создать Canvas** + InventoryManager
2. **Создать EventSystem** (если нет)
3. **Создать CellPrefab**
4. **Создать 1 ItemData** (тестовый предмет)
5. **Создать 1 Префаб предмета**
6. **Создать 1 Контейнер** (рюкзак)
7. **Создать 1 Слот** (оружие)
8. **Тест**: Instantiate предмет в рюкзаке

---

Эти примеры помогут вам быстро настроить любые объекты системы инвентаря! 🎨
