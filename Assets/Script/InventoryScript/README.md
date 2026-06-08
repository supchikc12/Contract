# 📚 Документация Системы Инвентаря

Добро пожаловать в документацию модульной системы инвентаря для Unity!

---

## 🚀 Быстрый Старт

### Новичок в системе?
1. Прочитайте [ARCHITECTURE.md](ARCHITECTURE.md) - понять структуру
2. Следуйте [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md) - создать первый предмет
3. Держите под рукой [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md) - справочник

### Мигрируете со старой системы?
- [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - пошаговая инструкция

### Есть проблема?
- [FAQ.md](FAQ.md) - частые вопросы и решения

---

## 📖 Документы

### 🏗️ [ARCHITECTURE.md](ARCHITECTURE.md)
**Полное описание архитектуры системы**
- Описание всех файлов и их ответственности
- Состояние объектов до старта
- Иерархия зависимостей
- Паттерны проектирования

**Читать когда**: Нужно понять, как система устроена

---

### 🎨 [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md)
**Пошаговые примеры настройки**
- Настройка Canvas
- Создание контейнера (рюкзака)
- Создание слота экипировки
- Создание предмета
- Настройка категорий

**Читать когда**: Нужно создать новый объект

---

### 📋 [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md)
**Краткий справочник по компонентам**
- Описание всех MonoBehaviour
- Описание вспомогательных классов
- Типичные вызовы методов
- Настройки Inspector

**Читать когда**: Нужно быстро найти информацию о компоненте

---

### 📐 [VISUAL_DIAGRAMS.md](VISUAL_DIAGRAMS.md)
**Визуальные диаграммы**
- Иерархия зависимостей
- Процесс перетаскивания (flow)
- Архитектура классов
- Структура сцены
- Диаграмма размещения предметов

**Читать когда**: Нужно визуально понять процессы

---

### 🔄 [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)
**Гайд по миграции со старой системы**
- Таблица соответствия старых/новых компонентов
- Пошаговая инструкция миграции
- Примеры рефакторинга кода

**Читать когда**: Переходите со старой архитектуры

---

### ❓ [FAQ.md](FAQ.md)
**Частые вопросы и решения проблем**
- Частые ошибки
- Проблемы с drag & drop
- Проблемы с размещением
- Проблемы с экипировкой
- Оптимизация производительности

**Читать когда**: Что-то не работает

---

## 🎯 Основные Концепции

### Разделение Ответственности

```
InventoryManager     → Управление системой
ItemInstance         → Координация предмета
ItemState            → Данные предмета
ItemView             → Визуализация
EquipmentSlot        → Управление экипировкой
ItemPlacementValidator → Валидация размещения
```

### Ключевые Компоненты

**Обязательные на сцене**:
- ✅ Canvas с GraphicRaycaster
- ✅ EventSystem
- ✅ InventoryManager

**Обязательные на предмете**:
- ✅ ItemState (с ItemData)
- ✅ ItemInstance
- ✅ CanvasGroup
- ✅ Image с Raycast Target

**Обязательные на слоте**:
- ✅ EquipmentSlot
- ✅ IconImage и IconObject

---

## 🏷️ Важные Настройки

### Layers
```
Layer 6: Items      ← Все предметы
Layer 7: Equipment  ← Все слоты экипировки
```

### Inspector Ссылки
```
InventoryManager → targetCanvas
EquipmentSlot → iconImage, iconObject
ItemState → data (ItemData)
InitializeGrid → prefabGrid
```

---

## 📦 Структура Файлов

```
Assets/Script/InventoryScript/
├── MonoBehaviour Components:
│   ├── InventoryManager.cs
│   ├── ItemInstance.cs
│   ├── ItemState.cs
│   ├── ItemView.cs
│   ├── EquipmentSlot.cs
│   ├── InitializeGrid.cs
│   ├── Grid.cs (InventoryCell)
│   └── InventoryContainer.cs (InventoryGrid)
│
├── Helpers:
│   └── ItemPlacementValidator.cs
│
├── Data:
│   ├── ItemData.cs (ScriptableObject)
│   └── ItemCategory.cs (Enum)
│
└── Documentation:
    ├── README.md ← Вы здесь
    ├── ARCHITECTURE.md
    ├── SETUP_EXAMPLES.md
    ├── COMPONENTS_REFERENCE.md
    ├── VISUAL_DIAGRAMS.md
    ├── MIGRATION_GUIDE.md
    └── FAQ.md
```

---

## 🔍 Поиск по Документации

### Я хочу...

**...понять архитектуру** → [ARCHITECTURE.md](ARCHITECTURE.md)

**...создать новый предмет** → [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md#пример-4-создание-предмета)

**...создать слот экипировки** → [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md#пример-3-создание-слота-экипировки)

**...узнать методы ItemInstance** → [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md#iteminstance)

**...увидеть flow drag & drop** → [VISUAL_DIAGRAMS.md](VISUAL_DIAGRAMS.md#процесс-перетаскивания)

**...мигрировать со старой системы** → [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)

**...решить проблему** → [FAQ.md](FAQ.md)

---

## ⚡ Быстрые Ссылки

### Checklist'ы
- [Checklist полной настройки](ARCHITECTURE.md#checklist-полной-настройки)
- [Checklist контейнера](SETUP_EXAMPLES.md#результат)
- [Checklist предмета](SETUP_EXAMPLES.md#результат-префаб)

### Примеры
- [Пример: Canvas](SETUP_EXAMPLES.md#пример-1-настройка-canvas)
- [Пример: Контейнер 5x7](SETUP_EXAMPLES.md#пример-2-создание-контейнера)
- [Пример: Слот оружия](SETUP_EXAMPLES.md#пример-3-создание-слота-экипировки)
- [Пример: Предмет AK-47](SETUP_EXAMPLES.md#пример-4-создание-предмета)

### Диаграммы
- [Диаграмма зависимостей](VISUAL_DIAGRAMS.md#иерархия-зависимостей)
- [Flow перетаскивания](VISUAL_DIAGRAMS.md#процесс-перетаскивания)
- [Структура сцены](VISUAL_DIAGRAMS.md#структура-сцены-иерархия)
- [Вычисление позиций](VISUAL_DIAGRAMS.md#диаграмма-размещения-предмета)

---

## 🎓 Обучающий Путь

### Уровень 1: Основы
1. Прочитать [ARCHITECTURE.md](ARCHITECTURE.md) (описание файлов)
2. Изучить [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md) (MonoBehaviour)
3. Посмотреть [VISUAL_DIAGRAMS.md](VISUAL_DIAGRAMS.md) (иерархия)

### Уровень 2: Практика
1. Следовать [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md) (Canvas)
2. Создать контейнер (Backpack)
3. Создать слот (WeaponSlot)
4. Создать предмет (Item_XXX)

### Уровень 3: Глубокое Понимание
1. Изучить [VISUAL_DIAGRAMS.md](VISUAL_DIAGRAMS.md) (flow drag & drop)
2. Прочитать код ItemPlacementValidator
3. Понять state machine ItemInstance

### Уровень 4: Расширение
1. Прочитать [FAQ.md](FAQ.md) (расширение системы)
2. Добавить новый тип предмета
3. Реализовать стакирование
4. Добавить сохранение/загрузку

---

## 📞 Поддержка

### Если что-то не работает:
1. Проверьте [FAQ.md](FAQ.md)
2. Проверьте checklist в [ARCHITECTURE.md](ARCHITECTURE.md)
3. Включите Debug.Log в коде
4. Проверьте Console на ошибки

### Если нужна помощь с настройкой:
- [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md) - пошаговые инструкции

### Если нужна информация о компоненте:
- [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md) - справочник

---

## ✅ Минимальная Настройка (5 минут)

```
1. Canvas + InventoryManager
2. EventSystem
3. CellPrefab
4. 1 ItemData
5. 1 Префаб предмета
6. 1 Контейнер
7. 1 Слот
8. Тест!
```

Подробнее → [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md#быстрый-старт)

---

## 🎨 Визуальная Структура

```
Scene
├── Canvas ✓
│   ├── InventoryManager ✓
│   ├── InventoryPanel
│   │   └── Backpack (контейнер) ✓
│   └── EquipmentPanel
│       └── WeaponSlot ✓
│
├── EventSystem ✓
│
└── [Prefabs] ✓
    ├── CellPrefab
    └── Item_AK47
```

---

Начните с [ARCHITECTURE.md](ARCHITECTURE.md) для понимания системы! 🚀
