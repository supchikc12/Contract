# Contract — справочник по скриптам

Документ собран по фактическому коду каталога `Assets/Script` (25 файлов, ~3 642 строки).
Описывает назначение каждого класса, публичный API, связи между системами и подводные камни.

- Unity: 6000.6.8f1 (в `AGENTS.md` указана 6000.0.22f1 — фактическая версия выше)
- Язык: C#, без namespace, один публичный класс на файл (исключения: `ItemActions.cs`, `ItemState.cs`, `ItemPlacementValidator.cs`)
- UI: uGUI + TextMesh Pro
- Ввод: старый Input Manager (`Input.GetAxis`, `Input.GetKeyDown`)
- Сборка: без `.asmdef`, всё в `Assembly-CSharp` / `Assembly-CSharp-Editor`

---

## 1. Карта систем

```
ItemData (ScriptableObject, ассет)
   │  данные: имя, иконка, категория, размер, стак, прочность, префабы
   ▼
ItemState (MonoBehaviour, runtime-состояние)
   │  data, currentStack, currentDurability, uniqueId, containerItems
   ├──────────────► ItemInstance (поведение, drag&drop, размещение)
   │                     │
   │                     ├── ItemView (визуал: иконка, стак, анимации)
   │                     └── InitializeGrid (сетка ячеек предмета)
   │
   └──────────────► ItemStateData (struct, снимок для клонирования/выброса)

InventoryManager (singleton, координатор)
   ├── InventoryGrid (фильтр категорий для контейнера)
   ├── InitializeGrid → InventoryCell (ячейки 1×1)
   ├── EquipmentSlot → EquipmentSlotData (слоты экипировки)
   ├── ContextMenuUI → IItemAction (ПКМ-меню)
   └── CollectionZone (временное 2D-отображение предметов рядом)

PlayerManager (движение, камера, инвентарь, сбор предметов)
   ├── PlayerStamina (стамина: расход, реген, порог бега, стоимость прыжка)
   ├── PlayerStaminaBar (отладочный OnGUI-бар стамины)
   └── PlayerProceduralBody (человекоподобное тело и процедурные ноги)

ItemPlacementValidator (статическая валидация размещения)
ItemPrefabGenerator (Editor: генерация 2D/3D префабов из ItemData)
```

Поток владения:

| Слой | Кто хранит | Что хранит |
|---|---|---|
| Ассет | `ItemData` | неизменяемые параметры предмета |
| Сцена/префаб | `ItemState` | runtime-состояние конкретного экземпляра |
| Поведение | `ItemInstance` | ссылки, занятые ячейки, мировые источники |
| Отображение | `ItemView` | анимации, иконка, текст стака |
| Сетка | `InitializeGrid` + `InventoryCell` | какие координаты заняты каким объектом |

---

## 2. Модель данных

### 2.1 `ItemData` — `Assets/Script/Inventory/Items/ItemData.cs`

`ScriptableObject`, создаётся через `Create → Inventory → Item Data`.

| Поле | Тип | Смысл |
|---|---|---|
| `itemName` | string | имя предмета, используется и в путях генератора |
| `icon` | Sprite | иконка в инвентаре |
| `category` | `ItemCategory` | битовая маска категорий |
| `size` | `Vector2Int` | размер в ячейках: **x = строки (высота), y = столбцы (ширина)** |
| `containerSize` | `Vector2Int` | размер внутренней сетки, если предмет — контейнер |
| `maxStackSize` | int | максимум в стаке (1 = не стакается) |
| `maxDurability` | float | максимальная прочность |
| `modelPrefab3D` | GameObject | префаб для мира |
| `modelPrefab2D` | GameObject | префаб для инвентаря |

### 2.2 `ItemCategory` — `Inventory/Items/ItemCategory.cs`

`[System.Flags]`, значения: `None=0`, `Weapon=1<<0`, `LightWeapon=1<<1`, `SecondaryWeapons=1<<2`,
`Helmet=1<<3`, `Armor=1<<4`, `Container=1<<5`, `Food=1<<6`, `Medicine=1<<7`, `Backpack=1<<8`, `All=~0`.

Проверки везде побитовые: `(mask & value) != 0`.

### 2.3 `ItemState` — `Inventory/Items/ItemState.cs`

Runtime-состояние экземпляра.

| Поле | Тип | Смысл |
|---|---|---|
| `data` | `ItemData` | ссылка на ассет |
| `childrenUI` | GameObject | префаб 2D-версии (для мира) |
| `currentStack` | int | текущее количество |
| `currentDurability` | float | текущая прочность |
| `containerItems` | `List<ItemStateData>` | содержимое контейнера |
| `uniqueId` | string | GUID, генерируется в `Awake`, если пуст |

Методы:

- `bool IsValid()` — `data != null && currentStack > 0 && currentDurability > 0`
- `ItemStateData GetStateData()` — снимок (включая копию `containerItems`)
- `void SetStateData(ItemStateData)` — применение снимка
- `bool IsEmptyContainer()`

`ItemStateData` — `[Serializable] struct`: `data`, `currentStack`, `currentDurability`, `uniqueId`, `containerItems`.

> `containerItems` — вложенные `ItemStateData`. Рекурсивная структура, именно она даёт
> `Serialization depth limit 10 exceeded` при глубокой вложенности (см. раздел 8).

---

## 3. Потоки

### 3.1 Появление предмета в мире

1. В сцене лежит 3D-префаб: `MeshFilter`/`MeshRenderer`, `BoxCollider`, `ItemState`, слой `8` (Item).
2. `ItemState.data` указывает на `ItemData`, `ItemState.childrenUI` — на 2D-префаб.
3. `PlayerManager.OnTriggerEnter` добавляет объекты слоя 8 в `SelectedObj`, `OnTriggerExit` убирает.

### 3.2 Открытие инвентаря и «коллекционная зона»

`PlayerManager.Update` → `I` → `OpenOrCloseInventory()`:

- открытие: `Inventory.SetActive(true)`, курсор разблокирован, `InventoryManager.AddCollectionZone(SelectedObj)`;
- закрытие: `Inventory.SetActive(false)`, курсор захвачен, `InventoryManager.ReplaceCollectionZone(SelectedObj)`.

`AddCollectionZone`:

1. Для каждого 3D-объекта берёт `ItemState` и его `childrenUI` (2D-префаб).
2. Сначала пытается долить стак в уже существующие 2D-экземпляры той же `ItemData` в `CollectionZone`.
3. Остаток инстанциирует в `viewport` найденного `ScrollRectNoDrag`, вызывает `ItemInstance.Init()`,
   задаёт стак и `WorldSourceObjects`, затем ищет свободную ячейку через `TryPlaceInGrid`.
4. Если не влезло — `Destroy(item)`.

`ReplaceCollectionZone`:

1. Собирает 3D-объекты, на которые ещё ссылаются 2D-стаки.
2. Уничтожает 3D-объекты, которые игрок «забрал» (на них больше нет ссылок).
3. Уничтожает все 2D-копии зоны и чистит `activeCollectionObjects`.

Смысл: пока инвентарь открыт, рядом лежащие предметы показываются как 2D; перенос в основную
сетку (вне `CollectionZone`) вызывает `HandlePickedUpItem`, который уничтожает исходные 3D-объекты.

### 3.3 Перетаскивание предмета

`ItemInstance` реализует `IBeginDragHandler/IDragHandler/IEndDragHandler/IPointerClickHandler`
и делегирует в `InventoryManager`:

| Событие | Что происходит |
|---|---|
| `OnBeginDrag` | `ItemInstance.OnDragStart()`: закрыть открытый контейнер, сохранить `savedPosition`/`savedParent`, запомнить `StartRotated`, освободить ячейки, включить визуал перетаскивания, переехать в `InventoryManager.transform` |
| `OnDrag` | `UpdateDragPosition(Input.mousePosition)` → `ItemView.SetPosition` |
| `OnEndDrag` | `InventoryManager.HandleEndDrag(item, target)` |
| `OnPointerClick` | ЛКМ → заглушка, ПКМ → `ContextMenuUI.Show` |

`HandleEndDrag`:

1. `TryPlaceInGrid(target)` — попытка положить.
2. Если положили — `item.OnDragEnd()`.
3. Если бросили в пустоту и предмет не из коллекционной зоны — `DropItem`.
4. Иначе — `ReturnToStart` (тряска + возврат).

`TryPlaceInGrid(target)` — порядок приоритетов:

1. **Слот экипировки** (`EquipmentSlot` в родителях target) — `CanEquip` + `EquipItem`.
2. **Стак** — если `targetItem.GetItemState().data == draggedItem...data` и `maxStackSize > 1`,
   переносит `min(остаток, свободное место)`, обновляет UI; при полном переносе уничтожает dragged.
3. **Сетка** — `InventoryGrid` в родителях target, `draggedItem.CanPlaceInGrid` + `PlaceInGrid`.
   Если сетка вне `CollectionZone` — `HandlePickedUpItem` (уничтожение 3D-источников).

### 3.4 Размещение в сетке

`ItemInstance.CanPlaceInGrid(targetCell, grid)`:

1. `ItemPlacementValidator.CanPlaceItem(...)` — точная позиция;
2. если не вышло — `TryFindNearbyPosition(...)` — проверка соседей `(0,0), (1,0), (-1,0), (0,1), (0,-1)`.

`CanPlaceItem` проверяет:

- валидность входов;
- запрет класть предмет в сетку другого предмета (`InitializeGrid.IsContainerGrid == false`
  и есть `ItemInstance` в родителях);
- фильтр категорий контейнера (`InventoryGrid.CanAcceptItem`);
- запрет контейнеров внутри контейнера (`Container`/`Backpack` + `IsInsideContainer`);
- границы по `InitializeGrid.GetDimensions()`;
- свободность всех целевых `InventoryCell` (`fullness == null`).

Позиция считается от **центральной** ячейки предмета:
`offset = targetCell.gridSize - itemCells[itemCells.Count / 2].gridSize`.

`PlaceInGrid` → `ExecutePlacement`:

- сохраняет `sizeDelta`, `localScale`, якоря и pivot;
- добавляет/включает `LayoutElement.ignoreLayout = true` (иначе `GridLayoutGroup` ломает размер);
- `SetParent` с `worldPositionStays: true`, восстановление размеров;
- для чётных размеров и многоячеечных предметов сдвиг на пол-ячейки (`±25f`);
- `ItemView.SmoothMoveTo`;
- `OccupyCells` — `InventoryCell.fullness = item`.

### 3.5 Поворот

`InventoryManager.Update` при активном drag и нажатии `R` → `ItemInstance.RequestRotate()`:

- переключает `isRotated`, крутит `ItemView` на `-90°`;
- `RotateGridCells` пересчитывает `InventoryCell.gridSize`:
  `baseX = i / H`, `baseY = i % H`; при повороте `gridSize = (baseY, W - 1 - baseX)`,
  где `W = size.x`, `H = size.y`.

### 3.6 Контейнеры

`OpenAction` (ПКМ → «Открыть») → `InventoryManager.OpenContainer(item)`:

- закрывает предыдущий контейнер;
- ищет дочерний объект с именем **ровно** `Conteyner` (опечатка зашита в коде и префабах);
- активирует панель, переносит её в `containerParent` (обычно Canvas);
- находит `InitializeGrid` внутри, ставит `CanvasGroup` в интерактивное состояние;
- вешает `CloseContainer` на `Header/Button_Close` (с `RemoveAllListeners`);
- запоминает `openContainer`.

`CloseContainer`: гасит `CanvasGroup`, возвращает панель внутрь предмета, выключает, чистит поля.

`TryPlaceInFirstFreeCell(item)` работает только с сеткой открытого контейнера (`openContainerGrid`),
поэтому снятая экипировка падает в открытый контейнер либо на землю.

### 3.7 Экипировка

`EquipmentSlot`:

- `Setup(EquipmentSlotData)` — категории, спрайт, позиция, размер;
- `CanEquip(ItemState)` — побитовая проверка `allowedCategories`;
- `EquipItem(ItemInstance)` — прячет объект в слот, копирует `ItemStateData`, ставит иконку;
- `UnequipItem()` — будит объект, чистит слот, кладёт в первую свободную ячейку или выбрасывает;
- drag из слота: `OnBeginDrag` будит предмет и передаёт управление `ItemInstance`;
- `OnEndDrag` → `InventoryManager.HandleEndDrag`, затем корутина `CheckSlotAfterDrag` ждёт **0.3 с**
  (длительность тряски) и либо очищает слот, либо возвращает предмет и иконку.

Слоты создаются `EquipmentSlotsManager.Awake`: инстанциирует `slotPrefab` по списку `slotDatas`.

### 3.8 Контекстное меню

`ContextMenuUI.Awake` регистрирует действия в порядке:

| Действие | Класс | Условие | Что делает |
|---|---|---|---|
| Выбросить | `DropAction` | любой предмет | `DropItem` |
| Использовать | `UseAction` | любой предмет | только `Debug.Log` (заглушка) |
| Открыть | `OpenAction` | `Container` или `Backpack` | `OpenContainer` |
| Экипировать | `EquipeItem` | оружие/броня/шлем/рюкзак, не в слоте | ищет первый подходящий пустой слот |
| Снять | `UnequipAction` | предмет лежит в слоте | `EquipmentSlot.UnequipItem` |
| Разделить | `SplitAction` | стак > 1, не в слоте, не в collection zone | делит пополам, клонирует объект |

Меню закрывается по `Escape` и по ПКМ вне предмета/слота/меню (raycast через `EventSystem`).

### 3.9 Стамина

`PlayerStamina` — отдельный компонент на `Player`; `PlayerManager` только спрашивает и тратит.

- `EvaluateSprint(sprintHeld, moving)` — вызывается каждый кадр до расчёта скорости. Уже начатый
  бег можно продолжать до нуля; начать заново — только при `StaminaPercent > 12%`.
- `TryJump()` — если стамины меньше `jumpCost`, возвращает `false`, прыжок не выполняется.
- `Tick(dt, moving, grounded)` — после движения: бег ест `sprintDrain`; при перевесе
  (`carryWeight > maxCarryWeight`) ходьба ест `walkOverweightDrain * (1 + OverweightRatio * multiplier)`;
  иначе, спустя `regenDelay`, стамина восстанавливается.
- `PlayerStaminaBar` только читает `Stamina`, `MaxStamina`, `StaminaPercent`, `SprintBlocked`
  и рисует масштабируемый бар в `OnGUI`.

### 3.10 Тело и процедурная анимация

`PlayerProceduralBody` строится в `Start` из примитивов и не сохраняется в сцену:

- пропорции считаются от пола (`controller.center.y - height / 2`) и центра `Head`,
  поэтому голова визуала совпадает с камерой;
- старая капсула `GG` и сфера `Head` не удаляются — у них выключается `MeshRenderer`,
  коллайдеры продолжают работать;
- ноги: ось маха `Cross(up, localVelocity)`; направление берётся из `CharacterController.velocity`
  и переводится в локальные координаты тела, поэтому шаг работает вперёд/назад/вбок/по диагонали;
- фаза и амплитуда зависят от скорости (`walkCadence → runCadence`, `walkSwing → runSwing`);
- колени сгибаются на обратном махе, стопы подворачиваются, руки качаются в противофазе,
  корпус наклоняется в сторону движения;
- в приседе тело опускается на `(standHeight - controller.height) / 2`, колени добавляют `crouchKneeBend`.

---

## 4. Справочник файлов

`ItemData`, `ItemCategory` и `ItemState` разобраны в разделе 2 (модель данных) — здесь остальные 22 файла.

Быстрый индекс:

| Файл | Классы | Раздел |
|---|---|---|
| `Player/PlayerManager.cs` | `PlayerManager` | 4.1 |
| `Inventory/Core/InventoryManager.cs` | `InventoryManager` | 4.2 |
| `Inventory/Core/ItemPlacementValidator.cs` | `ItemPlacementValidator`, `PlacementData` | 4.3 |
| `Inventory/Core/InitializeGrid.cs` | `InitializeGrid` | 4.4 |
| `Inventory/Core/Grid.cs` | `InventoryCell` | 4.5 |
| `Inventory/Core/InventoryGrid.cs` | `InventoryGrid` | 4.6 |
| `Inventory/Items/ItemInstance.cs` | `ItemInstance` | 4.7 |
| `Inventory/Items/ItemView.cs` | `ItemView` | 4.8 |
| `Inventory/Items/ItemState.cs` | `ItemState`, `ItemStateData` | 2.3 |
| `Inventory/Items/ItemData.cs` | `ItemData` | 2.1 |
| `Inventory/Items/ItemCategory.cs` | `ItemCategory` | 2.2 |
| `Inventory/Equipment/EquipmentSlot.cs` | `EquipmentSlot` | 4.9 |
| `Inventory/Equipment/EquipmentSlotData.cs` | `EquipmentSlotData` | 4.10 |
| `Inventory/Equipment/EquipmentSlotsManager.cs` | `EquipmentSlotsManager` | 4.11 |
| `Inventory/ContextMenu/ContextMenuUI.cs` | `ContextMenuUI` | 4.12 |
| `Inventory/ContextMenu/ItemActions.cs` | 6 классов действий | 4.13 |
| `Inventory/ContextMenu/IItemAction.cs` | `IItemAction` | 4.14 |
| `Inventory/ContextMenu/IInventoryClickHandler.cs` | `IInventoryClickHandler` | 4.15 |
| `Inventory/UI/ContainerSizeHandler.cs` | `ContainerSizeHandler` | 4.16 |
| `Inventory/UI/ScrollRectNoDrag.cs` | `ScrollRectNoDrag` | 4.17 |
| `Inventory/UI/UIDragHandler.cs` | `UIDragHandler` | 4.18 |
| `Editor/ItemPrefabGenerator.cs` | `ItemPrefabGenerator` | 4.19 |
| `Player/PlayerStamina.cs` | `PlayerStamina` | 4.20 |
| `Player/PlayerStaminaBar.cs` | `PlayerStaminaBar` | 4.21 |
| `Player/PlayerProceduralBody.cs` | `PlayerProceduralBody` | 4.22 |

### 4.1 `Player/PlayerManager.cs` (348 строк)

Компонент игрока: движение, камера, инвентарь. Стамина, её бар и визуальное тело вынесены
в отдельные компоненты, которые `EnsureReferences` добавляет при отсутствии.

Поля: `head`, `body`, `staminaComponent` (`PlayerStamina`), `Inventory`, `sensitivity` (0.1–9),
`public speed = 5`, `public gravity`, `public jumpHeight`, `yRotationLimit` (0–90),
`headYawLimit` (5–120, по умолчанию 60), `bodyAlignSpeed = 240` (°/с, 0 = выключить),
`coyoteTime = 0.12`, `jumpBuffer = 0.1`, `sprintSpeed = 8`, `crouchMoveSpeed = 2`,
`crouchHeightRatio = 0.55`, `crouchTransitionSpeed = 10`, параметры тряски (`bobFrequency`,
`bobAmount`, `bobSmoothing`, `bobSharpness`, `landShakeStrength`, `jumpShakeStrength`,
`impactDistance`, `impactRecovery`, `landMinFallSpeed`), `public List<GameObject> SelectedObj`.

Методы:

| Метод | Назначение |
|---|---|
| `Awake` | `SelectedObj = new List`, кэш `CharacterController`, `Cursor.lockState = Locked`, `EnsureReferences`, поиск `InventoryManager` в детях |
| `EnsureReferences` | ленивая инициализация `controller`, `body`, `head`; добавляет `PlayerStamina`, `PlayerStaminaBar`, `PlayerProceduralBody`, если их нет; вызывается из `Awake` и каждого `Update` (переживает hot reload) |
| `CaptureBase` | запоминает базовые `height`/`center` капсулы и локальную позицию головы |
| `UpdateStance` | плавно меняет высоту капсулы/центра и высоту камеры при приседе |
| `UpdateCameraBob` | шаговая тряска, толчок от прыжка/приземления, возврат камеры на место |
| `Update` | `EnsureReferences`, `CameraLook`, `I` — инвентарь; ввод WASD; доворот тела к камере; `PlayerStamina.EvaluateSprint`; движение по взгляду; `UpdateStance`; прыжок через `TryJump`; гравитация; **один** `CharacterController.Move`; `PlayerStamina.Tick`; `UpdateCameraBob` |
| `OpenOrCloseInventory` | toggle панели, курсор, `AddCollectionZone`/`ReplaceCollectionZone` |
| `CameraLook` | pitch (вверх/вниз) — только `head.localRotation`; yaw — сначала голова, после `headYawLimit` тело догоняет на превышение порога |
| `SetSensitivity(float)` | публичная установка чувствительности |
| `OnTriggerEnter/Exit` | сбор объектов слоя 8 в `SelectedObj` |

Публичные точки доступа: `Head`, `Body`, `Controller`, `StandHeight` — используются
`PlayerProceduralBody`.

Зависимости: `PlayerStamina`, `PlayerProceduralBody`, `InventoryManager`, `CharacterController`,
Input Manager (`Horizontal`, `Vertical`, `Jump`, `Mouse X`, `Mouse Y`).
Голова ищется как `GetComponentInChildren<Camera>().transform.parent` (`GG/Head`), тело — сам объект;
при другой иерархии назначить `head`/`body` в инспекторе.

### 4.2 `Inventory/Core/InventoryManager.cs` (477 строк)

Singleton-координатор инвентаря. `Instance` — статическое свойство.

Поля: `targetCanvas`, `player`, `CollectionZone`, `contextMenu`, `containerParent`,
`public ItemInstance openContainer`, `openContainerGrid`, `draggedItem`, `isDragging`, `activeCollectionObjects`.

Публичный API:

| Метод | Назначение |
|---|---|
| `IsInCollectionZone(ItemInstance)` | предмет внутри `CollectionZone` |
| `HandleLeftClick/RightClick` | ЛКМ-заглушка / показать меню |
| `HandleBeginDrag/HandleDrag/HandleEndDrag` | маршрутизация drag-событий |
| `HandlePickedUpItem(ItemInstance)` | уничтожить 3D-источники, убрать из `SelectedObj` |
| `TryPlaceInGridFirstFree(item, InitializeGrid, InventoryGrid)` | первая свободная ячейка в указанной сетке |
| `TryPlaceInFirstFreeCell(item)` | первая свободная ячейка открытого контейнера |
| `AddCollectionZone(List<GameObject>)` | создать 2D-отображения предметов рядом |
| `ReplaceCollectionZone(List<GameObject>)` | почистить зону (аргумент не используется) |
| `DropItem(ItemInstance)` | выбросить: 3 м вперёд, +1.5 м по Y, спавн `modelPrefab3D` |
| `OpenContainer/CloseContainer` | панель `Conteyner` |
| `IsDragging()`, `GetDraggedItem()` | состояние drag |

Приватно: `TryPlaceInGrid(GameObject)` — приоритет слот → стак → сетка.

Особенности:

- `Awake` уничтожает дубликат объекта, если `Instance` уже есть;
- `targetCanvas` ищется через `FindFirstObjectByType<Canvas>()` (Unity 6 API);
- `contextMenu` берётся из `targetCanvas` при отсутствии ссылки.

### 4.3 `Inventory/Core/ItemPlacementValidator.cs` (251 строка)

Статический валидатор размещения. Не MonoBehaviour.

| Метод | Назначение |
|---|---|
| `CanPlaceItem(targetCell, itemGrid, inventoryContainer, itemData, out PlacementData)` | полная проверка позиции |
| `TryFindNearbyPosition(...)` | перебор соседних ячеек |
| `OccupyCells(List<InventoryCell>, GameObject)` | `fullness = item` |
| `FreeCells(List<InventoryCell>)` | `fullness = null` |

`PlacementData`: `targetCell`, `targetGrids`, `itemCells`, `offset`, `inventoryInit`.

Приватные помощники: `IsContainer`, `IsInsideContainer` (поиск `InventoryGrid.isContainerGrid` вверх по родителям),
`ValidateInputs`, `GetItemCells`, `ComputeDesiredPositions`, `ArePositionsWithinBounds`,
`GetInventoryGridsForPositions`, `AreCellsFree`.

### 4.4 `Inventory/Core/InitializeGrid.cs` (170 строк)

Генератор сетки ячеек 1×1 внутри предмета/контейнера.

Поля: `prefabGrid`, `gridSize`, `public IsContainerGrid`, `public LimitColumns`, `public maxColumns = 20`,
`public List<GameObject> listGrid`.

| Метод | Назначение |
|---|---|
| `Awake` | `TryInit()` |
| `TryInit` | если у родителя есть `ItemState.data` — взять `containerSize` (контейнер) или `size`, очистить детей, сгенерировать |
| `ChangeSize` | `RectTransform.sizeDelta`: ширина `columns * 50`, высота `gridSize.x * 50`; `GridLayoutGroup.constraintCount` |
| `GenerateGrid` | создаёт `Cell_{i}_{j}`, задаёт `InventoryCell.gridSize = (i, j)`, слой как у родителя |
| `UpdateParentSize` | если родитель — `ItemState`, подгоняет его `RectTransform` под сетку |
| `ClearGrid` | удалить все ячейки |
| `GetDimensions` | `(maxX+1, maxY+1)` |
| `GetCellAt(Vector2Int)` | GameObject ячейки |
| `GetGridAt(Vector2Int)` | `InventoryCell` ячейки |

Координаты: `x` — строка (вертикаль), `y` — столбец (горизонталь). Ячейка 50×50 px.

### 4.5 `Inventory/Core/Grid.cs` (27 строк)

Класс `InventoryCell` (файл исторически называется `Grid.cs`).

Поля: `public GameObject fullness` (кто занимает), `public Vector2Int gridSize`.
Свойство `IsOccupied => fullness != null`. Методы: `Awake` (обнуление), `Clear()`.

### 4.6 `Inventory/Core/InventoryGrid.cs` (19 строк)

Фильтр контейнера.

Поля: `allowedTypes` (`ItemCategory`), `isContainerGrid` (запрет вложенных контейнеров).
Метод `CanAcceptItem(ItemCategory)` — `(allowedTypes & itemCategory) != 0`.

> `allowedTypes` по умолчанию `None` — если не настроить, в сетку нельзя положить ничего.

### 4.7 `Inventory/Items/ItemInstance.cs` (408 строк)

Координатор предмета: связывает `ItemState`, `ItemView`, `InitializeGrid`.

Реализует интерфейсы: `IPointerClickHandler`, `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`,
`IInventoryClickHandler`.

Поля: `itemState`, `itemView`, `itemGrid`, `isRotated`, `StartRotated`, `savedPosition`, `savedParent`,
`isInitialized`, `occupiedCells`, `public List<GameObject> WorldSourceObjects`.

Публичный API:

| Метод | Назначение |
|---|---|
| `ReleaseCells()` | освободить занятые ячейки |
| `GetSavedParent()` | родитель до drag |
| `Init()` | идемпотентная инициализация: `TryInit` сетки, обновление стака, скрытие `Conteyner` |
| `UpdateStackUI()` | обновить текст стака |
| `OnDragStart/UpdateDragPosition/OnDragEnd` | drag-логика |
| `RequestRotate()` | поворот на 90° |
| `CanPlaceInGrid(GameObject, InventoryGrid)` | проверка + поиск рядом |
| `PlaceInGrid(GameObject)` | размещение и занятие ячеек |
| `ReturnToStart()` / `ReturnToStart(Vector3, Transform)` | возврат с тряской |
| `GetItemState()`, `GetStateData()` | доступ к состоянию |
| `HandleLeftClick/HandleRightClick/...` | реализация `IInventoryClickHandler` |

Приватно: `ExecutePlacement`, `FreeOccupiedCells`, `RotateGridCells`, `UpdateVisuals` (пустой),
`OnDestroy` (освобождение ячеек).

### 4.8 `Inventory/Items/ItemView.cs` (256 строк)

Только визуал: иконка, текст стака, анимации.

Поля: `canvasGroup`, `iconImage`, `iconRect`, `stackText`, `moveDuration=0.15`, `rotationDuration=0.2`,
`shakeDuration=0.25`, `completeRotationDuration=0.1`.

| Метод | Назначение |
|---|---|
| `UpdateIcon(Sprite, bool rotateIfVertical)` | смена иконки, авто-поворот вертикальных |
| `UpdateStackDisplay(int)` | текст виден при `stack > 1` |
| `ClearIcon()` | сброс спрайта |
| `StartDragVisuals/EndDragVisuals` | прозрачность 0.8 / 1.0, `blocksRaycasts` |
| `SmoothMoveTo/SetPosition/SetLocalPosition/SetParent` | перемещение |
| `SmoothRotateTo(Quaternion)` | плавный поворот + контр-поворот текста стака |
| `PlayShakeEffect(Action)` | тряска по X (±10 px) с колбэком |

Корутины: `SmoothMoveCoroutine`, `SmoothRotateCoroutine`, `ShakeCoroutine`, `CompleteRotationCoroutine`.
`OnDestroy` останавливает корутины.

### 4.9 `Inventory/Equipment/EquipmentSlot.cs` (226 строк)

Слот экипировки: `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerClickHandler`.

Поля: `public allowedCategories`, `iconImage`, `defaultSprite`, `currentItemData`, `equippedInstance`, `hasItem`.
Свойство `IsEmpty => !hasItem`.

| Метод | Назначение |
|---|---|
| `Setup(EquipmentSlotData)` | настройка из ассета |
| `CanEquip(ItemState)` | битовая проверка категорий |
| `EquipItem(ItemInstance)` | экипировать, скрыть объект, поставить иконку |
| `UnequipItem()` | снять, положить в контейнер или выбросить |
| `GetEquippedItemData()` | снимок данных |
| `AcceptsCategory(ItemCategory)` | внешняя проверка |
| `OnBeginDrag/OnDrag/OnEndDrag/OnPointerClick` | drag из слота и ПКМ-меню |

Приватно: `CheckSlotAfterDrag` (ждёт 0.3 с), `ClearSlot`, `UpdateVisuals`
(центрирование иконки, поворот при явном несовпадении ориентаций слота и спрайта).

### 4.10 `Inventory/Equipment/EquipmentSlotData.cs` (14 строк)

`ScriptableObject` (`Create → Inventory → Equipment Slot`): `slotName`, `allowedCategories`,
`defaultSprite`, `anchoredPosition`, `size` (по умолчанию 100×100).

### 4.11 `Inventory/Equipment/EquipmentSlotsManager.cs` (20 строк)

Поля: `slotPrefab`, `List<EquipmentSlotData> slotDatas`.
`Awake` инстанциирует префаб на каждый элемент и вызывает `slot.Setup(data)`.

### 4.12 `Inventory/ContextMenu/ContextMenuUI.cs` (178 строк)

Поля: `offsetX = 100`, `canvasGroup`, `buttonPrefab`; приватно `allActions`, `menuRect`, `buttonsContainer`.

| Метод | Назначение |
|---|---|
| `Awake` | регистрирует 6 действий, кэширует `RectTransform` и `CanvasGroup` |
| `Update` | `Escape` — закрыть; ПКМ — закрыть, если клик не по предмету/слоту/меню |
| `Show(ItemInstance)` | позиционировать, пересоздать кнопки, включить |
| `Hide()` | выключить `CanvasGroup` |
| `PositionMenu` | мировая точка → экран → локальная точка канваса (+ `offsetX` и половина ширины предмета) |
| `CreateVerticalLayoutGroup` | контейнер кнопок с `VerticalLayoutGroup` + `ContentSizeFitter` |
| `CreateButtons` | инстанциирует `buttonPrefab`, ставит текст `ActionName`, вешает `Execute` |
| `ClearButtonsContainer` | удаляет кнопки |

### 4.13 `Inventory/ContextMenu/ItemActions.cs` (171 строка)

Шесть реализаций `IItemAction` (см. таблицу в 3.8). Отдельный файл на несколько классов —
исключение из правила «один класс на файл».

### 4.14 `Inventory/ContextMenu/IItemAction.cs` (8 строк)

```csharp
string ActionName { get; }
bool CanExecute(ItemInstance item);
void Execute(ItemInstance item);
```

### 4.15 `Inventory/ContextMenu/IInventoryClickHandler.cs` (10 строк)

`HandleLeftClick`, `HandleRightClick`, `HandleBeginDrag`, `HandleDrag`, `HandleEndDrag(GameObject target)`.
`ItemInstance` реализует интерфейс, но `InventoryManager` вызывает конкретные методы напрямую —
интерфейс сейчас не используется как точка расширения.

### 4.16 `Inventory/UI/ContainerSizeHandler.cs` (24 строки)

Поля: `containerRect`, `gridRect`, `padding = (70, 100)`, `maxWidth=1250`, `maxHeight=800`,
`minWidth=200`, `minHeight=200`.
`Update` каждый кадр ставит `containerRect.sizeDelta = clamp(gridRect.sizeDelta + padding)`
(строка дублируется — безвредно, но мусор).

### 4.17 `Inventory/UI/ScrollRectNoDrag.cs` (22 строки)

Наследник `ScrollRect`, глушит `OnBeginDrag`/`OnDrag`/`OnEndDrag`, чтобы перетаскивание предметов
не прокручивало список. Колесо мыши обрабатывается базовым `ScrollRect` и продолжает работать.

### 4.18 `Inventory/UI/UIDragHandler.cs` (28 строк)

Перетаскивание панели-контейнера за заголовок: двигает `RectTransform` родителя на
`eventData.delta / canvas.scaleFactor`.

### 4.19 `Editor/ItemPrefabGenerator.cs` (303 строки)

`[CustomEditor(typeof(ItemData))]`, Editor-only. Добавляет в инспектор `ItemData` кнопки:

- **Generate 2D Prefab** — из `Assets/Prefabs/Items/Templates/ItemTemplate.prefab`;
- **Generate 3D Prefab** — из `Assets/Prefabs/Items/Templates/Item3DTemplate.prefab`;
- **Generate All Prefabs** — обе.

Куда сохраняет: `Assets/Prefabs/Items/{Weapons|Containers|Equipment|Consumables|Misc}/{itemName}/{itemName}_2D.prefab`
и `_3D.prefab`.

2D: проставляет `ItemState.data`, `childrenUI = null`, настройки `InitializeGrid`, размеры
(`columns = size.y`, `width = columns*50`, `height = size.x*50`), иконку; для контейнеров
добавляет дочерний `Conteyner` с сеткой `containerSize` (колонок не больше 23) и `ContainerSizeHandler`.

3D: копирует меш и материалы из `modelPrefab3D`, подгоняет `BoxCollider` по `bounds`,
проставляет `childrenUI` в сгенерированный 2D-префаб.

Приватный `CreateContainer` — мёртвый код (загружает шаблон и ничего не делает).

### 4.20 `Player/PlayerStamina.cs` (101 строка)

Отдельный компонент стамины: только цифры и правила, без камеры и UI.

Поля:

| Поле | Значение по умолчанию | Смысл |
|---|---|---|
| `maxStamina` | 100 | максимум |
| `staminaRegen` | 12 | восстановление в секунду |
| `regenDelay` | 0.8 | пауза после последней траты |
| `sprintDrain` | 15 | расход бега в секунду |
| `walkOverweightDrain` | 10 | базовый расход при перевесе |
| `jumpCost` | 15 | стоимость прыжка |
| `sprintResumeThreshold` | 12 | порог возобновления бега, % |
| `carryWeight` | 0 | текущий вес (заглушка) |
| `maxCarryWeight` | 50 | лимит без штрафа |
| `overweightDrainMultiplier` | 1 | рост расхода от перевеса |

Публично: `Stamina`, `MaxStamina`, `StaminaPercent`, `JumpCost`, `IsSprinting`, `SprintBlocked`.

Методы:

| Метод | Назначение |
|---|---|
| `EvaluateSprint(sprintHeld, moving)` | решает, бежим ли: продолжить можно до нуля, начать — только при `StaminaPercent > 12%` |
| `TryJump()` | списывает `jumpCost`, возвращает `false`, если не хватает |
| `Tick(dt, moving, grounded)` | расход бега/перевеса, регенерация после `regenDelay` |
| `SetCarryWeight(float)` | точка входа для будущей системы веса |

При `stamina <= 0` бег выключается; после горячей перезагрузки `initialized == false` возвращает
стамину к максимуму, чтобы Play Mode не стартовал с нулём.

### 4.21 `Player/PlayerStaminaBar.cs` (78 строк)

Отладочный бар, только чтение `PlayerStamina`.

- `[RequireComponent(typeof(PlayerStamina))]`, ссылка берётся в `Awake`.
- `showStaminaBar` — включение/выключение.
- Базовые размеры для 1080p: `baseWidth = 280`, `baseHeight = 20`, `baseMargin = 16`,
  `baseFontSize = 14`; масштаб `Clamp(Screen.height / 1080, minScale 0.8, maxScale 2.5)`.
- Позиция — нижний левый угол, `OnGUI` + `EventType.Repaint`.
- Цвет: красный → зелёный по проценту; серый `[LOCK]`, когда `SprintBlocked`.
- `GUIStyle` кэшируется по размеру шрифта.

### 4.22 `Player/PlayerProceduralBody.cs` (383 строки)

Строит человекоподобное тело из примитивов в `Start` и анимирует конечности.
Ничего не сохраняет в сцену и не меняет префабы.

- Пропорции: пол из `controller.center.y - height / 2`, верх из `Head`; высота `H`,
  бёдра `0.52H`, колени `0.28H`, плечи `0.87H`, голова — радиус `0.065H`.
- Скелет: `VisualBody → Pelvis, UpperBody, Hip_L/R`; в `UpperBody` — торс, шея, плечи,
  предплечья, кисти; в ногах — бедро → колено → голень → стопа.
- Прячет `MeshRenderer` у старой капсулы `GG` и сферы `Head`, коллайдеры остаются.
- Материалы создаются в рантайме на `Shader.Find("Standard")` (Built-in RP), fallback — `Unlit/Color`.
- Анимация в `Update`: `controller.velocity` → локальные координаты; ось маха `Cross(up, direction)`;
  фаза `walkCadence → runCadence`, амплитуда `walkSwing → runSwing`; колени и стопы сгибаются,
  руки в противофазе, корпус наклоняется; в приседе тело опускается и колени добавляют `crouchKneeBend`.
- Настройки: `walkSwing = 24`, `runSwing = 42`, `walkCadence = 7`, `runCadence = 14`,
  `leanAngle = 7`, `crouchKneeBend = 45`, `smoothing = 12`.
- `OnDestroy` уничтожает созданные материалы; при повторном построении старый `VisualBody` удаляется.

---

## 5. Где что висит в сцене

| Объект | Компоненты |
|---|---|
| `Player` | `CharacterController`, `PlayerManager`, `BoxCollider`; в Play Mode добавляются `PlayerStamina`, `PlayerStaminaBar`, `PlayerProceduralBody` |
| `Player/GG` | `MeshRenderer` (капсула, скрывается в Play), `CapsuleCollider`, дочерние `Head`, `TargetHand`, `TargetBackPack` |
| `Player/GG/Head` | `MeshRenderer` (сфера, скрывается), `SphereCollider`, `Main Camera` |
| `Player/VisualBody` | создаётся в Play Mode: `Pelvis`, `UpperBody`, `Hip_L/R` и конечности |
| `Player/Canvas/Inventory` (или `Canvas/Inventory`) | `InventoryManager`, `InitializeGrid`, `ScrollRectNoDrag`, `InventoryGrid` |
| `Canvas` | `ContextMenuUI`, слоты экипировки, `Conteyner` |
| `Canvas/.../Slots` | `EquipmentSlotsManager` (создаёт `EquipmentSlot`) |
| мир | префабы `*_3D`: `ItemState` + коллайдер, слой 8 |
| `CollectionZone` | `ScrollRectNoDrag` + `InitializeGrid` + `InventoryGrid`, 2D-иконки |

`PlayerManager` сам находит `InventoryManager` в детях и подставляет его GameObject в `Inventory`,
поэтому имя объекта не важно, важен факт наличия компонента.
`PlayerStamina`/`PlayerStaminaBar`/`PlayerProceduralBody` не сериализуются в сцену — если нужны
постоянные настройки в инспекторе, добавь их на `Player` вручную (авто-добавление сработает только
при отсутствии).

---

## 6. Соглашения и числа

| Константа | Где | Значение |
|---|---|---|
| Размер ячейки | `InitializeGrid.ChangeSize`, `GenerateGrid`, `ItemPrefabGenerator`, `AddContainer` | 50×50 px |
| Сдвиг чётных предметов | `ItemInstance.ExecutePlacement`, `ReturnToStart` | ±25 px |
| Ожидание после drag из слота | `EquipmentSlot.CheckSlotAfterDrag` | 0.3 с |
| Дистанция выброса | `InventoryManager.DropItem` | 3 м вперёд, +1.5 м |
| Лимит колонок контейнера | `ItemPrefabGenerator.AddContainer` | 23 |
| Лимит `maxColumns` | `InitializeGrid` | 20 |
| Границы панели контейнера | `ContainerSizeHandler` | 200…1250 × 200…800 |
| Скорости игрока | `PlayerManager` | ходьба 5, бег 8, присед 2 м/с |
| Coyote time / буфер прыжка | `PlayerManager` | 0.12 / 0.1 с |
| Поворот головы | `PlayerManager` | pitch ±88°, yaw ±60° до подключения тела |
| Доворот тела к камере | `PlayerManager.bodyAlignSpeed` | 240 °/с |
| Стамина | `PlayerStamina` | 100 макс, реген 12/с, пауза 0.8 с, бег −15/с, перевес −10/с, прыжок −15, порог 12% |
| Шаговая тряска | `PlayerManager` | частота 12, амплитуда 0.045 м, сглаживание 20, резкость 0.35 |
| Прыжок/приземление | `PlayerManager` | подброс 0.25, посадка 0.6, сдвиг 0.08 м, затухание 9, мин. падение 3 м/с |
| Анимация тела | `PlayerProceduralBody` | мах 24°/42°, темп 7/14, наклон 7°, сгиб колен в приседе 45° |
| Бар стамины (1080p) | `PlayerStaminaBar` | 280×20 px, масштаб 0.8…2.5 от высоты экрана |

Размер предмета: `size = (высота, ширина)`. То есть `size.x` — количество строк,
`size.y` — количество столбцов. Это же соглашение используется в генераторе префабов.

---

## 7. Рецепты

### 7.1 Добавить новый предмет

1. `Create → Inventory → Item Data`, заполнить `itemName`, `icon`, `category`, `size`,
   `maxStackSize`, `modelPrefab2D`, `modelPrefab3D` (для контейнера — ещё `containerSize`).
2. Нажать **Generate All Prefabs** в инспекторе ассета.
3. 3D-префаб положить в сцену (слой 8, коллайдер), 2D — в `childrenUI` 3D-префаба (генератор это делает сам).
4. Проверить, что у 2D-префаба есть `ItemInstance`, `ItemState`, `ItemView`, `InitializeGrid` с `prefabGrid`.

### 7.2 Добавить слот экипировки

1. `Create → Inventory → Equipment Slot`, задать `allowedCategories`, `defaultSprite`, `size`, `anchoredPosition`.
2. Добавить ассет в `EquipmentSlotsManager.slotDatas`.
3. Убедиться, что `slotPrefab` содержит `EquipmentSlot` и `Image` для иконки.

### 7.3 Добавить действие контекстного меню

1. Реализовать `IItemAction` (`ActionName`, `CanExecute`, `Execute`).
2. Добавить экземпляр в `ContextMenuUI.Awake` (порядок = порядок кнопок).

### 7.4 Поменять размер ячейки

50 px зашито в четырёх местах: `InitializeGrid.ChangeSize`, `InitializeGrid.GenerateGrid`
(`cellSize`), `ItemPrefabGenerator.Generate2DPrefab`, `ItemPrefabGenerator.AddContainer`.
Менять все четыре согласованно, иначе префабы и рантайм разъедутся.

### 7.5 Настроить стамину

1. На `Player` в Play Mode появится `PlayerStamina` (или добавь компонент вручную в edit mode).
2. Крути `maxStamina`, `staminaRegen`, `sprintDrain`, `jumpCost`, `sprintResumeThreshold`.
3. Перевес: выставь `carryWeight` больше `maxCarryWeight` — ходьба начнёт тратить стамину.
4. Бар: `PlayerStaminaBar`, снять `showStaminaBar` — выключить; размеры — `baseWidth/baseHeight`.

### 7.6 Настроить тестовое тело и походку

1. `PlayerProceduralBody` строится в Play Mode; параметры — в его инспекторе.
2. `walkSwing`/`runSwing` — амплитуда ног, `walkCadence`/`runCadence` — частота шагов.
3. `leanAngle` — наклон корпуса, `smoothing` — плавность, `crouchKneeBend` — сгиб в приседе.
4. Хочешь своё тело — убери авто-добавление `PlayerProceduralBody` из `EnsureReferences`
   и повесь визуал вручную; процедурная анимация останется только у этого скрипта.

---

## 8. Техдолг и известные проблемы

### 8.1 Кодировки (важно)

Вопреки `AGENTS.md` («все скрипты в Windows-1251»), кодировки смешаны:

- **CP1251 (16 файлов):** `Editor/ItemPrefabGenerator.cs`, `Inventory/ContextMenu/ItemActions.cs`,
  `Inventory/Core/Grid.cs`, `Inventory/Core/InventoryGrid.cs`, `Inventory/Core/ItemPlacementValidator.cs`,
  `Inventory/Equipment/EquipmentSlotData.cs`, `Inventory/Items/ItemData.cs`, `Inventory/Items/ItemInstance.cs`,
  `Inventory/Items/ItemState.cs`, `Inventory/Items/ItemView.cs`, `Inventory/UI/ScrollRectNoDrag.cs`,
  `Inventory/UI/UIDragHandler.cs`, `Player/PlayerManager.cs`, `Player/PlayerStamina.cs`,
  `Player/PlayerStaminaBar.cs`, `Player/PlayerProceduralBody.cs`.
- **UTF-8 (9 файлов):** `Inventory/ContextMenu/ContextMenuUI.cs`, `IInventoryClickHandler.cs`,
  `IItemAction.cs`, `Inventory/Core/InitializeGrid.cs`, `Inventory/Core/InventoryManager.cs`,
  `Inventory/Equipment/EquipmentSlot.cs`, `EquipmentSlotsManager.cs`, `Inventory/Items/ItemCategory.cs`,
  `Inventory/UI/ContainerSizeHandler.cs`.

Правило: перед правкой проверять кодировку конкретного файла и сохранять её; полностью
перезаписывать файл в другой кодировке нельзя — русские комментарии превратятся в мусор.

### 8.2 Хрупкие места

- `InventoryManager.HandleEndDrag` обращается к `CollectionZone.transform` без проверки на `null` —
  если поле не назначено, будет `NullReferenceException` при отпускании предмета.
- `AddCollectionZone` не проверяет `initializeGrid` перед `initializeGrid.listGrid`.
- `ItemInstance.Awake` падает в `Debug.LogError`, если нет `ItemState`, но продолжает работу.
- `ItemInstance.OnDragStart` использует `GetComponentInParent<InventoryManager>()` — предмет обязан
  находиться под объектом с менеджером.
- `DropItem` использует `player.transform` без проверки `player`.
- `ContextMenuUI.PositionMenu` передаёт `null` как камеру в `WorldToScreenPoint` — корректно только
  для `Screen Space - Overlay`.
- `EquipmentSlot.CheckSlotAfterDrag` полагается на магические 0.3 с (`shakeDuration = 0.25`).
  Изменили анимацию — правьте и ожидание.
- `ItemInstance.ReturnToStart` (ветка без `ItemView`) содержит дублирующий `if` и нерабочий сдвиг —
  ветка практически недостижима, потому что `ItemView` добавляется автоматически.
- `EquipItem` дважды вызывает `SetParent`/`SetActive`.
- `ContainerSizeHandler.Update` каждый кадр пересчитывает размер, строка продублирована.
- `ContextMenuUI` пересоздаёт кнопки на каждый `Show` — при частых кликах лишние аллокации.
- `EquipeItem.Execute` делает `FindObjectsByType<EquipmentSlot>` на каждое действие.
- `UseAction` — заглушка (`Debug.Log`), реального использования нет.
- `ItemPrefabGenerator.CreateContainer` — мёртвый код.

Из `Player`-системы:

- `PlayerStamina`, `PlayerStaminaBar` и `PlayerProceduralBody` добавляются в рантайме и **не**
  сериализуются в сцену: их настройки сбрасываются при выходе из Play Mode. Нужна постоянная
  настройка — добавь компоненты на `Player` вручную.
- `PlayerProceduralBody` скрывает `MeshRenderer` капсулы `GG` и сферы `Head`; если у объекта
  появится другой визуал, он может остаться видимым поверх.
- Тело строится на `Shader.Find("Standard")` — в URP/HDRP материал будет розовым.
  Для смены пайплайна правится `CreateMaterial`.
- Пропорции тела зависят от позиции `Head` и высоты `CharacterController`: сдвинули камеру —
  поедет и анатомия.
- `PlayerStamina` после hot reload сбрасывается в максимум (страховка), а не сохраняет текущее значение.
- Бар — `OnGUI`-отладка: не участвует в Canvas-порядке, поверх UI, и его нельзя стилизовать как uGUI.
- `bodyAlignSpeed` и `headYawLimit` работают вместе: сначала голова до 60°, затем тело догоняет;
  при `bodyAlignSpeed = 0` доворот отключается, остаётся только правило лимита.

### 8.3 Известные ошибки из `AGENTS.md`

- `Serialization depth limit 10 exceeded at 'ItemStateData.data'` — рекурсия
  `ItemState.containerItems → ItemStateData.data → containerItems → …`.
  Решение: хранить ссылки на вложенные контейнеры по `uniqueId` либо не сериализовать runtime-связи.
- `EventSystem` на `StandaloneInputModule`; при переходе на Input System заменить на
  `InputSystemUIInputModule`, иначе UI перестанет реагировать.

### 8.4 Архитектурные замечания

- Нет namespace — все классы в глобальной области, риск конфликтов имён.
- `InventoryManager` — god-object: drag, стаки, экипировка, контейнеры, выброс, коллекционная зона.
  Кандидат на разбиение (DragService, StackService, ContainerService, LootService).
- `PlayerManager` после разделения отвечает за движение/камеру/инвентарь; стамина, бар и тело —
  отдельные компоненты. Дальше стоит так же вынести шаговую тряску и приседание.
- Три похожих понятия сетки: `InitializeGrid` (генератор), `InventoryGrid` (фильтр),
  `InventoryCell` (ячейка) — читать внимательно.
- Два флага `isContainerGrid`: `InitializeGrid.IsContainerGrid` (что рисовать) и
  `InventoryGrid.isContainerGrid` (запрет вложенности).
- Нет тестов и валидации данных ассетов: пустой `prefabGrid`, `allowedTypes = None`,
  отсутствие `modelPrefab3D` ловятся только в рантайме/логах.

---

## 9. Чек-лист после изменений

1. Скрипты компилируются, в консоли Unity нет `error CS`.
2. Инвентарь открывается по `I`, курсор разблокируется/захватывается.
3. Предметы рядом появляются в коллекционной зоне и исчезают из мира только после переноса в сетку.
4. Drag: перенос внутри сетки, между сетками, в слот, из слота.
5. Поворот `R` во время drag: визуал и занятые ячейки совпадают.
6. Стак: перенос на такой же предмет, частичный перенос, деление (`Split`).
7. Выброс: 3D-префаб появляется перед игроком, данные (`currentStack`, `containerItems`) сохранены.
8. Контейнер: открытие, закрытие, кнопка `Header/Button_Close`, предметы не теряются.
9. Экипировка: подходящая категория, снятие, возврат в свободную ячейку.
10. Контекстное меню: состав кнопок соответствует предмету, закрытие по `Esc` и клику мимо.
11. Генератор префабов: 2D/3D создаются по путям шаблонов, размеры сетки совпадают с `ItemData.size`.
12. Бег: Shift ускоряет до 8 м/с, стамина падает; в приседе бег недоступен.
13. Прыжок: при стамине меньше 15 не выполняется; после приземления есть толчок камеры.
14. Стамина: после остановки бега ниже 12% повторный бег недоступен до регена выше порога.
15. Перевес: `carryWeight > maxCarryWeight` — ходьба тратит стамину, рост расхода виден.
16. Бар стамины виден на 720p/1080p/4K, не уезжает за край, показывает `[LOCK]`.
17. Тело: в Play Mode походка меняется при движении вперёд/назад/вбок, в приседе колени согнуты,
    при остановке конечности возвращаются в покой.
