using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// EquipmentSlot отвечает за слоты экипировки
// Хранит данные экипированного предмета и управляет его визуализацией
public class EquipmentSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Настройки слота")]
    [SerializeField] public ItemCategory allowedCategories;

    [Header("Визуализация")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite defaultSprite;
    //[SerializeField] private GameObject iconObject;

    [Header("Текущее состояние")]
    [SerializeField] private ItemStateData currentItemData;
    [SerializeField] private ItemInstance equippedInstance;
    private bool hasItem = false;

    // Свойство для проверки пустоты слота
    public bool IsEmpty => !hasItem;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (equippedInstance == null) return;

        // "Будим" предмет, чтобы он мог двигаться
        equippedInstance.gameObject.SetActive(true);

        // Временно прячем иконку слота (чтобы не было дубля)
        iconImage.sprite = defaultSprite;

        // Передаем управление предмету (он сохранит старые координаты и переедет на Canvas)
        equippedInstance.HandleBeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (equippedInstance != null)
        {
            equippedInstance.HandleDrag();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (equippedInstance == null) return;

        ItemInstance item = equippedInstance;
        GameObject target = eventData.pointerCurrentRaycast.gameObject;

        // Передаем эстафету в InventoryManager (он решит: положить в сетку, выбросить или вернуть)
        item.HandleEndDrag(target);

        // Запускаем проверку, куда в итоге делся предмет (нужно подождать тряску)
        StartCoroutine(CheckSlotAfterDrag(item));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Если нажали ПКМ и в слоте есть предмет
        if (eventData.button == PointerEventData.InputButton.Right && equippedInstance != null)
        {
            // Открываем контекстное меню для этого предмета
            InventoryManager.Instance.HandleRightClick(equippedInstance);
        }
    }

    private System.Collections.IEnumerator CheckSlotAfterDrag(ItemInstance item)
    {
        // Ждем 0.3 секунды (время тряски в ItemView.PlayShakeEffect)
        yield return new WaitForSeconds(0.3f);

        // Если предмет уничтожен (выбросили) или улетел в сетку/контейнер
        if (item == null || item.transform.parent != transform)
        {
            ClearSlot(); // Очищаем слот
        }
        else
        {
            // Предмет вернулся в слот
            item.gameObject.SetActive(false); // Прячем его обратно
            hasItem = true;
            iconImage.sprite = item.GetItemState().data.icon; // Возвращаем иконку
        }
    }

    private void Awake()
    {
        // Инициализация - скрываем иконку пустого слота
        UpdateVisuals();
    }

    public void Setup(EquipmentSlotData data)
    {
        allowedCategories = data.allowedCategories;
        defaultSprite = data.defaultSprite;

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition = data.anchoredPosition;
        rt.sizeDelta = data.size;

        UpdateVisuals();
    }

    // Проверка: подходит ли предмет по категории
    public bool CanEquip(ItemState state)
    {
        if (state == null || state.data == null) return false;

        // Побитовая проверка категории
        return ((int)allowedCategories & (int)state.data.category) != 0;
    }

    // Экипировка предмета в слот
    public void EquipItem(ItemInstance itemInstance)
    {
        if (itemInstance == null) return;

        if (!CanEquip(itemInstance.GetItemState())) { Debug.LogWarning($"Не возможно разместить предмет"); return; }

        if (!IsEmpty) { UnequipItem(); }

        RectTransform rt = itemInstance.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        itemInstance.transform.SetParent(transform, false);
        itemInstance.gameObject.SetActive(false);

        itemInstance.ReleaseCells();
        itemInstance.transform.SetParent(transform, false);
        itemInstance.gameObject.SetActive(false);

        equippedInstance = itemInstance;
        currentItemData = itemInstance.GetStateData();
        iconImage.sprite = currentItemData.data.icon;

        hasItem = true;

        UpdateVisuals();

        Debug.Log($"Equipped: {currentItemData.data.itemName}");
    }

    // Снятие предмета из слота
    public ItemInstance UnequipItem()
    {
        if (IsEmpty || equippedInstance == null) { Debug.LogWarning("Slot is already empty"); return null; }

        ItemInstance item = equippedInstance;
        item.gameObject.SetActive(true);

        ClearSlot();
        InventoryManager manager = InventoryManager.Instance;
        if (manager == null) { Debug.LogError("InventoryManager не найден"); return item; }
        if (!manager.TryPlaceInFirstFreeCell(item)) manager.DropItem(item);
        return item;
    }

    // Получение данных экипированного предмета
    public ItemStateData GetEquippedItemData()
    {
        return equippedInstance != null ? equippedInstance.GetStateData() : default;
    }

    // Очистка слота
    private void ClearSlot()
    {
        currentItemData = default;
        hasItem = false;
        equippedInstance = null;
        UpdateVisuals();
    }

    // Обновление визуализации слота
    private void UpdateVisuals()
    {
        if (iconImage == null) return;

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        RectTransform slotRect = GetComponent<RectTransform>();

        // 1. Привязываем иконку строго к центру слота
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.localRotation = Quaternion.identity;

        // По умолчанию иконка пустого слота занимает весь слот
        iconRect.sizeDelta = slotRect.rect.size;
        iconImage.sprite = defaultSprite;
        iconImage.preserveAspect = true;

        // 2. Если предмет есть
        if (hasItem && currentItemData.data != null && currentItemData.data.icon != null)
        {
            iconImage.sprite = currentItemData.data.icon;

            Sprite spr = currentItemData.data.icon;

            // Считаем слот "явно вертикальным", только если он в 1.2 раза выше, чем шире
            // (Слот оружия 150x328 — явно вертикальный. Слот рюкзака 220x250 — почти квадрат).
            bool isSlotClearlyVertical = slotRect.rect.height > slotRect.rect.width * 1.2f;
            bool isSlotClearlyHorizontal = slotRect.rect.width > slotRect.rect.height * 1.2f;

            // Картинка горизонтальная, если её ширина больше высоты
            bool isSpriteVertical = spr.rect.height > spr.rect.width;

            // Крутим только если ориентации ЯВНО не совпадают
            if ((isSlotClearlyVertical && !isSpriteVertical) || (isSlotClearlyHorizontal && isSpriteVertical))
            {
                iconRect.localRotation = Quaternion.Euler(0, 0, 90);
                // Меняем местами ширину и высоту, чтобы preserveAspect не сплющил картинку
                iconRect.sizeDelta = new Vector2(slotRect.rect.height, slotRect.rect.width);
            }
        }
    }

    // Метод для внешней проверки совместимости категорий
    public bool AcceptsCategory(ItemCategory category)
    {
        return ((int)allowedCategories & (int)category) != 0;
    }
}
