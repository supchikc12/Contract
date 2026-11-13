# Contract — справочник по скриптам

Документ собран по фактическому коду каталога `Assets/Script`.
Описывает назначение каждого класса, публичный API, связи между системами и подводные камни.

- Unity: 6000.6.8f1
- Язык: C#, без namespace, один публичный класс на файл (исключения: `ItemActions.cs`,
  `ItemState.cs`, `ItemPlacementValidator.cs`, `ContainerGridData.cs` и наследники `ItemData` —
  там рядом с основным классом лежат тесно связанные структуры/действия)
- UI: uGUI + TextMesh Pro
- Ввод: старый Input Manager (`Input.GetAxis`, `Input.GetKeyDown`)
- Сборка: без `.asmdef`, всё в `Assembly-CSharp` / `Assembly-CSharp-Editor`

---

## 1. Карта систем

```
ItemData (ScriptableObject, базовый ассет)
   │  id, имя, иконка, категория, размер, вес, стак, прочность, префабы
   ├── EquippableData
   │      ├── WeaponData
   │      ├── ArmorData
   │      ├── ClothingData
   │      └── ContainerData → BackpackData / RigData
   ├── ConsumableData
   ├── AmmoData
   └── WeaponModData
        │
        ▼
ItemState (MonoBehaviour, runtime-состояние)
   │  data, currentStack, currentDurability, uniqueId, containerItems
   ├──────────────► ItemInstance (поведение, drag&drop, стаки)
   │                     │
   │                     ├── ItemView (визуал: иконка, стак, анимации)
   │                     ├── ItemGridPlacement (ячейки, поворот, позиция)
   │                     └── InitializeGrid (сетка ячеек предмета)
   │
   └──────────────► ItemStateData (struct, снимок для клонирования/выброса)

InventoryManager (singleton, координатор)
   ├── InventoryGrid (фильтр категорий для контейнера)
   ├── InitializeGrid → InventoryCell (ячейки 1×1)
   ├── EquipmentSlot → EquipmentSlotData (слоты экипировки)
   ├── ContextMenuUI → IItemAction (ПКМ-меню)
   └── TryPickupWorldItem → TryStoreItem (слот → контейнер)

PlayerMotor (движение, прыжок, присед, стамина)
   ├── PlayerCamera (обзор, доворот тела, тряска камеры)
   ├── PlayerInventoryToggle (I, курсор, блокировка ввода)
   ├── PlayerStamina (стамина: расход, реген, порог бега, стоимость прыжка)
   ├── PlayerStaminaBar (отладочный OnGUI-бар стамины)
   └── PlayerProceduralBody (анимация готовой модели Player/VisualBody)
PlayerHealth (хп 16 зон) ── BodyPartHealth (маркеры на частях тела)
BodyPartInspectionUI (F1 — заглушка осмотра зон)
PlayerPickupController (raycast + клавиша E, подбор предметов)
ItemPlacementValidator (статическая валидация размещения)
ItemPrefabFactory (Editor: единая сборка 2D/3D и контейнеров)
ItemCreationWizard (Editor: мастер создания предмета)
```

Поток владения:

| Слой | Кто хранит | Что хранит |
|---|---|---|
| Ассет | `ItemData` | неизменяемые параметры предмета |
| Сцена/префаб | `ItemState` | runtime-состояние конкретного экземпляра |
| Поведение | `ItemInstance` | drag-состояние, стаки, ссылки на компоненты |
| Размещение | `ItemGridPlacement` | занятые ячейки, поворот, позиция в сетке |
| Отображение | `ItemView` | анимации, иконка, текст стака |
| Сетка | `InitializeGrid` + `InventoryCell` | какие координаты заняты каким объектом |

### 1.1 Смысл и зона ответственности скриптов

| Файл | Класс(ы) | Смысл | Зона ответственности |
|---|---|---|---|
| `Editor/ItemPrefabGenerator.cs` | `ItemPrefabGenerator` | инспектор данных предмета | кнопка генерации и валидация; сама сборка в `ItemPrefabFactory` |
| `Editor/ItemPrefabFactory.cs` | `ItemPrefabFactory`, `PrefabOverwriteMode` | единая сборка prefabs | 2D→3D, контейнерная панель, слои, физика, ссылки |
| `Editor/ItemPrefabPaths.cs` | `ItemPrefabPaths` | пути и имена | папки по типу, `prefabName`, `EnsureFolder` |
| `Editor/ItemCreationWizard.cs` | `ItemCreationWizard`, `ItemCreationType` | мастер создания | данные, сетка, prefabs и режим перезаписи в одном окне |
| `Editor/WeaponModSceneSetup.cs` | `WeaponModSceneSetup` | настройка тестовой сцены | Editor-only: слот разгрузки, окно модификации, расстановка заглушек |
| `Inventory/ContextMenu/ContextMenuUI.cs` | `ContextMenuUI` | ПКМ-меню предмета | показ, позиционирование, кнопки действий |
| `Inventory/ContextMenu/IItemAction.cs` | `IItemAction` | контракт действия меню | `ActionName`, `CanExecute`, `Execute` |
| `Inventory/ContextMenu/IInventoryClickHandler.cs` | `IInventoryClickHandler` | контракт кликов по предмету | ЛКМ/ПКМ-события для `ItemInstance` |
| `Inventory/ContextMenu/ItemActions.cs` | `DropAction`, `UseAction`, `EquipeItem`, `ModifyWeaponAction`, `UnequipAction`, `SplitAction` | действия контекстного меню | один класс = одно действие; UI и хранение не трогает |
| `Inventory/Core/InventoryCell.cs` | `InventoryCell` | ячейка 1×1 | координаты, `fullness`, `isAvailable`, `pocketId`, `allowedCategories` |
| `Inventory/Core/InventoryGrid.cs` | `InventoryGrid` | фильтр сетки | `allowedTypes`, `isContainerGrid`, статический `FindInParents` |
| `Inventory/Core/InventoryManager.cs` | `InventoryManager` | координатор инвентаря | singleton, ссылки, drag-события и публичный API; хранение/UI вынесены |
| `Inventory/Core/InventoryStorage.cs` | `InventoryStorage` | хранение предметов | подбор, слоты, контейнеры, стаки, выброс; UI не знает |
| `Inventory/UI/InventoryContainerDock.cs` | `InventoryContainerDock` | правый док контейнеров | карточки, стрелки, раскрытие, перенос панелей `Conteyner` |
| `Inventory/Core/ItemPlacementValidator.cs` | `ItemPlacementValidator`, `PlacementData` | проверка размещения | границы, занятость, доступность, карманы, запрет вложенности; предметы не двигает |
| `Inventory/Core/InitializeGrid.cs` | `InitializeGrid` | генератор сетки предмета/контейнера | ячейки, размеры, `GetCellLocalPosition`, `FindInParents`; для контейнера берёт `ContainerGridData` |
| `Inventory/UI/ContainerGridVisuals.cs` | `ContainerGridVisuals` | визуал карманов контейнера | доступность ячеек, скрытие пустых промежутков; сетку не создаёт |
| `Inventory/Equipment/EquipmentSlot.cs` | `EquipmentSlot` | слот экипировки | визуал слота, экипировка/снятие, drag из слота, подпись |
| `Inventory/Equipment/EquipmentSlotData.cs` | `EquipmentSlotData` | конфиг слота | категория, иконка, позиция, размер, подпись |
| `Inventory/Equipment/EquipmentSlotsManager.cs` | `EquipmentSlotsManager` | создание слотов | инстанс `slotPrefab` по списку `slotDatas` |
| `Inventory/Items/ItemCategory.cs` | `ItemCategory` | категории предметов | битовая маска категорий |
| `Inventory/Items/ItemData/ItemData.cs` | `ItemData` | базовый ассет предмета | id, имя, иконка, категория, размер, вес, стак, прочность, 2D/3D-префабы |
| `Inventory/Items/ItemData/EquippableData.cs` | `EquippableData` | база надеваемых предметов | слот экипировки, позиция на модели |
| `Inventory/Items/ItemData/WeaponData.cs` | `WeaponData` | данные оружия | класс оружия, урон, темп, калибр, слоты модов |
| `Inventory/Items/ItemData/ArmorData.cs` | `ArmorData` | броня и шлемы | слот Head/Torso, класс брони, снижение урона |
| `Inventory/Items/ItemData/ClothingData.cs` | `ClothingData` | обычная одежда | слот одежды, утеплитель |
| `Inventory/Items/ItemData/ContainerData.cs` | `ContainerData` | общий контейнер | ссылка на `ContainerGridData` |
| `Inventory/Items/ItemData/BackpackData.cs` | `BackpackData` | рюкзак | наследник `ContainerData` |
| `Inventory/Items/ItemData/RigData.cs` | `RigData` | разгрузка | наследник `ContainerData` с карманами |
| `Inventory/Items/ItemData/ConsumableData.cs` | `ConsumableData` | еда и медицина | тип, время использования, количество применений, список эффектов |
| `Inventory/Items/ItemData/AmmoData.cs` | `AmmoData` | патроны | калибр, тип пули, пробитие, множитель урона |
| `Inventory/Items/ItemData/ContainerGridData.cs` | `ContainerGridData`, `Pocket` | конфиг сетки | размер, колонки, карманы без фильтров категорий, покамерные позиции в px |
| `Inventory/Items/ItemData/ContainerGridValidator.cs` | `ContainerGridValidator` | ограничения сетки | минимумы, максимумы, пересечения, проверка пиксельных позиций, раскладка карманов |
| `Inventory/Items/ItemInstance.cs` | `ItemInstance` | поведение экземпляра | drag, стаки, `TryPlaceOn`, клики; размещение делегирует в `ItemGridPlacement` |
| `Inventory/Items/ItemGridPlacement.cs` | `ItemGridPlacement` | размещение в сетке | занятые ячейки, поворот, позиционирование, возврат на место |
| `Inventory/Items/ItemState.cs` | `ItemState`, `ItemStateData` | runtime-состояние предмета | стак, прочность, `containerItems`, `uniqueId`; снимок для клонирования |
| `Inventory/Items/ItemView.cs` | `ItemView` | визуал предмета | иконка, текст стака, анимации; состояние не меняет |
| `Inventory/Items/ItemData/WeaponModData.cs` | `WeaponModData`, `StatModifier` | данные мода | тип слота, совместимость по `id`, задел статов |
| `Inventory/Modifications/WeaponModdingUI.cs` | `WeaponModdingUI` | окно модификации оружия | открытие окна, установка/снятие модов, drag в слоты |
| `Inventory/Modifications/WeaponModSlotType.cs` | `WeaponModSlotType` | типы слотов модов | enum слотов |
| `Inventory/Modifications/WeaponModSlotUI.cs` | `WeaponModSlotUI` | слот мода в окне | отображение и приём мода |
| `Inventory/UI/ContainerSizeHandler.cs` | `ContainerSizeHandler` | размер панели контейнера | подгонка `RectTransform` под сетку |
| `Inventory/UI/InventoryResponsiveLayout.cs` | `InventoryResponsiveLayout` | адаптивность слотов | масштаб `ManagerSlots` на узких экранах |
| `Inventory/UI/ScrollRectNoDrag.cs` | `ScrollRectNoDrag` | прокрутка без drag контента | колесо мыши, запрет перетаскивания содержимого |
| `Inventory/UI/UIDragHandler.cs` | `UIDragHandler` | перемещение окна | двигает панель контейнера за заголовок |
| `Player/PlayerMotor.cs` | `PlayerMotor` | движение игрока | WASD, бег, присед, прыжок, гравитация, `CharacterController.Move` |
| `Player/PlayerCamera.cs` | `PlayerCamera` | обзор и камера | мышь, голова/тело, наклон, шаговая тряска, прыжок/приземление |
| `Player/PlayerInventoryToggle.cs` | `PlayerInventoryToggle` | окно инвентаря | `I`, курсор, блокировка ввода движения, закрытие окна модификаций |
| `Player/PlayerPickupController.cs` | `PlayerPickupController` | подбор предметов | raycast из камеры, клавиша `E`, подсказка |
| `Player/Health/BodyPartType.cs` | `BodyPartType` | зоны тела | enum из 16 зон: голова, шея, торс, таз, конечности |
| `Player/Health/PlayerHealth.cs` | `PlayerHealth`, `BodyPartState` | здоровье игрока | хп всех зон, урон/лечение/сброс, события; не знает о модели |
| `Player/Health/BodyPartHealth.cs` | `BodyPartHealth` | маркер части тела | связывает коллайдер с зоной в `PlayerHealth`; хп не хранит |
| `Player/Health/BodyPartInspectionUI.cs` | `BodyPartInspectionUI` | осмотр тела | `F1`, список зон, часть под прицелом; временная заглушка |
| `Effects/StatType.cs` | `StatType` | статы эффектов | enum характеристик, на которые влияют модификаторы |
| `Effects/StatModifier.cs` | `StatModifier`, `Mode` | модификатор стата | `Add`/`Multiply`, значение |
| `Effects/EffectDefinition.cs` | `EffectDefinition`, `StackPolicy` | ассет эффекта | id, имя, длительность, стаки, цели, видимость, теги, модификаторы |
| `Effects/EffectTargetMask.cs` | `EffectTargetMask` | маска целей | части тела, персонаж, оружие, предмет, контейнер |
| `Effects/EffectVisibility.cs` | `EffectVisibility` | видимость эффекта | `InspectTarget`, `VisibleOnApply`, `Symptom`, `KnownFromItem` |
| `Effects/EffectTag.cs` | `EffectTag` | теги эффектов | кровотечение, ожог, яд, перелом, боль, бафф, дебафф |
| `Effects/EffectReceiver.cs` | `EffectReceiver` | активные эффекты цели | Apply/Remove, длительность, стаки, `GetValue`, обнаружение |
| `Effects/CharacterEffectAggregator.cs` | `CharacterEffectAggregator` | агрегатор персонажа | персонаж + части тела + надетые предметы, `MoveSpeed` и др. |
| `Editor/EffectDefinitionEditor.cs` | `EffectDefinitionEditor` | инспектор эффекта | кнопки «Редактировать в мастере» и «Создать новый эффект» |

Границы, которые нельзя размывать:

- `InventoryManager` не размещает предмет под курсором — это `ItemInstance.TryPlaceOn`.
- `InventoryManager` не хранит предметы — это `InventoryStorage`.
- `InventoryManager` не двигает карточки и панели дока — это `InventoryContainerDock`.
- `InitializeGrid` только строит сетку; правила «можно/нельзя» — `ItemPlacementValidator`.
- `ItemGridPlacement` двигает предмет и занимает ячейки; drag-состояние и стаки — `ItemInstance`.
- `ContainerGridVisuals` только рисует карманы; доступность ячеек проверяет `ItemPlacementValidator`.
- `InventoryGrid` только фильтр; геометрия и ячейки — `InitializeGrid`.
- `ItemView` только отображает; состояние меняют `ItemState`/`ItemInstance`.
- `PlayerMotor` не подбирает предметы — это `PlayerPickupController`.
- Editor-скрипты не участвуют в runtime и не должны попадать в игровые компоненты.

---

## 2. Модель данных

### 2.1 `ItemData` — `Assets/Script/Inventory/Items/ItemData/ItemData.cs`

`ScriptableObject`, базовый класс для всех ассетов предметов.
Создаётся через `Create → Inventory → Item Data`; специализированные типы — через свои меню
`Inventory/Weapon Data`, `Inventory/Backpack Data`, `Inventory/Rig Data` и т.д.

| Поле | Тип | Смысл |
|---|---|---|
| `id` | string | стабильный идентификатор для совместимости и сейвов |
| `itemName` | string | отображаемое имя |
| `prefabName` | string | имя папки и файлов prefabs; если пусто — `itemName` |
| `description` | string | описание предмета |
| `icon` | Sprite | иконка в инвентаре |
| `category` | `ItemCategory` | битовая маска категорий |
| `size` | `Vector2Int` | размер в ячейках: **x = строки (высота), y = столбцы (ширина)** |
| `weight` | float | вес предмета |
| `maxStackSize` | int | максимум в стаке (1 = не стакается) |
| `maxDurability` | float | максимальная прочность |
| `sourceModel3D` | GameObject | исходная модель/префаб для генерации 3D |
| `modelPrefab3D` | GameObject | сгенерированный prefab мира |
| `modelPrefab2D` | GameObject | сгенерированный prefab инвентаря |

### 2.1a Специализированные данные

| Класс | Дополнительные поля |
|---|---|
| `EquippableData` | `equipmentSlot`, `previewLocalPosition`, `previewLocalEuler` |
| `WeaponData` | `weaponClass`, `damage`, `fireRate`, `caliber`, `recoil`, `accuracy`, `allowedModSlots` |
| `ArmorData` | `armorSlot`, `armorClass`, `damageReduction` |
| `ClothingData` | `clothingSlot`, `insulation` |
| `ContainerData` | `grid` (`ContainerGridData`) |
| `BackpackData` / `RigData` | наследники `ContainerData` |
| `ConsumableData` | `consumableType`, `useTime`, `uses`, `effects` |
| `AmmoData` | `caliber`, `bulletType`, `damageMultiplier`, `penetration` |
| `WeaponModData` | `slotType`, `compatibleWeaponIds`, `statModifiers` |

`ContainerGridData` — отдельный ScriptableObject: `size`, `limitColumns`, `maxColumns`,
`allowedCategories`, `usePixelPositions`, `pockets`. У каждого `Pocket` есть `pixelPosition`
(левый верхний угол в пикселях) и `size` в ячейках. `pocketGapPixels` оставлен только
для старых ассетов. Один конфиг может использоваться несколькими предметами.

### 2.1b Миграция

Старые ассеты `ItemData` переведены на наследников заменой `m_Script` с сохранением GUID:
`AK-47 → WeaponData`, `Backpack → BackpackData`, `Razgruzka → RigData`.
Сетки вынесены в `Assets/Data/Grids/BackpackGrid.asset` и `RazgruzkaGrid.asset`.

### 2.2 `ItemCategory` — `Inventory/Items/ItemCategory.cs`

`[System.Flags]`, значения: `None=0`, `Weapon=1<<0`, `LightWeapon=1<<1`, `SecondaryWeapons=1<<2`,
`Helmet=1<<3`, `Armor=1<<4`, `Container=1<<5`, `Food=1<<6`, `Medicine=1<<7`, `Backpack=1<<8`,
`Rig=1<<9`, `WeaponMod=1<<10`, `Clothing=1<<11`, `Ammo=1<<12`, `All=~0`.

Проверки везде побитовые: `(mask & value) != 0`.

### 2.3 `ItemState` — `Inventory/Items/ItemState.cs`

Runtime-состояние экземпляра.

| Поле | Тип | Смысл |
|---|---|---|
| `data` | `ItemData` | ссылка на ассет |
| `childrenUI` | GameObject | префаб 2D-версии (для мира) |
| `currentStack` | int | текущее количество |
| `currentDurability` | float | текущая прочность |
| `containerItems` | `List<ItemStateData>` | runtime-содержимое контейнера (не сериализуется) |
| `installedMods` | `List<ItemStateData>` | установленные модификации оружия (не сериализуется) |
| `uniqueId` | string | GUID, генерируется в `Awake`, если пуст |

Методы:

- `bool IsValid()` — `data != null && currentStack > 0 && currentDurability > 0`
- `ItemStateData GetStateData()` — снимок (включая копии `containerItems` и `installedMods`)
- `void SetStateData(ItemStateData)` — применение снимка
- `bool IsEmptyContainer()`

`ItemStateData` — `[Serializable] struct`: `data`, `currentStack`, `currentDurability`, `uniqueId`,
`containerItems`, `installedMods`.

> `containerItems` и `installedMods` помечены `[NonSerialized]`: это runtime-связи,
> их не нужно отдавать сериализатору Unity. Это убирает предупреждение
> `Serialization depth limit 10 exceeded`.

---

## 3. Потоки

### 3.1 Появление предмета в мире

1. В сцене лежит 3D-префаб: `MeshFilter`/`MeshRenderer`, `BoxCollider`, `ItemState`, слой `8` (Item).
2. `ItemState.data` указывает на `ItemData`, `ItemState.childrenUI` — на 2D-префаб.
3. `PlayerPickupController` находит предметы слоя 8 лучом из центра камеры (радиус 3 м).

### 3.2 Открытие инвентаря и подбор предметов

`PlayerInventoryToggle.Update` → `I` → `SetOpen()`:

- открытие: `Inventory.SetActive(true)`, курсор разблокирован;
- закрытие: `Inventory.SetActive(false)`, курсор захвачен, окно модификации закрывается.

Режим «коллекционной зоны» как в PUBG удалён: отдельного списка предметов рядом больше нет.

Подбор выполняет `PlayerPickupController`:

1. Луч из центра камеры (`layer 8`, 3 м) находит `ItemState`.
2. По клавише **E** вызывается `InventoryManager.TryPickupWorldItem`.
3. Создаётся 2D-экземпляр из `childrenUI`, в него копируется `ItemStateData` мира.
4. `TryStoreItem` выбирает место:
   - свободный слот экипировки, если предмет туда подходит;
   - надетые разгрузка, рюкзак, контейнер;
   - если места нет — предмет остаётся в мире.
5. При успехе 3D-объект уничтожается. Стаки больше 1 в слот не кладутся — уходят в контейнер.
6. Если места нет, `PlayerPickupController` показывает сообщение «Нет свободного слота или контейнера».

`InventoryStorage.TryAddToContainer` умеет доливать стаки, проверять сетку/карманы и
восстанавливать отложенное содержимое контейнера перед добавлением.

### 3.3 Перетаскивание предмета

`ItemInstance` реализует `IBeginDragHandler/IDragHandler/IEndDragHandler/IPointerClickHandler`
и делегирует в `InventoryManager`:

| Событие | Что происходит |
|---|---|
| `OnBeginDrag` | `ItemInstance.OnDragStart()`: сохранить `savedPosition`/`savedParent`, вызвать `ItemGridPlacement.CaptureStartState()`, освободить ячейки, включить визуал перетаскивания, переехать в `InventoryManager.transform` |
| `OnDrag` | `UpdateDragPosition(Input.mousePosition)` → `ItemView.SetPosition` |
| `OnEndDrag` | `InventoryManager.HandleEndDrag(item, target)` |
| `OnPointerClick` | ЛКМ → заглушка, ПКМ → `ContextMenuUI.Show` |

`HandleEndDrag`:

1. `item.TryPlaceOn(target)` — попытка положить.
2. Если положили — `item.OnDragEnd()`.
3. Если бросили в пустоту — `DropItem`.
4. Иначе — `ReturnToStart` (тряска + возврат).

`ItemInstance.TryPlaceOn(target)` — порядок приоритетов:

1. **Слот экипировки** (`EquipmentSlot` в родителях target) — `CanEquip` + `EquipItem`.
2. **Стак** — если `targetItem.GetItemState().data == draggedItem...data` и `maxStackSize > 1`,
   переносит `min(остаток, свободное место)`, обновляет UI; при полном переносе уничтожает dragged.
3. **Сетка** — `InventoryGrid` в родителях target, `draggedItem.CanPlaceInGrid` + `PlaceInGrid`.

Важно: иконка `Image` в 2D-префабе предмета должна иметь **Raycast Target = true**.
Иначе клик проходит в ячейку/`ScrollRect` под предметом: в контейнере вместо переноса
начинается прокрутка. Во время drag `CanvasGroup.Blocks Raycasts` выключается
(`ItemView.StartDragVisuals`), поэтому перетаскиваемый предмет не перехватывает свой же drop.

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
- свободность всех целевых `InventoryCell` (`fullness == null`);
- доступность ячеек (`isAvailable`);
- принадлежность всех ячеек одному карману (`pocketId`) и фильтр кармана (`allowedCategories`).

Позиция считается от **центральной** ячейки предмета:
`offset = targetCell.gridSize - itemCells[itemCells.Count / 2].gridSize`.

`ItemGridPlacement.PlaceInGrid` → `ExecutePlacement`:

- сохраняет `sizeDelta`, `localScale`, якоря и pivot;
- добавляет/включает `LayoutElement.ignoreLayout = true` (иначе `GridLayoutGroup` ломает размер);
- `SetParent` с `worldPositionStays: true`, восстановление размеров;
- для чётных размеров и многоячеечных предметов сдвиг на пол-ячейки (`±25f`);
- `ItemView.SmoothMoveTo`;
- `OccupyCells` — `InventoryCell.fullness = item`.

### 3.5 Поворот

`InventoryManager.Update` при активном drag и нажатии `R` → `ItemInstance.RequestRotate()` →
`ItemGridPlacement.RequestRotate()`:

- переключает `isRotated`, крутит `ItemView` на `-90°`;
- `RotateGridCells` пересчитывает `InventoryCell.gridSize`:
  `baseX = i / H`, `baseY = i % H`; при повороте `gridSize = (baseY, W - 1 - baseX)`,
  где `W = size.x`, `H = size.y`.

### 3.6 Контейнеры

Все надетые контейнеры — **рюкзак и разгрузка** — показываются в правом доке карточками.

- Карточка в свёрнутом виде: заголовок с названием, стрелка `>` справа и большая иконка
  предмета; ширина ~320 px, высота ~300 px, не на всю панель.
- Раскрытие — **только по стрелке**; можно раскрыть обе карточки одновременно.
  Раскрытая карточка расширяется до ~800 px (не на всю панель) и высоты ~440 px.
- Состояние раскрытия (`backpackExpanded`/`rigExpanded`) запоминается на время сессии:
  при закрытии и повторном открытии инвентаря карточки остаются в том же положении.
- В раскрытом виде под заголовком показывается панель `Conteyner` с сеткой; сетка
  остаётся **50×15** с ячейкой 50 px, не масштабируется и прокручивается внутри секции.
- `InventoryContainerDock.OnEnable/Refresh` находит надетые контейнеры через
  `InventoryManager.FindEquippedContainer` и обновляет карточки; `DisplayDockSection` переносит панель
  в `Host`, скрывает её `Header`/`Frame`, отключает `ContainerSizeHandler`/`UIDragHandler`.
- `OnDisable` компонента возвращает раскрытые панели внутрь предметов и выключает их.
- Если контейнер не надет, его карточка скрывается.
- Пункт «Открыть» удалён: сетки доступны сразу при открытом инвентаре.
- `InventoryStorage.TryPlaceInFirstFreeCell(item)` ищет место в **надетых** рюкзаке/разгрузке
  (даже если карточка свёрнута); если места нет, снятая экипировка выбрасывается.

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
| Экипировать | `EquipeItem` | оружие/броня/шлем/рюкзак, не в слоте | ищет первый подходящий пустой слот |
| Снять | `UnequipAction` | предмет лежит в слоте | `EquipmentSlot.UnequipItem` |
| Разделить | `SplitAction` | стак > 1, не в слоте | делит пополам, клонирует объект |

Меню закрывается по `Escape` и по ПКМ вне предмета/слота/меню (raycast через `EventSystem`).

### 3.9 Стамина

`PlayerStamina` — отдельный компонент на `Player`; `PlayerMotor` только спрашивает и тратит.

- `EvaluateSprint(sprintHeld, moving)` — вызывается каждый кадр до расчёта скорости. Уже начатый
  бег можно продолжать до нуля; начать заново — только при `StaminaPercent > 12%`.
- `TryJump()` — если стамины меньше `jumpCost`, возвращает `false`, прыжок не выполняется.
- `Tick(dt, moving, grounded)` — после движения: бег ест `sprintDrain`; при перевесе
  (`carryWeight > maxCarryWeight`) ходьба ест `walkOverweightDrain * (1 + OverweightRatio * multiplier)`;
  иначе, спустя `regenDelay`, стамина восстанавливается.
- `PlayerStaminaBar` только читает `Stamina`, `MaxStamina`, `StaminaPercent`, `SprintBlocked`
  и рисует масштабируемый бар в `OnGUI`.

### 3.10 Тело и процедурная анимация

`PlayerProceduralBody` анимирует готовую модель `Player/VisualBody`, которая лежит в сцене:

- части модели (`Pelvis`, `UpperBody`, `Hip_L/R`, колени, стопы, плечи, локти) — обычные
  GameObject'ы сцены; ссылки на них сериализованы в `PlayerProceduralBody`;
- голова и глаза — дочерние объекты `GG/Head` (`HeadVisual`, `Eye_L/R`);
- материалы — ассеты в `Assets/Materials/Player`, в рантайме ничего не создаётся;
- старая капсула `GG` и сфера `Head` не удаляются — у них выключен `MeshRenderer`,
  коллайдеры продолжают работать;
- ноги: ось маха `Cross(up, localVelocity)`; направление берётся из `CharacterController.velocity`
  и переводится в локальные координаты тела, поэтому шаг работает вперёд/назад/вбок/по диагонали;
- фаза и амплитуда зависят от скорости (`walkCadence → runCadence`, `walkSwing → runSwing`);
- колени сгибаются на обратном махе, стопы подворачиваются, руки качаются в противофазе,
  корпус наклоняется в сторону движения;
- в приседе тело опускается на `(standHeight - controller.height) / 2`, колени добавляют `crouchKneeBend`.

---

## 4. Справочник файлов

`ItemData`, `ItemCategory` и `ItemState` разобраны в разделе 2 (модель данных) — здесь остальные скрипты.

Быстрый индекс:

| Файл | Классы | Раздел |
|---|---|---|
| `Player/PlayerMotor.cs` | `PlayerMotor` | 4.1 |
| `Player/PlayerPickupController.cs` | `PlayerPickupController` | 4.1a |
| `Player/PlayerCamera.cs` | `PlayerCamera` | 4.1b |
| `Player/PlayerInventoryToggle.cs` | `PlayerInventoryToggle` | 4.1c |
| `Inventory/Core/InventoryManager.cs` | `InventoryManager` | 4.2 |
| `Inventory/Core/InventoryStorage.cs` | `InventoryStorage` | 4.2a |
| `Inventory/UI/InventoryContainerDock.cs` | `InventoryContainerDock` | 4.2b |
| `Inventory/Core/ItemPlacementValidator.cs` | `ItemPlacementValidator`, `PlacementData` | 4.3 |
| `Inventory/Core/InitializeGrid.cs` | `InitializeGrid` | 4.4 |
| `Inventory/UI/ContainerGridVisuals.cs` | `ContainerGridVisuals` | 4.4a |
| `Inventory/Items/ItemData/ContainerGridValidator.cs` | `ContainerGridValidator` | 4.4b |
| `Inventory/Core/InventoryCell.cs` | `InventoryCell` | 4.5 |
| `Inventory/Core/InventoryGrid.cs` | `InventoryGrid` | 4.6 |
| `Inventory/Items/ItemInstance.cs` | `ItemInstance` | 4.7 |
| `Inventory/Items/ItemGridPlacement.cs` | `ItemGridPlacement` | 4.7a |
| `Inventory/Items/ItemView.cs` | `ItemView` | 4.8 |
| `Inventory/Items/ItemState.cs` | `ItemState`, `ItemStateData` | 2.3 |
| `Inventory/Items/ItemData/ItemData.cs` | `ItemData` | 2.1 |
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
| `Editor/ItemPrefabFactory.cs` | `ItemPrefabFactory`, `PrefabOverwriteMode` | 4.19a |
| `Editor/ItemPrefabPaths.cs` | `ItemPrefabPaths` | 4.19b |
| `Editor/ItemCreationWizard.cs` | `ItemCreationWizard`, `ItemCreationType` | 4.19c |
| `Editor/EffectDefinitionEditor.cs` | `EffectDefinitionEditor` | 4.19f |
| `Effects/EffectTargetMask.cs` | `EffectTargetMask` | 4.27 |
| `Effects/EffectVisibility.cs` | `EffectVisibility` | 4.27 |
| `Effects/EffectTag.cs` | `EffectTag` | 4.27 |
| `Effects/EffectTargets.cs` | `EffectTargets` | 4.27 |
| `Effects/IEffectTarget.cs` | `IEffectTarget` | 4.27 |
| `Effects/ActiveEffect.cs` | `ActiveEffect` | 4.27 |
| `Effects/EffectReceiver.cs` | `EffectReceiver` | 4.27 |
| `Effects/CharacterEffectAggregator.cs` | `CharacterEffectAggregator` | 4.27 |
| `Editor/ContainerGridPacker.cs` | `ContainerGridPacker` | 4.19e |
| `Editor/WeaponModSceneSetup.cs` | `WeaponModSceneSetup` | 4.19d |
| `Player/PlayerStamina.cs` | `PlayerStamina` | 4.20 |
| `Player/PlayerStaminaBar.cs` | `PlayerStaminaBar` | 4.21 |
| `Player/PlayerProceduralBody.cs` | `PlayerProceduralBody` | 4.22 |
| `Player/Health/BodyPartType.cs` | `BodyPartType` | 4.23 |
| `Player/Health/PlayerHealth.cs` | `PlayerHealth`, `BodyPartState` | 4.24 |
| `Player/Health/BodyPartHealth.cs` | `BodyPartHealth` | 4.25 |
| `Player/Health/BodyPartInspectionUI.cs` | `BodyPartInspectionUI` | 4.26 |
| `Effects/StatType.cs` | `StatType` | 4.27 |
| `Effects/StatModifier.cs` | `StatModifier`, `Mode` | 4.27 |
| `Effects/EffectDefinition.cs` | `EffectDefinition`, `StackPolicy` | 4.27 |

### 4.1 `Player/PlayerMotor.cs` (180 строк)

Движение игрока: WASD, бег, присед, прыжок, гравитация и единственный `CharacterController.Move`.
Не читает мышь, не управляет камерой и инвентарём. Итоговая скорость проходит через
`CharacterEffectAggregator.GetValue(StatType.MoveSpeed, ...)`, поэтому эффекты на ногах
складываются.

Поля: `view` (направление движения, обычно `GG/Head`), `staminaComponent`, `public speed = 5`,
`public gravity`, `public jumpHeight`, `coyoteTime = 0.12`, `jumpBuffer = 0.1`,
`landMinFallSpeed = 3`, `sprintSpeed = 8`, `crouchMoveSpeed = 2`, `crouchHeightRatio = 0.55`,
`crouchTransitionSpeed = 10`.

Свойства: `Controller`, `StandHeight`, `CurrentHeight`, `CrouchTransitionSpeed`, `IsCrouching`,
`IsSprinting`, `IsMoving`, `IsGrounded`, `MoveInput`, `InputEnabled`.
События: `Jumped`, `Landed(float strength)` — для камеры.

| Метод | Назначение |
|---|---|
| `Awake` | кэш `CharacterController`, `EnsureReferences` |
| `EnsureReferences` | ленивая инициализация `controller`, `view`, `PlayerStamina`; вызывается из `Awake` и каждого `Update` |
| `CaptureBase` | запоминает базовые `height`/`center` капсулы |
| `Update` | ввод WASD (если `InputEnabled`), присед/бег, `EvaluateSprint`, движение по взгляду, `UpdateStance`, прыжок, гравитация, `Move`, `PlayerStamina.Tick` |
| `UpdateStance` | плавно меняет высоту и центр капсулы при приседе |

`InputEnabled = false` отключает движение, присед, бег и прыжок — так `PlayerInventoryToggle`
блокирует игрока на время открытого инвентаря.

### 4.1a `Player/PlayerPickupController.cs` (145 строк)

- Луч из центра камеры, `LayerMask` = слой 8 (`Item`), дальность 3 м.
- По `E` вызывает `InventoryManager.TryPickupWorldItem`.
- Ссылки `playerCamera` и `inventoryToggle` назначаются в инспекторе; при отсутствии ищутся
  в детях `Player`, `Camera.main` — только резервный вариант.
- Взаимодействие запрещено, пока `PlayerInventoryToggle.IsOpen == true`.
- Показывает подсказку `E — подобрать`; при неудаче — «Нет свободного слота или контейнера».
- Подсказка создаётся в рантайме на отдельном Canvas (`ScreenSpaceOverlay`, sorting order 100).

### 4.1b `Player/PlayerCamera.cs` (206 строк)

Обзор от первого лица и камера. Не двигает игрока и не трогает инвентарь.

Поля: `head`, `body`, `motor`, `sensitivity` (0.1–9), `yRotationLimit` (0–90),
`headYawLimit` (5–120, по умолчанию 60), `bodyAlignSpeed = 240` (°/с, 0 = выключить),
`bobFrequency`, `bobAmount`, `bobSmoothing`, `bobSharpness`, `landShakeStrength`,
`jumpShakeStrength`, `impactDistance`, `impactRecovery`.

| Метод | Назначение |
|---|---|
| `Awake` | `EnsureReferences`, базовые позиции головы |
| `OnEnable/OnDisable` | подписка/отписка на `PlayerMotor.Jumped` и `Landed` |
| `Update` | при `InputEnabled` — `CameraLook`; при открытом инвентаре мышь не крутит камеру |
| `LateUpdate` | `AlignBodyToView`, `UpdateCameraHeight`, `UpdateCameraBob` |
| `CameraLook` | pitch — только `head.localRotation`; yaw — сначала голова, после `headYawLimit` тело догоняет |
| `AlignBodyToView` | плавный доворот тела и компенсация угла головы |
| `UpdateCameraHeight` | высота камеры следует за сжатием капсулы (`CurrentHeight / StandHeight`) |
| `UpdateCameraBob` | шаговая тряска, толчки от прыжка/приземления, возврат камеры |

### 4.1c `Player/PlayerInventoryToggle.cs` (53 строки)

Окно инвентаря и состояние ввода.

- `Awake` находит `InventoryManager` в детях, захватывает курсор.
- `Start` закрывает инвентарь, если сцена сохранена с открытым окном.
- `Update` — клавиша `I` → `Toggle()`.
- `SetOpen(bool)` включает/выключает `Inventory`, меняет курсор, ставит
  `PlayerMotor.InputEnabled = !open` и закрывает `WeaponModdingUI` при закрытии.
- `IsOpen` — единая точка проверки для подбора и бара стамины.

### 4.2 `Inventory/Core/InventoryManager.cs` (139 строк)

Singleton-координатор инвентаря. `Instance` — статическое свойство.

Поля: `targetCanvas`, `player`, `contextMenu`, `draggedItem`, `isDragging`, `storage`,
`dock`.

Публичный API:

| Метод | Назначение |
|---|---|
| `HandleLeftClick/RightClick` | ЛКМ-заглушка / показать меню |
| `HandleBeginDrag/HandleDrag/HandleEndDrag` | маршрутизация drag-событий |
| `TryPlaceInGridFirstFree(item, InitializeGrid, InventoryGrid)` | делегирует в `InventoryStorage` |
| `TryPlaceInFirstFreeCell(item)` | делегирует в `InventoryStorage` |
| `TryPickupWorldItem(GameObject)` | делегирует в `InventoryStorage` |
| `TryStoreItem(ItemInstance)` | делегирует в `InventoryStorage` |
| `TryAddItemToInventory(ItemInstance)` | алиас `TryStoreItem` |
| `DropItem(ItemInstance)` | делегирует в `InventoryStorage` |
| `FindEquippedContainer(ItemCategory)` | делегирует в `InventoryStorage` |
| `RefreshContainerDock()` | делегирует в `InventoryContainerDock` |
| `IsDragging()`, `GetDraggedItem()` | состояние drag |

Особенности:

- `Awake` уничтожает дубликат объекта, если `Instance` уже есть;
- `targetCanvas` ищется через `FindAnyObjectByType<Canvas>()` (Unity 6 API);
- `dock` находится через `GetComponentInChildren<InventoryContainerDock>(true)`;
- `storage` создаётся в `Awake`, логики хранения в самом менеджере нет.

### 4.2a `Inventory/Core/InventoryStorage.cs` (395 строк)

Слой хранения: подбор, слоты, контейнеры, стаки, выброс. Не MonoBehaviour.

| Метод | Назначение |
|---|---|
| `TryPickupWorldItem(GameObject)` | создать 2D-копию, сохранить, уничтожить 3D-объект |
| `TryStoreItem(ItemInstance)` | свободный слот → разгрузка → рюкзак → контейнер |
| `TryAddItemToInventory(ItemInstance)` | алиас `TryStoreItem` |
| `TryPlaceInFirstFreeCell(ItemInstance)` | первая свободная ячейка надетых рюкзака/разгрузки |
| `TryPlaceInGridFirstFree(...)` | первая свободная ячейка конкретной сетки |
| `FindEquippedContainer(ItemCategory)` | надетый контейнер по категории |
| `DropItem(ItemInstance)` | вернуть панель из дока, сохранить содержимое, заспавнить 3D |
| приватные `Capture/RestoreContainerContents`, `TryAddToContainer` | синхронизация содержимого контейнера |

`InventoryStorage` не управляет карточками и стрелками — только данными и панелями.

### 4.2b `Inventory/UI/InventoryContainerDock.cs` (305 строк)

MonoBehaviour на `InventoryFrame`. Отвечает только за правый док:

- `Refresh()` — находит надетые рюкзак/разгрузку и обновляет карточки;
- карточка в свёрнутом виде: заголовок, название, стрелка, большая иконка;
- по стрелке — раскрытие до ~800 px, внутри панель `Conteyner` с сеткой;
- `GetContainerPanel(ItemInstance)` — панель для `InventoryStorage`;
- `RestoreContainer(ItemInstance)` — вернуть панель в предмет (например, при выбросе);
- `OnDisable` возвращает все раскрытые панели в предметы.

Состояние `backpackExpanded`/`rigExpanded` хранится в компоненте и переживает
закрытие/открытие инвентаря в рамках сессии.

### 4.3 `Inventory/Core/ItemPlacementValidator.cs` (283 строки)

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

### 4.4 `Inventory/Core/InitializeGrid.cs` (356 строк)

Генератор сетки ячеек 1×1 внутри предмета/контейнера.

Поля: `prefabGrid`, `gridSize` (private), `public IsContainerGrid`, `public LimitColumns`,
`maxColumns = 20`, `public List<GameObject> listGrid`, `containerGrid`, `usePixelGaps`,
`pocketOriginX/Y`.

| Метод | Назначение |
|---|---|
| `Awake` | `TryInit()` |
| `TryInit` | если у родителя есть `ItemState.data` — взять `ContainerData.grid` (контейнер) или `size`, очистить детей, сгенерировать; для карманов вызывает `ContainerGridVisuals.Apply`; родитель ищется ручным обходом иерархии, поэтому работает и в закрытом инвентаре |
| `ChangeSize` | `RectTransform.sizeDelta`: ширина `columns * 50`, высота `gridSize.x * 50`; при пиксельном зазоре добавляет место под карманы, отключает `GridLayoutGroup` и расставляет ячейки вручную |
| `GenerateGrid` | создаёт `Cell_{i}_{j}`, задаёт `InventoryCell.gridSize = (i, j)`, слой как у родителя |
| `UpdateParentSize` | если родитель — `ItemState`, подгоняет его `RectTransform` под сетку |
| `ClearGrid` | удалить все ячейки |
| `GetDimensions` | `(maxX+1, maxY+1)` |
| `GetCellAt(Vector2Int)` | GameObject ячейки |
| `GetGridAt(Vector2Int)` | `InventoryCell` ячейки |
| `PreparePixelGaps(ContainerGridData)` | берёт `Pocket.pixelPosition`; для старых ассетов считает начала через `ContainerGridValidator.CalculatePocketOrigins` |
| `ApplyPixelCellLayout` | расставляет ячейки по пиксельным началам карманов; привязка к левому верхнему углу не зависит от смены pivot корня |
| `GetPixelCellLocalPosition(Vector2Int)` | позиция ячейки внутри своего кармана; ячейки одного кармана идут строго через 50 px, начало — из `pixelPosition` |
| `GetCellLocalPosition(Vector2Int)` | локальная позиция центра ячейки: при пиксельной раскладке — через карман, иначе по параметрам `GridLayoutGroup`; работает в неактивной иерархии |
| `FindInParents(Transform)` | статический поиск `InitializeGrid` по родителям без учёта `activeInHierarchy` |

Координаты: `x` — строка (вертикаль), `y` — столбец (горизонталь). Ячейка 50×50 px.

### 4.4a `Inventory/UI/ContainerGridVisuals.cs` (45 строк)

Разметка карманов контейнера. Вызывается из `InitializeGrid.TryInit`, когда у
`ContainerGridData` есть карманы.

- помечает ячейки `isAvailable`, `pocketId`;
- у доступных ячеек оставляет обычный вид ячейки, у недоступных скрывает `Image`;
- карманы разделяются только пиксельными промежутками — без overlay, подписей и рамок;
- центрирует небольшую сетку в окне контейнера;
- сам сетку не генерирует и правила размещения не проверяет.

### 4.4b `Inventory/Items/ItemData/ContainerGridValidator.cs` (321 строка)

Технические ограничения сеток и карманов:

- минимум `1×1` на карман;
- позиции не меньше `(0,0)`;
- карманы не пересекаются ни в логических ячейках, ни в пикселях;
- позиция кармана — `Pocket.pixelPosition` в пикселях от левого верхнего угла сетки;
- старый общий зазор `pocketGapPixels` (`0…32` px) проверяется только без `usePixelPositions`;
- максимум `50` строк, `20` колонок, `1000` ячеек;
- `maxColumns` не меньше фактического числа колонок.

Методы:

| Метод | Назначение |
|---|---|
| `Validate(ContainerGridData, requirePockets)` | полная проверка ассета |
| `ValidateLayout(...)` | проверка ручных позиций/размеров для мастера и preview |
| `ValidatePixelLayout(...)` | проверка покамерных позиций: неотрицательные координаты, отсутствие пересечений |
| `CalculatePocketOrigins(...)` | старый расчёт начал по логическим позициям и `pocketGapPixels`; нужен только для миграции |
| `CalculateBounds(...)` | расчёт размера сетки по карманам |

### 4.5 `Inventory/Core/InventoryCell.cs` (33 строки)

Класс `InventoryCell` (файл раньше назывался `Grid.cs`, переименован под класс).

Поля: `public GameObject fullness` (кто занимает), `public Vector2Int gridSize`.
Свойство `IsOccupied => fullness != null`. Методы: `Awake` (обнуление), `Clear()`.
Статический `FindInParents(Transform)` ищет `InventoryGrid` по родителям без учёта
`activeInHierarchy`.

### 4.6 `Inventory/Core/InventoryGrid.cs` (19 строк)

Фильтр контейнера.

Поля: `allowedTypes` (`ItemCategory`), `isContainerGrid` (запрет вложенных контейнеров).
Метод `CanAcceptItem(ItemCategory)` — `(allowedTypes & itemCategory) != 0`.

> `allowedTypes` по умолчанию `None` — если не настроить, в сетку нельзя положить ничего.

### 4.7 `Inventory/Items/ItemInstance.cs` (418 строк)

Координатор предмета: связывает `ItemState`, `ItemView`, `InitializeGrid` и `ItemGridPlacement`.

Реализует интерфейсы: `IPointerClickHandler`, `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`,
`IInventoryClickHandler`, `IEffectTarget`.

`Effects` — receiver эффектов предмета; `TargetMask` — `Weapon` для оружия,
`Item|Container` для контейнеров, иначе `Item`. `ApplyEquippedEffects`/`RemoveEquippedEffects`
применяют и снимают эффекты из `EquippableData.effects` при надевании/снятии.

Поля: `itemState`, `itemView`, `itemGrid`, `placement`, `savedPosition`, `savedParent`,
`isInitialized`, `dropHandled`.

Публичный API:

| Метод | Назначение |
|---|---|
| `ReleaseCells()` | освободить занятые ячейки (делегирует в `ItemGridPlacement`) |
| `GetSavedParent()` | родитель до drag |
| `Init()` | идемпотентная инициализация: `TryInit` сетки, обновление стака, скрытие `Conteyner` |
| `UpdateStackUI()` | обновить текст стака |
| `OnDragStart/UpdateDragPosition/OnDragEnd` | drag-логика и визуал |
| `TryPlaceOn(GameObject)` | положить предмет в слот, стак или сетку под курсором |
| `ReleaseOwnerSlot()` | сообщить слоту, что предмет больше не принадлежит ему |
| `RequestRotate()` | поворот на 90° через `ItemGridPlacement` |
| `CanPlaceInGrid(GameObject, InventoryGrid)` | проверка + поиск рядом через `ItemGridPlacement` |
| `PlaceInGrid(GameObject)` | размещение и занятие ячеек через `ItemGridPlacement` |
| `ReturnToStart()` / `ReturnToStart(Vector3, Transform)` | возврат с тряской |
| `TryGetOccupiedCellsCenter(out Vector3)` | центр занятых ячеек через `ItemGridPlacement` |
| `CreateStackCopy(int)` | отделить часть стака в клон |
| `GetItemState()`, `GetStateData()` | доступ к состоянию |
| `HandleLeftClick/HandleRightClick/...` | реализация `IInventoryClickHandler` |

Приватно: `HideItemGridVisuals` (скрыть служебную сетку), `OnDestroy` (освобождение ячеек).
Размещение под курсором, поворот и занятые ячейки живут в `ItemGridPlacement`; `ItemInstance`
остаётся точкой входа для UI/слотов и не разрастается.

### 4.7a `Inventory/Items/ItemGridPlacement.cs` (242 строки)

Не MonoBehaviour: создаётся в `ItemInstance.Awake`, получает `ItemState`, `ItemView` и `InitializeGrid`.

| Метод | Назначение |
|---|---|
| `BindGrid(InitializeGrid)` | привязать/обновить сетку после `Init` |
| `CaptureStartState()` | запомнить поворот и занятые ячейки перед drag |
| `ReleaseCells()` | освободить ячейки |
| `RestoreSavedCells()` | вернуть занятие ячеек после неудачного drag |
| `CanPlaceInGrid(...)` / `PlaceInGrid(...)` | проверка и размещение |
| `RequestRotate()` / `RestoreStartRotation()` | поворот и возврат к исходному |
| `ApplyReturnOffset()` | компенсация ±25 px для чётных предметов |
| `TryGetOccupiedCellsCenter(out Vector3)` | центр блока ячеек |

`ExecutePlacement` берёт позицию ячейки через `InitializeGrid.GetCellLocalPosition`,
поэтому размещение работает и в закрытом контейнере, где `GridLayoutGroup` ещё не рассчитал
`localPosition` детей.

### 4.8 `Inventory/Items/ItemView.cs` (297 строк)

Только визуал: иконка, текст стака, анимации. Если иерархия неактивна
(закрытый инвентарь), `SmoothMoveTo`, `SmoothRotateTo` и `PlayShakeEffect` не запускают
корутины, а сразу ставят конечную позицию/поворот.

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

### 4.9 `Inventory/Equipment/EquipmentSlot.cs` (281 строка)

Слот экипировки: `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerClickHandler`.

Поля: `public allowedCategories`, `iconImage`, `defaultSprite`, `currentItemData`, `equippedInstance`, `hasItem`,
`effectAggregator`.
Свойство `IsEmpty => !hasItem`.

При экипировке предмет регистрируется в `CharacterEffectAggregator` и получает пассивные
эффекты из `EquippableData.effects`; при снятии/выбросе они снимаются и receiver удаляется.

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

### 4.10 `Inventory/Equipment/EquipmentSlotData.cs` (15 строк)

`ScriptableObject` (`Create → Inventory → Equipment Slot`): `slotName`, `slotLabel`,
`allowedCategories`, `defaultSprite`, `anchoredPosition`, `size` (по умолчанию 100×100).

### 4.11 `Inventory/Equipment/EquipmentSlotsManager.cs` (20 строк)

Поля: `slotPrefab`, `List<EquipmentSlotData> slotDatas`.
`Awake` инстанциирует префаб на каждый элемент и вызывает `slot.Setup(data)`.

### 4.12 `Inventory/ContextMenu/ContextMenuUI.cs` (179 строк)

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

### 4.13 `Inventory/ContextMenu/ItemActions.cs` (172 строки)

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

### 4.19 `Editor/ItemPrefabGenerator.cs` (39 строк)

`[CustomEditor(typeof(ItemData), true)]`, Editor-only. Показывает поля данных, ошибки
`ItemPrefabFactory.Validate` и кнопки «Сгенерировать 2D и 3D», «Редактировать в мастере»,
«Открыть мастер создания предмета». Сама сборка вынесена в `ItemPrefabFactory`, чтобы инспектор
не занимался файлами и префабами.

### 4.19a `Editor/ItemPrefabFactory.cs` (435 строк)

Единая фабрика prefabs. Используется инспектором и мастером.

- `Create2D`:
  - `ItemTemplate`;
  - `ItemState.data`, `currentStack = 1`, `currentDurability = maxDurability`;
  - `ItemView.canvasGroup/iconImage/iconRect/stackText`;
  - размер корня и `Grid` по `size`;
  - `InitializeGrid.prefabGrid = InventoryCell`;
  - иконка с `raycastTarget = true`;
  - для `ContainerData` — панель `Conteyner` из `ContainerTemplate`;
    размер сетки учитывает покамерные пиксельные позиции.

- `Create3D`:
  - `Item3DTemplate`;
  - меш и материалы из `ItemData.sourceModel3D`;
  - `BoxCollider` по `bounds`;
  - `Rigidbody` с массой из `weight` и `FreezeRotation`;
  - слой 8 (`Item`);
  - `ItemState.data`, `childrenUI = modelPrefab2D`, `currentDurability = maxDurability`.

- `Generate` всегда вызывает 2D, затем 3D, и сам проставляет
  `ItemData.modelPrefab2D/modelPrefab3D`.

Режимы: `Overwrite`, `SkipExisting`, `CreateCopy`.

### 4.19b `Editor/ItemPrefabPaths.cs` (101 строка)

Пути и имена:

- `sourceModel3D` — исходная модель;
- `prefabName` — имя папки и файлов prefabs (если пусто — `itemName`);
- папки по типу данных: Weapons, Containers, Equipment, Consumables, Ammo, Mods, Misc;
- `ItemPrefabPaths.EnsureFolder` создаёт папки через `AssetDatabase`.

### 4.19c `Editor/ItemCreationWizard.cs` (1760 строк)

Мастер создания и редактирования предметов и эффектов. Две вкладки: «Предмет» и «Эффект».

Вкладка «Предмет»:

- открыть создание: `Tools → Contract → Мастер создания предмета`;
- открыть редактирование: кнопка «Редактировать в мастере» в инспекторе `ItemData`;
- при редактировании тип предмета заблокирован, данные обновляются в том же ассете;
- выбор типа (`Weapon`, `Armor`, `Clothing`, `Container`, `Backpack`, `Rig`,
  `Consumable`, `Ammo`, `WeaponMod`, `Generic`);
- общие поля: `id`, `itemName`, `prefabName`, `category`, `size`, `weight`,
  `maxStackSize`, `maxDurability`, `icon`, `sourceModel3D`;
- поля конкретного типа;
- у расходников — список `EffectDefinition` с кнопками добавить/удалить/создать;
- у экипировки — список `EquippableData.effects`, эффекты применяются при надевании;
- для разгрузки — количество карманов, размер и позиция каждого кармана в пикселях
  (`X`, `Y` от левого верхнего угла сетки);
  карманы можно сворачивать, копировать и раскладывать автоматически;
- копия ищет свободное место в пикселях, новые карманы не сдвигают уже настроенные;
- отдельный блок предпросмотра сетки: размер, количество ячеек, карманов и доступных
  ячеек, схема с пиксельными промежутками; невалидные карманы подсвечиваются красным;
- создание/обновление `ContainerGridData` для контейнеров;
- выбор режима перезаписи (только при создании);
- создание или обновление данных и вызов `ItemPrefabFactory.Generate`.

Вкладка «Эффект»:

- открыть: `Tools → Contract → Мастер эффектов` или вкладка в окне мастера предмета;
- создание нового `EffectDefinition` или загрузка существующего;
- поля: `id`, `effectName`, `description`, `icon`, `duration` (0 = постоянно),
  `stackPolicy`, `maxStacks`, `allowedTargets`, `visibility`, `tags`, список `StatModifier`;
- `allowedTargets` ограничивает цели: `Legs`, `Arms`, `Weapon`, `Item`, `Character`;
- `visibility` определяет, когда эффект появится в HUD;
- модификатор: `StatType`, режим `Add`/`Multiply`, значение;
  `Multiply = 0.25` означает `+25%`;
- путь `Assets/Data/Effects/<имя>.asset`, режим перезаписи, проверка уникальности `id`;
- создание или обновление ассета без `ItemPrefabFactory`.

### 4.19e `Editor/ContainerGridPacker.cs` (88 строк)

Автоматическая упаковка карманов:

- вход — список размеров карманов;
- карманы ставятся вплотную в логических ячейках, затем получают пиксельные позиции;
- перебирает ширину и выбирает наиболее сбалансированную сетку;
- возвращает размер сетки и позиции карманов.

### 4.19d `Editor/WeaponModSceneSetup.cs` (113 строк)

Editor-only инструмент настройки тестовой сцены: слот разгрузки, окно модификации,
расстановка заглушек. Использует пути `Assets/Data/Equipment/Slots/...` и
`Assets/Prefabs/Items/...`.

### 4.19f `Editor/EffectDefinitionEditor.cs` (19 строк)

`[CustomEditor(typeof(EffectDefinition))]`, Editor-only. Показывает поля эффекта и кнопки
«Редактировать в мастере» и «Создать новый эффект». Открывает вкладку «Эффект» в
`ItemCreationWizard`.

### 4.20 `Player/PlayerStamina.cs` (102 строки)

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

### 4.21 `Player/PlayerStaminaBar.cs` (83 строки)

Отладочный бар, только чтение `PlayerStamina`.

- `[RequireComponent(typeof(PlayerStamina))]`, ссылки на стамину и `PlayerInventoryToggle`
  берутся в `Awake`.
- `showStaminaBar` — включение/выключение; `hideWhenInventoryOpen` скрывает бар, когда
  `PlayerInventoryToggle.IsOpen == true`.
- Базовые размеры для 1080p: `baseWidth = 280`, `baseHeight = 20`, `baseMargin = 16`,
  `baseFontSize = 14`; масштаб `Clamp(Screen.height / 1080, minScale 0.8, maxScale 2.5)`.
- Позиция — нижний левый угол, `OnGUI` + `EventType.Repaint`.
- Цвет: красный → зелёный по проценту; серый `[LOCK]`, когда `SprintBlocked`.
- `GUIStyle` кэшируется по размеру шрифта.

### 4.22 `Player/PlayerProceduralBody.cs` (181 строка)

Анимирует готовую модель игрока, которая лежит в сцене: `Player/VisualBody`.
Скрипт ничего не создаёт и не удаляет — только вращает сохранённые в сцене части.

- `motor` (`PlayerMotor`) даёт высоту капсулы и высоту стойки; `bodyAnchor = motor.transform`.
- Ссылки на `visualRoot`, `upperBody`, бёдра, колени, стопы, плечи и локти сериализованы
  в сцене; если поле пустое, `ResolveRig` ищет части по именам (`VisualBody/UpperBody`, `Hip_L` и т.д.).
- Модель: `VisualBody → Pelvis, UpperBody, Hip_L/R`; в `UpperBody` — торс, шея, плечи,
  предплечья, кисти; в ногах — бедро → колено → голень → стопа. Голова и глаза —
  дочерние объекты `GG/Head`.
- Материалы — ассеты в `Assets/Materials/Player` (`Skin`, `Shirt`, `Pants`, `Shoes`, `Eyes`),
  `Shader.Find("Standard")` (Built-in RP). В рантайме материалы не создаются.
- `MeshRenderer` старой капсулы `GG` и сферы `Head` выключены в сцене; их старые
  коллайдеры тоже выключены — хитбоксы теперь у частей `VisualBody`.
- Анимация в `Update`: `controller.velocity` → локальные координаты; ось маха `Cross(up, direction)`;
  фаза `walkCadence → runCadence`, амплитуда `walkSwing → runSwing`; колени и стопы сгибаются,
  руки в противофазе, корпус наклоняется; в приседе тело опускается и колени добавляют `crouchKneeBend`.
- Настройки: `walkSwing = 24`, `runSwing = 42`, `walkCadence = 7`, `runCadence = 14`,
  `leanAngle = 7`, `crouchKneeBend = 45`, `smoothing = 12`.
- Если части модели не найдены, `Start` пишет ошибку и отключает компонент.

### 4.23 `Player/Health/BodyPartType.cs`

`enum` из 16 зон: `Head`, `Neck`, `Torso`, `Pelvis`, `LeftThigh`, `LeftShin`, `LeftFoot`,
`RightThigh`, `RightShin`, `RightFoot`, `LeftUpperArm`, `LeftForearm`, `LeftHand`,
`RightUpperArm`, `RightForearm`, `RightHand`. Порядок используется в инспекторе и UI.

### 4.24 `Player/Health/PlayerHealth.cs`

Центральное состояние здоровья игрока; не зависит от модели и коллайдеров.

- Поля: `bodyParts` (список `BodyPartState { part, maxHealth, currentHealth }`),
  `defaultMaxHealth = 100`.
- `EnsureAllParts` в `Awake` добавляет недостающие зоны; `FillAllParts` пересоздаёт все 16
  с `defaultMaxHealth`; `Reset` делает это при добавлении компонента.
- API: `GetHealth`, `GetMaxHealth`, `GetHealthPercent`, `IsAlive`, `ApplyDamage`, `Heal`,
  `SetHealth`, `ResetAll`, `TotalHealth`, `TotalMaxHealth`.
- Перегрузка `ApplyDamage(part, DamageInfo)` пока применяет `info.amount`; броня и
  сопротивления будут подключены отдельным этапом.
- События: `HealthChanged(part, current, max)` при изменении, `PartDied(part)` при переходе
  из > 0 в 0. Общей смерти игрока пока нет — это решается позже.
- Значения по умолчанию — 100 на зону; в инспекторе можно задать разные `maxHealth`.

### 4.25 `Player/Health/BodyPartHealth.cs`

Маркер части тела на объекте с коллайдером.

- Поля: `playerHealth`, `partType`; в `Awake`/`OnValidate` ищет `PlayerHealth` у родителя.
- Реализует `IEffectTarget`: `TargetMask` — бит этой части тела, `Effects` — её receiver.
- Периодический тик эффекта наносит урон/лечение именно этой зоне; `canKill = false`
  оставляет минимум 1 HP.
- Свойства `CurrentHealth`, `MaxHealth`, `HealthPercent` читают центральные данные;
  `ApplyDamage` / `Heal` перенаправляют в `PlayerHealth`.
- Хп не хранит — источник истины только `PlayerHealth`; компонент нужен для raycast/урона.
- `ContextMenu("Тест: 10 урона")` — быстрый тест из инспектора.

### 4.26 `Player/Health/BodyPartInspectionUI.cs`

Временная заглушка осмотра тела (`OnGUI`).

- `F1` — открыть/закрыть, `Esc` — закрыть; `showHint` рисует подсказку внизу.
- При открытии вызывает `CharacterEffectAggregator.InspectAll()` — это debug-осмотр,
  который раскрывает все активные эффекты. В будущем осмотр будет через 3D-модель в инвентаре.
- Показывает список видимых эффектов с целью, стаками и временем.
- Список всех зон из `PlayerHealth` с полосками HP и общим итогом.
- При `inspectUnderCrosshair` бросает луч из центра камеры (`RaycastAll`,
  `QueryTriggerInteraction.Collide`): ближайший `BodyPartHealth` показывается как «В фокусе».
  Свои служебные коллайдеры (например, `BoxCollider` игрока) игнорируются, чужая геометрия
  закрывает обзор.
- Полноценный UI медосмотра и логика лечения позже.

### 4.27 Эффекты — `Assets/Script/Effects/`

| Файл | Класс(ы) | Назначение |
|---|---|---|
| `StatType.cs` | `StatType` | скорость, стамина, вес, урон, темп, отдача, точность, сопротивления |
| `StatModifier.cs` | `StatModifier`, `Mode` | один модификатор: `Add` или `Multiply` и значение |
| `EffectTargetMask.cs` | `EffectTargetMask` | биты частей тела + `Character`, `Weapon`, `Item`, `Container` |
| `EffectTargets.cs` | `EffectTargets` | `ForBodyPart`: бит цели по `BodyPartType` |
| `EffectVisibility.cs` | `EffectVisibility` | `InspectTarget`, `VisibleOnApply`, `Symptom`, `KnownFromItem` |
| `EffectTag.cs` | `EffectTag` | теги для лечения, UI и группировки |
| `EffectDefinition.cs` | `EffectDefinition`, `StackPolicy` | ScriptableObject эффекта: цели, видимость, теги, длительность, стаки, модификаторы, периодические тики |
| `DamageType.cs` | `DamageType` | баллистический, термический, электрический, химический, кровотечение, радиация |
| `DamageInfo.cs` | `DamageInfo` | урон, тип, часть тела, источник, пробитие |
| `PeriodicEffect.cs` | `PeriodicEffect` | урон/лечение за тик, интервал, `canKill` |
| `ActiveEffect.cs` | `ActiveEffect` | runtime: definition, source, stacks, remainingTime, discovered, targetMask, таймеры |
| `IEffectTarget.cs` | `IEffectTarget` | контракт цели: `TargetMask`, `Effects` |
| `EffectReceiver.cs` | `EffectReceiver` | активные эффекты одной цели, таймеры, стаки, `GetValue`, периодические тики, обнаружение |
| `CharacterEffectAggregator.cs` | `CharacterEffectAggregator` | персонаж + 16 частей тела + надетые не-оружейные предметы |

### Как работает

- `EffectReceiver.Apply` проверяет `allowedTargets` цели. Если маска `None` (старый ассет),
  эффект считается совместимым со всеми целями.
- `EffectReceiver` хранит эффекты независимо для каждой цели. Перелом левой ноги лежит
  на `LeftThigh`, правой — на `RightThigh`.
- `CharacterEffectAggregator.GetValue` складывает модификаторы со всех частей тела и
  надетых предметов. Два перелома ног по `-20%` дают `-40%` скорости.
- Оружие в агрегатор персонажа не входит: эффекты оружия должны читаться отдельным
  оружейным агрегатором.
- `ItemInstance` реализует `IEffectTarget`; `TargetMask` зависит от категории:
  оружие → `Weapon`, контейнер → `Item|Container`, остальное → `Item`.
- `EquippableData.effects` применяются к receiver предмета при экипировке и снимаются
  при снятии (`ItemInstance.ApplyEquippedEffects`/`RemoveEquippedEffects`).
- `BodyPartHealth` — цель с битом своей части тела.
- `EffectReceiver.Tick` обрабатывает периодические тики; `BodyPartHealth` применяет
  урон/лечение к своей зоне, `CharacterEffectAggregator` для эффектов на персонаже
  использует `periodicFallbackPart` (по умолчанию торс).
- `DamageInfo` пока несёт только сумму; броня и сопротивления ещё не применяются.
- `EffectReceiver.MarkAllDiscovered` раскрывает эффекты; `F1`-осмотр вызывает
  `CharacterEffectAggregator.InspectAll` для debug.
- Покрытие брони/одежды и сопротивления по типам урона пока не подключены —
  это следующий шаг.

---

## 5. Где что висит в сцене

| Объект | Компоненты |
|---|---|
| `Player` | `CharacterController`, `PlayerMotor`, `PlayerCamera`, `PlayerInventoryToggle`, `PlayerPickupController`, `PlayerStamina`, `PlayerStaminaBar`, `PlayerProceduralBody`, `PlayerHealth`, `BodyPartInspectionUI`, `CharacterEffectAggregator` |
| `Player/GG` | `MeshRenderer` (капсула, выключен в сцене), `CapsuleCollider`, дочерние `Head`, `TargetHand`, `TargetBackPack` |
| `Player/GG/Head` | `MeshRenderer` (сфера, выключен в сцене), `SphereCollider`, `Main Camera`; дочерние `HeadVisual`, `Eye_L/R` |
| `Player/VisualBody` | модель игрока в сцене: `Pelvis`, `UpperBody`, `Hip_L/R` и конечности; материалы в `Assets/Materials/Player`; на 16 частях `BodyPartHealth` + trigger-коллайдеры |
| `Player/Canvas` | `Canvas`, `CanvasScaler` (ScaleWithScreenSize 2560×1440, match height) |
| `Player/Canvas/Inventory` (или `Canvas/Inventory`) | `InventoryManager`, `WeaponModdingUI`, `InventoryResponsiveLayout` |
| `Player/Canvas/Inventory/InventoryFrame` | инстанс `InventoryFrame.prefab`: `InventoryContainerDock`, фон, шапка, футер, силуэт, правый док, сетка |
| `Canvas` | `ContextMenuUI`, слоты экипировки, `Conteyner` |
| `Canvas/.../Slots` | `EquipmentSlotsManager` (создаёт `EquipmentSlot`, включая слот `Rig`) |
| мир | префабы `*_3D`: `ItemState` + коллайдер, слой 8 (подбор по `E`) |
| `TEST_Content` | тестовые мировые объекты: `KVAZIR_3D`, `AK-47_3D`, `WeaponMods_Placeholders` |

`PlayerInventoryToggle` сам находит `InventoryManager` в детях и подставляет его GameObject в
`inventory`, поэтому имя объекта не важно, важен факт наличия компонента.
`PlayerStamina`/`PlayerStaminaBar` добавлены на `Player` в сцене, их настройки сохраняются.
`PlayerProceduralBody` уже стоит на `Player`; модель `Player/VisualBody` редактируется в сцене,
а не создаётся скриптом.
`PlayerHealth` и `BodyPartInspectionUI` тоже стоят на `Player`; на каждой части
`Player/VisualBody` и на `GG/Head/HeadVisual` висит `BodyPartHealth`. Коллайдеры частей —
trigger: для осмотра/урона нужен `QueryTriggerInteraction.Collide`.

---

## 6. Соглашения и числа

| Константа | Где | Значение |
|---|---|---|
| Размер ячейки | `InitializeGrid.ChangeSize`, `GenerateGrid`, `ItemPrefabFactory` | 50×50 px |
| Сдвиг чётных предметов | `ItemGridPlacement.ExecutePlacement`, `ApplyReturnOffset` | ±25 px |
| Ожидание после drag из слота | `EquipmentSlot.CheckSlotAfterDrag` | 0.3 с |
| Дистанция выброса | `InventoryStorage.DropItem` | 3 м вперёд, +1.5 м |
| Лимит колонок контейнера | `ContainerGridData.maxColumns` | по умолчанию 20 |
| Лимит `maxColumns` | `InitializeGrid` | 20 |
| Границы панели контейнера | `ContainerSizeHandler` | 200…1250 × 200…800 |
| Референс UI | `CanvasScaler` на `Player/Canvas` | 2560×1440, match height |
| Радиус подбора | `PlayerPickupController.pickupRange` | 3 м |
| Клавиша подбора | `PlayerPickupController.pickupKey` | `E` |
| Слой подбора | `PlayerPickupController.pickupMask` | 8 (`Item`) |
| Зон здоровья | `BodyPartType` | 16 (4 корпус + 6 ног + 6 рук) |
| Хп зоны по умолчанию | `PlayerHealth.defaultMaxHealth` | 100 |
| Клавиша осмотра | `BodyPartInspectionUI.toggleKey` | `F1` |
| Дальность осмотра | `BodyPartInspectionUI.inspectDistance` | 100 м |
| Опорная ширина группы слотов | `InventoryResponsiveLayout.slotsWidth` | 2800 px (масштаб 0.5…1) |
| Смещение группы слотов | `ManagerSlots.anchoredPosition` | x = -450, y = -25 |
| Правый док | `InventoryFrame/MainPanel/Dock` | 38% ширины панели, отступы 18/112/62 |
| Сетка разгрузки | `RazgruzkaGrid` | размер, количество карманов и их пиксельные позиции задаются в мастере |
| Корень эффектов | `ItemCreationWizard.EffectRoot` | `Assets/Data/Effects` |
| Длительность эффекта | `EffectDefinition.duration` | 10 с по умолчанию, 0 = постоянно |
| Максимум стаков | `EffectDefinition.maxStacks` | 1 по умолчанию |
| Мин. размер кармана | `ContainerGridValidator.MinPocketSize` | 1×1 |
| Позиция кармана | `ContainerGridData.Pocket.pixelPosition` | пиксели от левого верхнего угла |
| Старый общий зазор | `ContainerGridData.pocketGapPixels` | только для старых ассетов, `0…32` px |
| Макс. сетка | `ContainerGridValidator` | 50 строк × 20 колонок, 1000 ячеек |
| Скорости игрока | `PlayerMotor` | ходьба 5, бег 8, присед 2 м/с |
| Coyote time / буфер прыжка | `PlayerMotor` | 0.12 / 0.1 с |
| Поворот головы | `PlayerCamera` | pitch ±88°, yaw ±60° до подключения тела |
| Доворот тела к камере | `PlayerCamera.bodyAlignSpeed` | 240 °/с |
| Стамина | `PlayerStamina` | 100 макс, реген 12/с, пауза 0.8 с, бег −15/с, перевес −10/с, прыжок −15, порог 12% |
| Шаговая тряска | `PlayerCamera` | частота 12, амплитуда 0.045 м, сглаживание 20, резкость 0.35 |
| Прыжок/приземление | `PlayerMotor`, `PlayerCamera` | мин. падение 3 м/с; подброс 0.25, посадка 0.6, сдвиг 0.08 м, затухание 9 |
| Анимация тела | `PlayerProceduralBody` | мах 24°/42°, темп 7/14, наклон 7°, сгиб колен в приседе 45° |
| Бар стамины (1080p) | `PlayerStaminaBar` | 280×20 px, масштаб 0.8…2.5 от высоты экрана |

Размер предмета: `size = (высота, ширина)`. То есть `size.x` — количество строк,
`size.y` — количество столбцов. Это же соглашение используется в генераторе префабов.

---

## 7. Рецепты

### 7.1 Добавить новый предмет

1. `Create → Inventory → Item Data`, заполнить `itemName`, `icon`, `category`, `size`,
   `maxStackSize`, `modelPrefab2D`, `modelPrefab3D` (для контейнера — ещё `ContainerData.grid`).
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
(`cellSize`), `ItemPrefabFactory.Create2D`, `ItemPrefabFactory.CreateContainerPanel`.
Менять все четыре согласованно, иначе префабы и рантайм разъедутся.

### 7.5 Настроить стамину

1. На `Player` в Play Mode появится `PlayerStamina` (или добавь компонент вручную в edit mode).
2. Крути `maxStamina`, `staminaRegen`, `sprintDrain`, `jumpCost`, `sprintResumeThreshold`.
3. Перевес: выставь `carryWeight` больше `maxCarryWeight` — ходьба начнёт тратить стамину.
4. Бар: `PlayerStaminaBar`, снять `showStaminaBar` — выключить; размеры — `baseWidth/baseHeight`.

### 7.6 Настроить тестовое тело и походку

1. Модель — объект `Player/VisualBody` в сцене, её части можно двигать и заменять в edit mode.
2. `PlayerProceduralBody` на `Player` только анимирует модель; параметры — в его инспекторе.
3. `walkSwing`/`runSwing` — амплитуда ног, `walkCadence`/`runCadence` — частота шагов.
4. `leanAngle` — наклон корпуса, `smoothing` — плавность, `crouchKneeBend` — сгиб в приседе.
5. Ссылки на части модели (`visualRoot`, `upperBody`, `Hip_L/R`, `Knee_L/R`, `Ankle_L/R`,
   `Shoulder_L/R`, `Elbow_L/R`) назначены в сцене; пустые поля ищутся по именам.

---

## 8. Техдолг и известные проблемы

### 8.1 Кодировки (важно)

Все `.cs` с русскими комментариями сохранены в **Windows-1251 без BOM** (55 файлов),
UTF-8-файлов среди скриптов нет; 5 файлов содержат только ASCII.
Это соответствует правилу из `AGENTS.md`.

Правило: перед правкой проверять кодировку конкретного файла и сохранять её; полностью
перезаписывать файл в другой кодировке нельзя — русские комментарии превратятся в мусор.

`docs/**/*.md` и `AGENTS.md` — UTF-8.

### 8.2 Хрупкие места

- `InventoryStorage.TryAddToContainer` ищет панель `Conteyner` по имени (`transform.Find`) —
  переименование узла в префабе контейнера ломает подбор.
- `InventoryCell.Awake` обнуляет `fullness`. Ячейки, созданные в неактивной иерархии,
  нельзя заполнять до активации — `TryPickupWorldItem` на время подбора временно включает инвентарь.
- У иконки `Image` в 2D-префабе предмета должен быть включён `Raycast Target`, иначе
  предмет не ловит начало drag и события уходят в `ScrollRect` контейнера.
- В закрытом контейнере `GridLayoutGroup` ещё не расставил ячейки. Позиции нужно брать
  через `InitializeGrid.GetCellLocalPosition`, а не `targetCell.transform.position`,
  иначе предмет встаёт в центр сетки.
- `PlayerInventoryToggle.Start` закрывает инвентарь, если сцена сохранена с ним открытым; слоты
  должны успеть создать свои `Awake` — не переносить это в `PlayerInventoryToggle.Awake`.
- `ItemInstance.Awake` падает в `Debug.LogError`, если нет `ItemState`, но продолжает работу.
- `ItemInstance.OnDragStart` использует `GetComponentInParent<InventoryManager>()` — предмет обязан
  находиться под объектом с менеджером.
- `ContextMenuUI.PositionMenu` передаёт `null` как камеру в `WorldToScreenPoint` — корректно только
  для `Screen Space - Overlay`.
- `EquipmentSlot.CheckSlotAfterDrag` полагается на магические 0.3 с (`shakeDuration = 0.25`).
  Изменили анимацию — правьте и ожидание.
- `ItemInstance.ReturnToStart` (ветка без `ItemView`) — резервная; `ItemView` добавляется
  автоматически, поэтому на практике используется только ветка с тряской.
- `EquipItem` дважды вызывает `SetParent`/`SetActive`.
- `ContainerSizeHandler.Update` каждый кадр пересчитывает размер, строка продублирована.
- `ContextMenuUI` пересоздаёт кнопки на каждый `Show` — при частых кликах лишние аллокации.
- `EquipeItem.Execute` делает `FindObjectsByType<EquipmentSlot>` на каждое действие.
- `UseAction` — заглушка (`Debug.Log`), реального использования нет.
- `ItemPrefabFactory` использует `sourceModel3D` для 3D и `prefabName` для путей; вручную
  ссылки `modelPrefab2D/3D` больше назначать не нужно.
- `IndexOutOfRangeException` в `Selectable.OnDisable` (uGUI) при входе/выходе Play Mode и
  domain reload — известный баг Unity, не связан с логикой инвентаря; после загрузки консоль чистая.

Из `Player`-системы:

- `PlayerStamina`, `PlayerStaminaBar` и `PlayerProceduralBody` добавлены на `Player` в сцене;
  их настройки сохраняются. Если компонент удалить — соответствующее поведение пропадёт.
- `PlayerProceduralBody` больше не строит тело: модель `Player/VisualBody` лежит в сцене,
  а скрипт только вращает её части. Пропорции модели фиксированы в сцене: если менять
  `CharacterController.height` или позицию `Head`, модель нужно поправить руками.
- Материалы модели — ассеты в `Assets/Materials/Player` на `Standard` (Built-in RP); в URP/HDRP
  их нужно перевести на соответствующий шейдер.
- `MeshRenderer` капсулы `GG` и сферы `Head` выключены; их старые коллайдеры тоже выключены,
  хитбоксы теперь — trigger-коллайдеры частей `VisualBody`.
- Коллайдеры `BodyPartHealth` — trigger: `Physics.Raycast` без `QueryTriggerInteraction.Collide`
  их не увидит. Это важно для будущего урона и осмотра.
- Хп всех зон по умолчанию 100; значения меняются в `PlayerHealth` на `Player`.
- `BodyPartInspectionUI` — только просмотр; урон от оружия, лечение и статусы пока не подключены.
- `PlayerStamina` после hot reload сбрасывается в максимум (страховка), а не сохраняет текущее значение.
- Бар — `OnGUI`-отладка: не участвует в Canvas-порядке, поверх UI, и его нельзя стилизовать как uGUI.
- `bodyAlignSpeed` и `headYawLimit` работают вместе: сначала голова до 60°, затем тело догоняет;
  при `bodyAlignSpeed = 0` доворот отключается, остаётся только правило лимита.

### 8.3 Известные ошибки из `AGENTS.md`

- `Serialization depth limit 10 exceeded at 'ItemStateData.data'` — **исправлено**:
  `containerItems` и `installedMods` в `ItemStateData` помечены `[NonSerialized]`.
- `EventSystem` на `StandaloneInputModule`; при переходе на Input System заменить на
  `InputSystemUIInputModule`, иначе UI перестанет реагировать.
- У модификаций оружия пока нет числовых статов: `WeaponModData.statModifiers` — только задел.

### 8.4 Архитектурные замечания

- Нет namespace — все классы в глобальной области, риск конфликтов имён.
- `InventoryManager` теперь тонкий координатор; хранение вынесено в `InventoryStorage`,
  карточки дока — в `InventoryContainerDock`, размещение предмета — в `ItemGridPlacement`,
  визуал карманов — в `ContainerGridVisuals`. `ItemInstance` сократился до 373 строк.
- `PlayerMotor` (движение/прыжок/присед), `PlayerCamera` (обзор/тряска) и
  `PlayerInventoryToggle` (I/курсор) разделены; стамина, бар и тело — отдельные компоненты.
- `WeaponModdingUI` и подсказка `PlayerPickupController` всё ещё создают UI в рантайме;
  для единообразия их стоит перевести в префабы.
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
3. Подбор по `E`: предмет попадает в свободный слот или контейнер, иначе остаётся в мире.
4. Drag: перенос внутри сетки, между сетками, в слот, из слота.
5. Поворот `R` во время drag: визуал и занятые ячейки совпадают.
6. Стак: перенос на такой же предмет, частичный перенос, деление (`Split`).
7. Выброс: 3D-префаб появляется перед игроком, данные (`currentStack`, `containerItems`) сохранены.
8. Контейнеры: рюкзак и разгрузка видны справа одновременно, перетаскивание между ними работает, содержимое не теряется.
9. Экипировка: подходящая категория, снятие, возврат в свободную ячейку.
10. Контекстное меню: состав кнопок соответствует предмету, закрытие по `Esc` и клику мимо.
11. Генератор префабов: 2D/3D создаются по путям шаблонов, размеры сетки совпадают с `ItemData.size`.
12. Модификации: окно из контекстного меню, 7 слотов, установка/снятие, сохранение при выбрасывании/подборе.
13. Разгрузка: карманы разного размера, запрет перехода между карманами, сохранение содержимого.
14. Контейнеры: подбор в надетые разгрузку/рюкзак/контейнер работает и при закрытом инвентаре.
15. UI: при смене разрешения/пропорций инвентарь не разъезжается (`CanvasScaler` + `InventoryResponsiveLayout`).
16. Бег: Shift ускоряет до 8 м/с, стамина падает; в приседе бег недоступен.
17. Прыжок: при стамине меньше 15 не выполняется; после приземления есть толчок камеры.
18. Стамина: после остановки бега ниже 12% повторный бег недоступен до регена выше порога.
19. Перевес: `carryWeight > maxCarryWeight` — ходьба тратит стамину, рост расхода виден.
20. Бар стамины виден на 720p/1080p/4K, не уезжает за край, показывает `[LOCK]`.
21. Тело: в Play Mode походка меняется при движении вперёд/назад/вбок, в приседе колени согнуты,
    при остановке конечности возвращаются в покой.

---

## 10. Модификации оружия, разгрузка и адаптивный UI

### 10.1 Модификации оружия

- `WeaponModSlotType` — `Scope`, `Muzzle`, `Stock`, `Grip`, `Underbarrel`, `SideLeft`, `SideRight`.
- `WeaponModData : ItemData` — тип слота, список совместимых `id`, задел `statModifiers`.
- Мод — обычный предмет `ItemCategory.WeaponMod` с 2D/3D-префабами и иконкой.
- Установленные моды хранятся в `ItemState.installedMods` как `ItemStateData` (runtime, `[NonSerialized]`).
  Отдельных UI-объектов у установленных модов нет: окно модификации строит иконки из данных.
- `WeaponModdingUI` — окно из контекстного меню «Модифицировать». Создаёт панель в рантайме,
  принимает drag&drop модов, по ПКМ по слоту снимает мод обратно в инвентарь/контейнер.
- `ModifyWeaponAction` добавлен в `ContextMenuUI.allActions`.
- При выбрасывании/подборе оружия `installedMods` копируется через `GetStateData/SetStateData`.
- Заглушки: меню `Tools/Contract/Generate Weapon Mod Placeholders`.
  Ассеты: `Assets/Data/Items/Mods`, префабы: `Assets/Prefabs/Items/Mods`.

### 10.2 Разгрузка и карманы

- `ContainerGridData` — набор карманов: имя, `pixelPosition` (левый верхний угол
  в пикселях от левого верхнего угла сетки) и `size` в ячейках. Категории карманов
  не задаются: в любой карман можно положить любой предмет.
- `ContainerData.grid` включает режим карманов. `InitializeGrid` строит общую сетку,
  а `ContainerGridVisuals` помечает ячейки `isAvailable`, `pocketId`, центрирует
  небольшую сетку и скрывает пустые промежутки.
- Позиция кармана задаётся в пикселях, поэтому промежуток между любыми двумя
  карманами можно сделать произвольным, не меняя размер ячейки 50×50. Карман
  остаётся единым блоком: его ячейки не разрезаются промежутком.
- `pocketGapPixels` — старый общий зазор; используется только для ассетов без
  `usePixelPositions` и мигрируется мастером при сохранении.
- В мастере задаются количество, размеры и координаты `X/Y` каждого кармана;
  `ContainerGridPacker` может разложить карманы автоматически без промежутков.
- `ItemPlacementValidator` запрещает:
  - ставить предмет в недоступные ячейки;
  - занимать ячейки двух разных карманов.
- `ItemCategory.Rig` — новый тип предмета; `RigSlotData` добавлен в `EquipmentSlotsManager`.
- Placeholder-разгрузка: `Assets/Data/Items/Containers/Razgruzka.asset`, меню
  `Tools/Contract/Setup Weapon Mod Scene` создаёт слот, окно модификации и тестовые предметы.

### 10.3 Подбор предметов и хранение

- `PlayerPickupController`: луч из центра камеры, слой 8, 3 м, клавиша `E`.
- Игра начинается с закрытым инвентарём; подбор работает и при закрытом инвентаре.
- `TryPickupWorldItem` на время создания 2D-копии включает инвентарь, чтобы сработали
  `Awake` у сеток и ячеек, затем возвращает исходное состояние.
- `TryStoreItem`: стак > 1 не кладёт в слот, а сразу отправляет в контейнеры.
- Порядок контейнеров: надетые разгрузка → рюкзак → контейнер.
- `TryAddToContainer` сначала доливает существующие стаки, затем ищет свободные ячейки.
- Для закрытых (неактивных) контейнеров родительские `InitializeGrid`/`InventoryGrid`
  ищутся ручным обходом иерархии (`ItemState.FindInParents`, `InitializeGrid.FindInParents`,
  `InventoryGrid.FindInParents`).
- `ItemView` при неактивной иерархии не запускает корутины, а ставит позицию/поворот сразу.
- Если места нет, 3D-объект остаётся в мире, игрок видит сообщение об ошибке.

### 10.4 Адаптивный UI

- `CanvasScaler` на `Player/Canvas`: `ScaleWithScreenSize`, reference `2560×1440`, match height.
- `InventoryFrame` — статичный префаб-каркас: `MainPanel` растянут по экрану с отступами 30 px,
  правый док привязан к 62% ширины панели, шапка/футер растянуты по ширине.
- Панели полупрозрачные, чтобы за инвентарём было видно игру. Общая палитра — светло-серая:
  фон ~0.10, `MainPanel` ~0.28, док ~0.30, слоты ~0.50 (тёмные для контраста с иконками),
  шапка/футер/заголовок дока ~0.45–0.50. Панель контейнера ~0.55, сетка ~0.65.
- `InventoryResponsiveLayout` масштабирует `ManagerSlots` (0.5…1) по ширине инвентаря;
  `ManagerSlots` статично смещён влево (`x = -450`), чтобы освободить место под док.
- Правый док — аккордеон: карточки `РЮКЗАК` и `РАЗГРУЗКА`. В свёрнутом виде —
  заголовок, стрелка и большая иконка предмета; ширина ~320 px. По стрелке карточка
  раскрывается до ~800 px и высоты ~440 px.
- Можно раскрыть обе карточки одновременно; состояние запоминается на время сессии.
- В раскрытой карточке панель `Conteyner` показывается с сеткой **50×15** (ячейка 50 px)
  внутри окна фиксированной высоты; сетка прокручивается и не масштабируется.
- `InventoryContainerDock.Refresh` обновляет карточки, `DisplayDockSection`
  переносит панель в `Host`, скрывает её `Header`/`Frame` и растягивает по ширине хоста.
  При закрытии инвентаря `OnDisable` возвращает панели в предметы.
- Если контейнер не надет, его карточка скрыта. Пункта «Открыть» нет.
- Слоты экипировки рассчитаны на опорную область 1000×1440; позиции задаются в
  `EquipmentSlotData` и расставлены вокруг силуэта.
- Подписи слотов (`01 ШЛЕМ` … `07 КОНТЕЙНЕР`) лежат в `EquipmentSlot.prefab`,
  текст задаётся из `EquipmentSlotData.slotLabel` в `EquipmentSlot.Setup`.

### 10.5 Эффекты

- `EffectDefinition` — ScriptableObject с id, именем, описанием, иконкой, целями,
  видимостью, тегами, длительностью, стаками, модификаторами и периодическими тиками.
- `StatModifier` поддерживает `Add` (плоская прибавка) и `Multiply` (доля: 0.25 = +25%).
- Эффекты создаются и редактируются в мастере на вкладке «Эффект».
- `allowedTargets` ограничивает цели; перелом ноги нельзя повесить на руку.
- `EffectReceiver` хранит эффекты на каждой цели, `CharacterEffectAggregator` складывает
  эффекты персонажа, частей тела и надетых предметов.
- Периодические тики уже работают: кровотечение/ожог/яд наносят урон с интервалом,
  лечение идёт через `healing = true`, `canKill = false` оставляет 1 HP.
- Скрытые эффекты (`InspectTarget`) работают сразу, но не видны в HUD до осмотра.
  Сейчас debug-осмотр — `F1`, он раскрывает все эффекты.
- У `ConsumableData` и `EquippableData` есть списки эффектов; у оружия/предметов
  receiver есть, отдельный оружейный агрегатор ещё впереди.
- Покрытие брони/одежды и урон по типам пока не подключены.
