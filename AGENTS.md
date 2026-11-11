# Contract — FPS-прототип на Unity 6

Проект разрабатывается двумя людьми. Этот файл читают AI-ассистенты (OpenCode),
чтобы не изучать проект с нуля каждую сессию.

## Технические факты

- Unity **6000.6.8f1** (Unity 6), Windows, Built-in Render Pipeline — уточнять по `ProjectSettings/GraphicsSettings.asset`
- Основная сцена: `Assets/Scenes/MainWorld/SampleScene.unity`
- Язык кода: C#, **без namespace**, один публичный класс на файл
- Комментарии в коде на русском
- Смысл и зона ответственности каждого скрипта — `docs/CODE.md`, раздел **1.1**

## Структура

```
Assets/
  Script/
    Player/            PlayerMotor (движение), PlayerCamera (обзор/тряска),
                       PlayerInventoryToggle (I, курсор), PlayerPickupController (подбор по E),
                       PlayerStamina, PlayerStaminaBar, PlayerProceduralBody
    Inventory/
      Core/            InventoryCell, InventoryGrid, InventoryManager, InventoryStorage,
                       ItemPlacementValidator, InitializeGrid
      Items/           ItemState, ItemData, ItemInstance, ItemView, ItemCategory,
                       ItemGridPlacement
      Equipment/       EquipmentSlot, EquipmentSlotData, EquipmentSlotsManager
      UI/              ContainerSizeHandler, ScrollRectNoDrag, UIDragHandler,
                       InventoryResponsiveLayout, InventoryContainerDock
      ContextMenu/     ContextMenuUI, ItemActions, IItemAction
      Modifications/   WeaponModSlotType, WeaponModData, WeaponModdingUI, WeaponModSlotUI
      Vest/            VestLayoutData (карманы разгрузки), VestGridVisuals (отрисовка карманов)
    Editor/            ItemPrefabGenerator, WeaponModPlaceholderGenerator, WeaponModSceneSetup
  Scenes/MainWorld/    SampleScene.unity + NavMesh
  Prefabs/
    Inventory/         InventoryFrame, EquipmentSlot, InventoryCell, Button
    Items/
      Templates/       ItemTemplate, Item3DTemplate, ContainerTemplate
      Weapons/         AK-47/
      Containers/      KVAZIR/, Razgruzka/
      Mods/            заглушки модов
  Data/
    Items/             Weapons/ (AK-47), Containers/ (Backpack, Razgruzka), Mods/
    Equipment/Slots/   WeaponSlotData, RigSlotData и остальные слоты
    Vests/             RazgruzkaLayout.asset
  Materials/Placeholders/ материалы заглушек
  Textures/
    UI/Inventory/      фон, сетка, иконки экипировки
    Items/             иконки контейнеров и оружия
    Placeholders/      иконки заглушек (моды, разгрузка)
  TextMesh Pro/        ресурсы TMP
```

Иерархия сцены (сверху вниз):

- `Player` — CharacterController, PlayerMotor, PlayerCamera, PlayerInventoryToggle,
  PlayerPickupController, PlayerStamina, PlayerStaminaBar, PlayerProceduralBody; внутри
  `Canvas/Inventory` (UI инвентаря) и `GG`
- `GG/Head/Main Camera` — камера от первого лица
- `GG/TargetHand`, `GG/TargetBackPack` — точки крепления оружия и рюкзака
- `MainTerrain`, `NavMesh`, `Directional Light`, `EventSystem`
- `TEST_Content` — тестовые мировые предметы `KVAZIR_3D`, `AK-47_3D`, `AK-47_3D (1)`:
  `Rigidbody` + `ItemState`

## Слои (Layer)

| Индекс | Имя |
|---|---|
| 3 | UI Inventory |
| 5 | UI |
| 6 | UI Item |
| 7 | UI Slot |
| 8 | Item |

## Кодировка — важно

Файлы `Assets/Script/**/*.cs` сохранены в **Windows-1251** без BOM, а не в UTF-8.
Если перезаписать файл целиком в UTF-8, все русские комментарии превратятся в «кракозябры».

Правило: править такие файлы точечно (replace, не полная перезапись) и сохранять исходную
кодировку, либо сначала отдельной задачей перевести весь `Assets/Script` в UTF-8 и проверить,
что проект компилируется.

## Язык

- Рассуждать (внутренние рассуждения) и отвечать пользователю — **на русском**.
- Код, имена переменных/классов, пути и сообщения коммитов — на английском.
- Комментарии в C# — на русском (как уже принято в проекте).

## UI и разрешения

- CanvasScaler инвентаря: **Scale With Screen Size**, reference resolution **2560×1440**,
  **Match Width Or Height = 1 (height)**. Не переключать на `Constant Pixel Size` —
  иначе инвентарь по-разному выглядит на разных мониторах.
- `InventoryResponsiveLayout` на `Player/Canvas/Inventory` масштабирует группу
  `ManagerSlots` на узких пропорциях.
- Каркас инвентаря — префаб `Assets/Prefabs/Inventory/InventoryFrame.prefab`, вставленный
  в сцену под `Player/Canvas/Inventory`: фон, шапка, футер, силуэт, правый док и сетка.
  Карточками дока управляет `InventoryContainerDock` на этом же префабе.
- Карточки контейнеров управляются `InventoryContainerDock` на `InventoryFrame`,
  хранение и выбор контейнера — `InventoryStorage`.
- Правый док показывает надетые контейнеры компактными карточками: свёрнутая — название,
  стрелка и большая иконка; по стрелке карточка расширяется до ~800 px и показывает сетку.
  Рюкзак и разгрузку можно раскрыть одновременно. Пункта «Открыть» больше нет.
- Панели инвентаря полупрозрачные и светло-серые: за интерфейсом видно игровой мир,
  но шапка, подписи и сетка остаются читаемыми.
- Автоматического списка предметов рядом (режим «как в PUBG») больше нет:
  предметы подбираются вручную через `PlayerPickupController`.
- Опорный размер ячейки — 50×50 px. Все размеры предметов задаются в ячейках.
- У иконки `Image` в 2D-префабе предмета должен быть включён **Raycast Target**:
  иначе предмет не начинает drag, а клик уходит в ячейку или `ScrollRect` контейнера.

## Подбор предметов

- `PlayerPickupController` висит на `Player`, смотрит в центр камеры (raycast, слой 8,
  радиус 3 м) и по клавише **E** вызывает `InventoryManager.TryPickupWorldItem`.
- Инвентарь на старте закрыт (сцена могла быть сохранена открытой — `PlayerInventoryToggle.Start`
  закрывает его). Подбор работает и при закрытом инвентаре.
- При подборе предмет сначала пытается попасть в **свободный слот экипировки**
  (оружие, шлем, броня, рюкзак, разгрузка, лёгкое оружие, контейнер).
- Если слот занят/не подходит или предмет стакается (стак > 1) — предмет идёт в
  **разгрузку**, **рюкзак** или **контейнер**.
- Если свободного слота и контейнера нет, подбор не происходит: предмет остаётся в мире,
  игрок видит сообщение «Нет свободного слота или контейнера».
- Общего «главного» инвентаря-сетки больше нет: хранение только в слотах и контейнерах.

## Правила работы ассистента

1. **Главное правило:** перед любыми изменениями проекта сначала изучить проект целиком —
   структуру, сцены, префабы, существующие скрипты и их зоны ответственности — и только
   после этого решать, нужен ли новый элемент. Не создавать новый скрипт или компонент,
   если задачу можно решить расширением существующего. Не плодить похожие скрипты:
   у каждого класса должна быть своя зона ответственности, следуем ООП и SRP.
2. После правок C# — читать консоль Unity и убеждаться, что нет ошибок компиляции,
   прежде чем использовать новые типы.
3. Не изменять `Packages/com.coplaydev.unity-mcp/` — это вендорный пакет MCP-моста,
   обновляется заменой папки целиком.
4. Сцены (`*.unity`) и префабы плохо мержатся в git. Не редактировать одну сцену
   одновременно с другим разработчиком; для экспериментов — отдельные сцены/префабы.
5. Сначала читать состояние (ресурсы `mcpforunity://editor/state`, `mcpforunity://project/*`),
   потом менять через инструменты, а не «на глаз».
6. Не коммитить сгенерированное: `Library/`, `Temp/`, `Logs/`, `obj/`, `*.csproj`, `*.sln`.
7. При важных изменениях архитектуры, данных, UI или сцены обновлять `AGENTS.md` и
   `docs/CODE.md` — документация должна соответствовать фактическому коду.
8. Не коммитить и не пушить каждое изменение. Коммит — только по явной просьбе пользователя
   или для крупных, полностью проверенных изменений без ошибок, багов и незавершённых мест.
   Мелкие правки и промежуточные состояния оставлять в рабочем дереве.

## Известные проблемы

- `Serialization depth limit 10 exceeded at 'ItemStateData.data'` — **исправлено**:
  `containerItems` и `installedMods` в `ItemStateData` помечены `[NonSerialized]`;
  это runtime-данные, а не сохраняемое состояние.
- Модификации оружия пока без числовых статов: `WeaponModData.statModifiers` — задел,
  расчёт урона/отдачи/точности/эргономики не реализован.
- Предметы-заглушки модов и разгрузки лежат в сцене в `WeaponMods_Placeholders`;
  после появления настоящих моделей их нужно заменить.
- `EventSystem` использует `StandaloneInputModule` (старый Input Manager). Если проект
  перейдёт на новый Input System — заменить на `InputSystemUIInputModule`, иначе UI не реагирует.
