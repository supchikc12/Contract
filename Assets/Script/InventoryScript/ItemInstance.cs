using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Используем современные интерфейсы Unity для перетаскивания
public class ItemInstance : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Coroutine rotateCoroutine;
    private bool isVertical = true; // Предполагаем, что изначально предмет стоит вертикально (0 градусов)
    private bool startRotation;
    [SerializeField] private float rotationDuration = 0.2f; // Скорость поворота

    [Header("Данные предмета")]
    public ItemState state; // теперь храним runtime state, откуда берём ItemData через state.data
    [SerializeField] private Coroutine moveCoroutine;

    [Header("UI Настройки")]
    private Vector3 startLocalPosition;
    private Transform originalParent;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    // Инициализация: получаем ссылки на необходимые компоненты (ItemState, Canvas raycaster, EventSystem, CanvasGroup)
    private void Awake()
    {
        state = GetComponent<ItemState>();
        if (state == null)
        {
            Debug.LogError("ItemInstance requires an ItemState component on the same GameObject.");
        }

        canvasGroup = GetComponent<CanvasGroup>();

        // Находим компоненты для Raycast
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }

    // --- Интерфейсы перетаскивания ---

    // Вызывается при начале перетаскивания: сохраняет стартовые параметры, снимает пометку занятости из ячейки и делает предмет "сквозным" для Raycast
    public void OnBeginDrag(PointerEventData eventData)
    {
        startRotation = isVertical;

        SetOccupied(false);

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);

        startLocalPosition = transform.localPosition;
        originalParent = transform.parent;

        // Предмет становится прозрачным для луча, чтобы мы видели ячейки ПОД ним
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; // Эффект полупрозрачности при переносе
        transform.SetAsLastSibling(); // Поверх всех остальных окон
    }

    // Вызывается каждый кадр перетаскивания: двигает предмет за курсором
    public void OnDrag(PointerEventData eventData)
    {
        // Двигаем предмет за мышкой
        transform.position = eventData.position;
    }

    // Вызывается при отпускании предмета: ищет цель под курсором и пытается положить предмет или вернуть назад
    public void OnEndDrag(PointerEventData eventData)
    {

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // Ищем, что находится под курсором в момент отпускания
        GameObject target = GetTargetUnderMouse(eventData);

        if (target != null)
        {
            // ПРОВЕРКА 1: Экипировочный слот
            if (target.TryGetComponent<EquipmentSlot>(out var slot))
            {
                if (slot.CanEquip(state))
                {
                    slot.EquipItem(this);
                    return;
                }
            }

            // ПРОВЕРКА 2: Сетка рюкзака или склада
            InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
            if (grid != null)
            {
                // Используем фильтр из InventoryGrid
                if (state != null && grid.CanAcceptItem(state.data.category) && IsAcceptItem(target))
                {
                    PlaceInTarget(target.transform);
                    return;
                }
            }
        }

        // Если бросили в пустоту или тип не подошел — возвращаем с анимацией
        StartCoroutine(ShakeAndReturn());
    }

    // --- Логика размещения и вращения ---

    // Триггер поворота предмета (плавная анимация). Переключает флаг ориентации isVertical.
    public void Rotate()
    {
        // Если поворот уже идет, прерываем его, чтобы начать новый
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);

        // Определяем цель: если был вертикально (0), крутим в горизонталь (-90) и наоборот
        float targetZ = isVertical ? -90f : 0f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);

        // Запускаем плавную корутину
        rotateCoroutine = StartCoroutine(SmoothRotate(targetRotation));

        // Инвертируем состояние
        isVertical = !isVertical;
    }

    // Размещает визуально предмет над целевой ячейкой: изменяет родителя, запускает анимацию перемещения и помечает ячейку как занятую
    private void PlaceInTarget(Transform target)
    {
        // 1. Оставляем родителя прежним (твоя сетка/рюкзак)
        transform.SetParent(originalParent);

        // 2. Вычисляем, где находится ячейка 'target' относительно нашего 'originalParent'
        // InverseTransformPoint переводит мировую позицию ячейки в локальную позицию внутри сетки
        Vector3 targetLocalPos = originalParent.InverseTransformPoint(target.position);
        if (isVertical)
            targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
        else
            targetLocalPos = new Vector3(targetLocalPos.x, targetLocalPos.y - 25f);
        // 3. Запускаем полет в эту вычисленную точку
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(SmoothFly(targetLocalPos));

        // 4. Запоминаем эту позицию как "стартовую" для следующего раза
        // (startPosition removed as unused)

        // 5. Важно: для работы SetOccupied нам всё равно нужно знать, над какой ячейкой мы "висим"
        SetOccupied(true, target);
    }

    // Возвращает GameObject под курсором, фильтруя лишние элементы (сам предмет и дочерние объекты).
    private GameObject GetTargetUnderMouse(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        foreach (var res in results)
        {
            // Присваивание родителя обьекта 
            Transform CellParent = res.gameObject.transform.parent;
            
            // 1. Пропускаем сам предмет и ячейку самого предмета
            if (res.gameObject == gameObject || CellParent != null && CellParent.gameObject.transform.parent == transform) continue;

            // 2. Проверяем, если это ячейка сетки
            else if (res.gameObject.TryGetComponent<Grid>(out var cell))
            {
                return res.gameObject; // Нашли новую цель
            }

        }
        return null;
    }
    
    // Эффект тряски и возврат предмета на место: проигрывается при неудачном размещении
    private IEnumerator ShakeAndReturn()
    {
        float elapsed = 0;
        while (elapsed < 0.25f)
        {
            float x = Mathf.Sin(elapsed * 50f) * 10f;
            transform.position = new Vector3(transform.position.x + x, transform.position.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (startRotation != isVertical) Rotate();
        StartCoroutine(SmoothFly(startLocalPosition));
    }

    // Плавный переход позиции (корутина) — сглаживает перемещение предмета
    private IEnumerator SmoothFly(Vector3 targetLocalPos)
    {
        float elapsed = 0;
        float duration = 0.15f;
        Vector3 startLocalPos = transform.localPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, percent);
            yield return null;   
        }
        transform.localPosition = targetLocalPos;
    }

    // Плавное вращение (корутина) — сглаживает поворот предмета
    private IEnumerator SmoothRotate(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / rotationDuration;

            // Используем Slerp для плавного вращения кватернионов
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, percent);
            yield return null;
        }

        transform.rotation = targetRotation;
        rotateCoroutine = null;
    }
    // Управление пометкой "занятости" ячейки: пометить/очистить fullness
    private void SetOccupied(bool state, Transform target = null)
    {
        // Если мы помечаем ячейку как занятую
        if (state && target != null)
        {
            // Пытаемся получить скрипт Grid из той ячейки, над которой отпустили мышь
            if (target.TryGetComponent<Grid>(out var cell))
            {
                cell.fullness = gameObject; // Записываем этот предмет в ячейку
            }
        }
        // Если освобождаем ячейку (при начале перетаскивания)
        else if (!state)
        {
            // ищем обьект в котором лежали до этого
            GameObject cellObj = GetTargetUnderMouse(new PointerEventData(eventSystem) { position = transform.position });

            if (cellObj != null && cellObj.TryGetComponent<Grid>(out var cell))
            {
                //// Валидация и получение необходимых компонентов
                //if (!ValidatePlacementInputs(target, out InitializeGrid itemInit, out InitializeGrid inventoryInit, out Grid inventoryCell))
                //    return false;

                
                //List<Grid> itemCells = GetItemCells(itemInit);
                //Grid middleCell = itemCells[itemCells.Count / 2];
                //Vector2Int offset = inventoryCell.gridSize - middleCell.gridSize;
                //List<Vector2Int> desiredPositions = ComputeDesiredPositions(itemCells, offset);
                //List<Grid> targetGrids = GetInventoryGridsForPositions(desiredPositions, inventoryInit);
                cell.fullness = null; // Очищаем ячейку
            }
        }
    }

    // Проверка возможности размещения предмета — теперь только проверяет (без изменения состояния)
    // Метод вызывает приватные вспомогательные функции и возвращает true только если все проверки пройдены
    private bool IsAcceptItem(GameObject target)
    {
        // Валидация и получение необходимых компонентов
        if (!ValidatePlacementInputs(target, out InitializeGrid itemInit, out InitializeGrid inventoryInit, out Grid inventoryCell))
            return false;

        // Получаем ячейки предмета
        List<Grid> itemCells = GetItemCells(itemInit);
        if (itemCells == null || itemCells.Count == 0)
            return false;

        // Вычисляем смещение так, чтобы центральная ячейка предмета совпала с ячейкой инвентаря
        Grid middleCell = itemCells[itemCells.Count / 2];
        Vector2Int offset = inventoryCell.gridSize - middleCell.gridSize;

        // Вычисляем желаемые позиции в инвентаре
        List<Vector2Int> desiredPositions = ComputeDesiredPositions(itemCells, offset);

        // Проверяем границы инвентаря
        Vector2Int inventoryDimensions = inventoryInit.GetDimensions();
        if (!ArePositionsWithinBounds(desiredPositions, inventoryDimensions))
            return false;

        // Получаем соответствующие Grid объекты инвентаря
        List<Grid> targetGrids = GetInventoryGridsForPositions(desiredPositions, inventoryInit);
        if (targetGrids == null || targetGrids.Count != desiredPositions.Count)
            return false;

        // Проверяем, свободны ли целевые ячейки
        if (!AreCellsFree(targetGrids))
            return false;

        // Всё готово — можно разместить (метод только проверяет)
        return true;
    }

    // --- Приватные вспомогательные методы для проверки размещения ---

    // Валидация входных параметров и получение ссылок на InitializeGrid и Grid
    private bool ValidatePlacementInputs(GameObject target, out InitializeGrid itemInit, out InitializeGrid inventoryInit, out Grid inventoryCell)
    {
        itemInit = GetComponentInChildren<InitializeGrid>();
        inventoryInit = target.GetComponentInParent<InitializeGrid>();
        inventoryCell = target.GetComponent<Grid>();

        if (itemInit == null)
        {
            Debug.LogWarning("IsAcceptItem: item InitializeGrid not found.");
            return false;
        }

        if (inventoryInit == null)
        {
            Debug.LogWarning("IsAcceptItem: inventory InitializeGrid not found.");
            return false;
        }

        if (inventoryCell == null)
        {
            Debug.LogWarning("IsAcceptItem: target is not a Grid cell.");
            return false;
        }

        if (state == null || state.data == null)
        {
            Debug.LogWarning("IsAcceptItem: item state or data missing.");
            return false;
        }

        return true;
    }

    // Собирает Grid-компоненты всех внутренних ячеек предмета
    private List<Grid> GetItemCells(InitializeGrid itemInit)
    {
        List<Grid> itemCells = new List<Grid>();
        foreach (var go in itemInit.listGrid)
        {
            Grid g = go.GetComponent<Grid>();
            if (g != null) itemCells.Add(g);
        }
        return itemCells;
    }

    // Вычисляет желаемые координаты в инвентаре для всех ячеек предмета, не меняя исходных координат
    private List<Vector2Int> ComputeDesiredPositions(List<Grid> itemCells, Vector2Int offset)
    {
        List<Vector2Int> positions = new List<Vector2Int>(itemCells.Count);
        foreach (Grid g in itemCells)
        {
            positions.Add(g.gridSize + offset);
        }
        return positions;
    }

    // Проверяет, что все координаты лежат внутри размеров инвентаря
    private bool ArePositionsWithinBounds(List<Vector2Int> positions, Vector2Int dims)
    {
        foreach (var pos in positions)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= dims.x || pos.y >= dims.y)
                return false;
        }
        return true;
    }

    // Получает Grid-объекты инвентаря по списку координат; возвращает null, если какая-то ячейка не найдена
    private List<Grid> GetInventoryGridsForPositions(List<Vector2Int> positions, InitializeGrid inventoryInit)
    {
        List<Grid> grids = new List<Grid>(positions.Count);
        foreach (var pos in positions)
        {
            Grid g = inventoryInit.GetGridAt(pos);
            if (g == null) return null;
            grids.Add(g);
        }
        return grids;
    }

    // Проверяет, свободны ли переданные ячейки (fullness == null)
    private bool AreCellsFree(List<Grid> grids)
    {
        foreach (var g in grids)
        {
            if (g.fullness != null) return false;
        }
        return true;
    }
}
