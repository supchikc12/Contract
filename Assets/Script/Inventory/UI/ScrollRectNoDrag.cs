using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectNoDrag : ScrollRect
{
    public override void OnBeginDrag(PointerEventData eventData)
    {
        // Запрещаем начало перетаскивания (ЛКМ)
    }

    public override void OnDrag(PointerEventData eventData)
    {
        // Запрещаем саму прокрутку при перетаскивании
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        // Код здесь выполняется при отпускании кнопки
    }
}
