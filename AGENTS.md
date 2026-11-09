# Contract — FPS-прототип на Unity 6

Проект разрабатывается двумя людьми. Этот файл читают AI-ассистенты (OpenCode),
чтобы не изучать проект с нуля каждую сессию.

## Технические факты

- Unity **6000.0.22f1** (Unity 6 Preview), Windows, Built-in/URP — уточнять по `ProjectSettings/GraphicsSettings.asset`
- Основная сцена: `Assets/Scenes/MainWorld/SampleScene.unity`
- Язык кода: C#, **без namespace**, один публичный класс на файл
- Комментарии в коде на русском

## Структура

```
Assets/
  Script/
    Player/            PlayerManager (движение, камера, взаимодействие)
    Inventory/
      Core/            Grid, InventoryGrid, InventoryManager, ItemPlacementValidator, InitializeGrid
      Items/           ItemState, ItemData, ItemInstance, ItemView, ItemCategory
      Equipment/       EquipmentSlot, EquipmentSlotData, EquipmentSlotsManager
      UI/              ContainerSizeHandler, ScrollRectNoDrag, UIDragHandler
      ContextMenu/     ContextMenuUI, ItemActions, IItemAction
    Editor/            редакторные утилиты (ItemPrefabGenerator и т.п.)
  Prefabs/ Models/ Materials/ Textures/ Data/
  TextMesh Pro/
```

Иерархия сцены (сверху вниз):

- `Player` — CharacterController, PlayerManager; внутри `Canvas/Inventory` (UI инвентаря) и `GG`
- `GG/Head/Main Camera` — камера от первого лица
- `GG/TargetHand`, `GG/TargetBackPack` — точки крепления оружия и рюкзака
- `MainTerrain`, `NavMesh`, `Directional Light`, `EventSystem`
- Предметы в мире: `KVAZIR_3D`, `AK-47_3D`, `AK-47_3D (1)` — `Rigidbody` + `ItemState`

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

## Правила работы ассистента

1. После правок C# — читать консоль Unity и убеждаться, что нет ошибок компиляции,
   прежде чем использовать новые типы.
2. Не изменять `Packages/com.coplaydev.unity-mcp/` — это вендорный пакет MCP-моста,
   обновляется заменой папки целиком.
3. Сцены (`*.unity`) и префабы плохо мержатся в git. Не редактировать одну сцену
   одновременно с другим разработчиком; для экспериментов — отдельные сцены/префабы.
4. Сначала читать состояние (ресурсы `mcpforunity://editor/state`, `mcpforunity://project/*`),
   потом менять через инструменты, а не «на глаз».
5. Не коммитить сгенерированное: `Library/`, `Temp/`, `Logs/`, `obj/`, `*.csproj`, `*.sln`.

## Известные проблемы

- `Serialization depth limit 10 exceeded at 'ItemStateData.data'` — рекурсия при сериализации
  контейнеров (`ItemState.containerItems` → `ItemStateData.data` → `containerItems` → …).
  Нужно рвать цикл: хранить ссылки на вложенные контейнеры по `uniqueId`, а не объектами,
  либо не сериализовать runtime-связи.
- `EventSystem` использует `StandaloneInputModule` (старый Input Manager). Если проект
  перейдёт на новый Input System — заменить на `InputSystemUIInputModule`, иначе UI не реагирует.
