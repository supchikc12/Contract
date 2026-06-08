using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// InventoryManager менеджер для всей системы перетаскивания
public class InventoryManager : MonoBehaviour
{
    // Константы слоев для читаемости
    private const int ITEM_LAYER = 6;
    private const int EQUIPMENT_LAYER = 7;

    [Header("Ссылки на компоненты")]
    [SerializeField] private Canvas targetCanvas;
    
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    [Header("Состояние перетаскивания")]
    private ItemInstance draggedItem;
    private Vector3 dragStartPosition;
    private Transform dragStartParent;
    private bool isDragging;

    private void Awake()
    {
        // Инициализация компонентов
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas != null)
        {
            raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        }
        else
        {
            Debug.LogError("InventoryManager: Canvas not found!");
        }

        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("InventoryManager: EventSystem not found!");
        }
    }

    private void Update()
    {
        // Обработка ввода
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            TryEndDrag();
        }

        // Обновление позиции при перетаскивании
        if (isDragging && draggedItem != null)
        {
            UpdateDragPosition();
        }

        // Поворот предмета клавишей R
        if (Input.GetKeyDown(KeyCode.R) && isDragging && draggedItem != null)
        {
            draggedItem.RequestRotate();
        }
    }

    // Попытка начать перетаскивание
    private void TryStartDrag()
    {
        if (raycaster == null || eventSystem == null) return;

        PointerEventData pointerData = new PointerEventData(eventSystem) 
        { 
            position = Input.mousePosition 
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (var result in results)
        {

            float scrolControl = Input.mouseScrollDelta.y;

            if (scrolControl > 0.1f)
            {
                var sr = result.gameObject.GetComponentInParent<ScrollRect>();
                if (sr != null)
                {
                    float newPosition = sr.verticalNormalizedPosition + scrolControl * 0.1f;
                    sr.verticalNormalizedPosition = Mathf.Clamp01(newPosition);
                }
            }

            // Проверка слоя предметов
            if (result.gameObject.layer == ITEM_LAYER)
            {
                ItemInstance item = result.gameObject.GetComponent<ItemInstance>();
                if (item != null && item.CanStartDrag())
                {
                    StartDragging(item);
                    return;
                }
            }
            // Проверка слоя экипировки
            else if (result.gameObject.layer == EQUIPMENT_LAYER)
            {
                EquipmentSlot slot = result.gameObject.GetComponentInParent<EquipmentSlot>();
                if (slot != null && !slot.IsEmpty)
                {
                    ItemInstance item = slot.UnequipItem();
                    if (item != null)
                    {
                        StartDragging(item);
                        return;
                    }
                }
            }
        }
    }

    // Начало перетаскивания
    private void StartDragging(ItemInstance item)
    {
        draggedItem = item;
        dragStartPosition = item.transform.localPosition;
        dragStartParent = item.transform.parent;
        isDragging = true;

        // Уведомляем предмет о начале перетаскивания
        draggedItem.OnDragStart();
    }

    // Обновление позиции предмета во время перетаскивания
    private void UpdateDragPosition()
    {
        draggedItem.UpdateDragPosition(Input.mousePosition);
    }

    // Попытка завершить перетаскивание
    private void TryEndDrag()
    {
        if (draggedItem == null)
        {
            isDragging = false;
            return;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem) 
        { 
            position = Input.mousePosition 
        };

        GameObject target = GetTargetUnderMouse(pointerData);
        bool placedSuccessfully = false;

        // Разблокируем ближайший ScrollRect в иерархии родителя (если есть)
        var sr = target.gameObject.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            sr.enabled = true;
        }

        if (target != null)
        {
            // Попытка экипировать в слот
            EquipmentSlot equipmentSlot = target.GetComponent<EquipmentSlot>();
            if (equipmentSlot != null)
            {
                if (equipmentSlot.CanEquip(draggedItem.GetItemState()))
                {
                    equipmentSlot.EquipItem(draggedItem);
                    placedSuccessfully = true;
                }
            }
            // Попытка поместить в инвентарь
            else
            {
                InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
                if (grid != null && draggedItem.CanPlaceInGrid(target, grid))
                {
                    draggedItem.PlaceInGrid(target);
                    placedSuccessfully = true;
                }
            }
        }

        // Если не удалось разместить - возвращаем на место
        if (!placedSuccessfully)
        {
            draggedItem.ReturnToStart(dragStartPosition, dragStartParent);
        }

        // Завершаем перетаскивание
        draggedItem.OnDragEnd();
        ClearDragState();
    }

    // Получение объекта под курсором
    private GameObject GetTargetUnderMouse(PointerEventData pointerData)
    {
        if (raycaster == null) return null;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (var result in results)
        {
            // Пропускаем сам перетаскиваемый предмет и его дочерние объекты
            if (draggedItem != null && IsChildOf(result.gameObject, draggedItem.gameObject) || result.gameObject.layer == ITEM_LAYER)
            {
                continue;
            }

            // Проверяем, является ли объект ячейкой или слотом
            if (result.gameObject.GetComponent<InventoryCell>() != null || 
                result.gameObject.GetComponent<EquipmentSlot>() != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    // Проверка, является ли объект дочерним
    private bool IsChildOf(GameObject obj, GameObject parent)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.gameObject == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    // Очистка состояния перетаскивания
    private void ClearDragState()
    {
        draggedItem = null;
        dragStartPosition = Vector3.zero;
        dragStartParent = null;
        isDragging = false;
    }

    // Публичный метод для проверки состояния перетаскивания
    public bool IsDragging() => isDragging;

    // Получение текущего перетаскиваемого предмета
    public ItemInstance GetDraggedItem() => draggedItem;
}
