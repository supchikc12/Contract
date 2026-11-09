using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform targetRect;
    private Canvas canvas;

    private void Awake()
    {
        // Берем RectTransform родителя (это сам контейнер)
        targetRect = transform.parent.GetComponent<RectTransform>();
        if (targetRect == null) targetRect = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetRect != null && canvas != null)
        {
            // Двигаем родительский объект
            targetRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}