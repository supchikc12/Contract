using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    // Текущий предмет в руках игрока
    private ItemInstance draggedItem;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    private void Awake()
    {
        // Находим Canvas более современным способом (Unity 2023.2+)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        eventSystem = EventSystem.current;
    }

    private void Update()
    {
        // 1. Начало клика
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ClearDraggedItem();
        }

        // 2. Поворот (Клавиша R)
        if (Input.GetKeyDown(KeyCode.R) && draggedItem != null)
        {
            draggedItem.Rotate();
        }
    }

    private void TryStartDrag()
    {
        PointerEventData data = new PointerEventData(eventSystem) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(data, results);

        foreach (var result in results)
        {
            // Слой 6 — это наши предметы (Items)
            if (result.gameObject.layer == 6)
            {
                ItemInstance item = result.gameObject.GetComponent<ItemInstance>();
                if (item != null)
                {
                    draggedItem = item;
                    break;
                }
            }
        }
    }

    // Метод для очистки ссылки (вызывается, когда предмет отпущен)
    public void ClearDraggedItem()
    {
        draggedItem = null;
    }
}
