using UnityEngine;

// Адаптивная раскладка инвентаря для разных разрешений и пропорций экрана.
// CollectionZone прижимается к левому верхнему углу и сужается, чтобы
// не перекрывать центральную группу слотов экипировки.
public class InventoryResponsiveLayout : MonoBehaviour
{
    [SerializeField] private RectTransform inventoryRoot;
    [SerializeField] private RectTransform collectionZone;
    [SerializeField] private RectTransform equipmentSlots;

    [Header("Размеры в опорных пикселях (2560x1440)")]
    [SerializeField] private float collectionMaxWidth = 500f;
    [SerializeField] private float collectionMinWidth = 300f;
    [SerializeField] private float collectionHeight = 500f;
    [SerializeField] private float slotsWidth = 1000f;
    [SerializeField] private float gap = 20f;
    [SerializeField] private float margin = 20f;

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
        if (inventoryRoot == null || collectionZone == null) return;

        float canvasWidth = inventoryRoot.rect.width;
        if (canvasWidth <= 1f) return;

        // На CollectionZone висит ContainerSizeHandler, который каждый кадр
        // подгоняет размер под сетку и ломает адаптивную раскладку.
        ContainerSizeHandler legacySizeHandler = collectionZone.GetComponent<ContainerSizeHandler>();
        if (legacySizeHandler != null) legacySizeHandler.enabled = false;

        // Центральная группа слотов имеет фиксированную ширину,
        // поэтому слева остаётся ровно столько места под CollectionZone.
        float slotsLeft = (canvasWidth - slotsWidth) * 0.5f;
        float targetWidth = Mathf.Clamp(slotsLeft - gap, collectionMinWidth, collectionMaxWidth);

        collectionZone.anchorMin = new Vector2(0f, 1f);
        collectionZone.anchorMax = new Vector2(0f, 1f);
        collectionZone.pivot = new Vector2(0f, 1f);
        collectionZone.anchoredPosition = new Vector2(margin, -margin);
        collectionZone.sizeDelta = new Vector2(targetWidth, collectionHeight);

        if (equipmentSlots == null) return;

        // На очень узких окнах дополнительно уменьшаем группу слотов.
        float availableForSlots = canvasWidth - targetWidth - gap * 2f;
        float slotsScale = Mathf.Clamp(availableForSlots / slotsWidth, 0.5f, 1f);
        equipmentSlots.localScale = new Vector3(slotsScale, slotsScale, 1f);
    }
}
