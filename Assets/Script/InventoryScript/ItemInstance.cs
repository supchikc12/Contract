using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Используем современные интерфейсы Unity для перетаскивания
public class ItemInstance : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Данные предмета")]
    public ItemData data;
    [SerializeField] private int currentStack;
    [SerializeField] private Coroutine moveCoroutine;
    [Header("UI Настройки")]
    private Vector3 startPosition;
    private Transform originalParent;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Находим компоненты для Raycast
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }

    // --- Интерфейсы перетаскивания ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        SetOccupied(false);

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        startPosition = transform.position;
        originalParent = transform.parent;

        // Предмет становится прозрачным для луча, чтобы мы видели ячейки ПОД ним
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; // Эффект полупрозрачности при переносе
        transform.SetAsLastSibling(); // Поверх всех остальных окон
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Двигаем предмет за мышкой
        transform.position = eventData.position;
    }

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
                if (slot.CanEquip(data))
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
                if (grid.CanAcceptItem(data.category))
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

    public void Rotate()
    {
        // Поворот на 90 градусов
        transform.Rotate(0, 0, -90f);
    }

    private void PlaceInTarget(Transform target)
    {
        // 1. Оставляем родителя прежним (твоя сетка/рюкзак)
        transform.SetParent(originalParent);

        // 2. Вычисляем, где находится ячейка 'target' относительно нашего 'originalParent'
        // InverseTransformPoint переводит мировую позицию ячейки в локальную позицию внутри сетки
        Vector3 targetLocalPos = originalParent.InverseTransformPoint(target.position);
        targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
        // 3. Запускаем полет в эту вычисленную точку
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(SmoothFly(targetLocalPos));

        // 4. Запоминаем эту позицию как "стартовую" для следующего раза
        startPosition = target.position;

        // 5. Важно: для работы SetOccupied нам всё равно нужно знать, над какой ячейкой мы "висим"
        SetOccupied(true, target);
    }

    private GameObject GetTargetUnderMouse(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        foreach (var res in results)
        {
            // Присваивание родителя обьекта 
            Transform CellParent = res.gameObject.transform.parent;
            
            // 1. Пропускаем сам предмет и ячейку самого предмета
            if (res.gameObject == gameObject || CellParent.gameObject.transform.parent == transform) continue;

            // 2. Проверяем, если это ячейка сетки
            else if (res.gameObject.TryGetComponent<Grid>(out var cell))
            {
                return res.gameObject; // Нашли новую цель
            }

        }
        return null;
    }

    private IEnumerator ShakeAndReturn()
    {
        float elapsed = 0;
        Vector3 currentPos = transform.position;
        while (elapsed < 0.25f)
        {
            float x = Mathf.Sin(elapsed * 50f) * 10f; // Эффект тряски
            transform.position = startPosition;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

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
                cell.fullness = null; // Очищаем ячейку
            }
        }
    }
}
