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

        // 2. Поворот (Клавиша R)
        // Мы вызываем метод Rotate внутри самого предмета
        if (Input.GetKeyDown(KeyCode.R) && draggedItem != null)
        {
            draggedItem.Rotate();
        }

        // ПРИМЕЧАНИЕ: Мы убрали GetMouseButtonUp и GetMouseButton(0) отсюда,
        // так как теперь предмет сам двигает себя через интерфейс IDragHandler.
        // Это предотвращает "дерганье" иконки.
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
                    // Мы не вызываем BeginDrag вручную, Unity сделает это через интерфейс
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
