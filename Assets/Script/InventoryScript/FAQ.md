# ❓ FAQ и Решение Проблем

Частые вопросы и решения распространенных проблем системы инвентаря.

---

## 📋 Содержание
1. [Частые Ошибки](#частые-ошибки)
2. [Проблемы с Drag & Drop](#проблемы-с-drag--drop)
3. [Проблемы с Размещением](#проблемы-с-размещением)
4. [Проблемы с Экипировкой](#проблемы-с-экипировкой)
5. [Проблемы с Визуализацией](#проблемы-с-визуализацией)
6. [Производительность](#производительность)
7. [Расширение Системы](#расширение-системы)

---

## 🚨 Частые Ошибки

### ❌ NullReferenceException в InventoryManager.TryStartDrag()

**Симптомы**:
```
NullReferenceException: Object reference not set to an instance of an object
InventoryManager.TryStartDrag()
```

**Причины**:
1. Нет GraphicRaycaster на Canvas
2. Нет EventSystem в сцене
3. targetCanvas не назначен

**Решение**:
- Добавьте GraphicRaycaster на Canvas
- Создайте EventSystem в сцене
- Назначьте targetCanvas в Inspector

---

### ❌ Клики не работают на предметах

**Причины**:
1. CanvasGroup.blocksRaycasts = false
2. Image.raycastTarget = false
3. Неправильный Layer

**Решение**:
- Проверьте Layer = Items (6)
- Включите Raycast Target на Image
- CanvasGroup.blocksRaycasts = true

---

## 🖱️ Проблемы с Drag & Drop

### ❓ Предмет не следует за курсором

**Решение**:
```csharp
// Используйте правильный метод для Canvas типа
private void UpdateDragPosition()
{
    if (draggedItem != null)
    {
        draggedItem.transform.position = Input.mousePosition;
    }
}
```

---

## 📦 Проблемы с Размещением

### ❓ CanPlaceItem всегда возвращает false

**Проверьте**:
1. Категория разрешена (InventoryGrid.allowedTypes)
2. Размер предмета помещается в сетку
3. Ячейки свободны (не занятые)

---

## 🎽 Проблемы с Экипировкой

### ❓ При экипировке предмет исчезает

**Решение**:
- Назначьте iconImage и iconObject в EquipmentSlot
- Проверьте ItemData.icon не null

---

## 📚 Дополнительные Ресурсы

- [ARCHITECTURE.md](ARCHITECTURE.md) - Архитектура
- [SETUP_EXAMPLES.md](SETUP_EXAMPLES.md) - Примеры
- [COMPONENTS_REFERENCE.md](COMPONENTS_REFERENCE.md) - Справочник
