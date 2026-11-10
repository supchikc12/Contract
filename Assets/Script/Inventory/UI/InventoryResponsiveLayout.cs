using UnityEngine;

// Адаптивность группы слотов экипировки: на узких экранах уменьшаем масштаб.
public class InventoryResponsiveLayout : MonoBehaviour
{
    [SerializeField] private RectTransform inventoryRoot;
    [SerializeField] private RectTransform equipmentSlots;
    [SerializeField] private float slotsWidth = 1000f;
    [SerializeField] private float gap = 40f;

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Start()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void Apply()
    {
        if (inventoryRoot == null) inventoryRoot = transform as RectTransform;
        if (inventoryRoot == null || equipmentSlots == null) return;

        float canvasWidth = inventoryRoot.rect.width;
        if (canvasWidth <= 1f) return;

        float available = canvasWidth - gap * 2f;
        float scale = Mathf.Clamp(available / slotsWidth, 0.5f, 1f);
        equipmentSlots.localScale = new Vector3(scale, scale, 1f);
    }
}
