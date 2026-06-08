using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// EquipmentSlot отвечает за слоты экипировки
// Хранит данные экипированного предмета и управляет его визуализацией
public class EquipmentSlot : MonoBehaviour
{
    [Header("Настройки слота")]
    [SerializeField] private ItemCategory allowedCategories;

    [Header("Визуализация")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite defaultSprite;
    //[SerializeField] private GameObject iconObject;

    [Header("Текущее состояние")]
    [SerializeField] private ItemStateData currentItemData;
    private bool hasItem = false;

    // Свойство для проверки пустоты слота
    public bool IsEmpty => !hasItem;
    
    private void Awake()
    {
        // Инициализация - скрываем иконку пустого слота
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

        ItemState state = itemInstance.GetItemState();
        if (!CanEquip(state))
        {
            Debug.LogWarning($"Cannot equip item: category mismatch");
            return;
        }

        // Если слот занят, снимаем старый предмет
        if (!IsEmpty)
        {
            UnequipItem();
        }

        // Сохраняем данные предмета
        currentItemData = itemInstance.GetStateData();
        iconImage.sprite = currentItemData.data.icon;
        hasItem = true;

        // Обновляем визуал
        UpdateVisuals();

        // Уничтожаем экземпляр предмета (он теперь в слоте)
        Destroy(itemInstance.gameObject);

        Debug.Log($"Equipped: {currentItemData.data.itemName}");
    }

    // Снятие предмета из слота
    public ItemInstance UnequipItem()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("Slot is already empty");
            return null;
        }

        InventoryManager manager = GetComponentInParent<InventoryManager>();
        if (manager == null)
        {
            Debug.LogError("EquipmentSlot: InventoryManager not found in parents!");
            return null;
        }

        // Создаем новый экземпляр предмета
        Transform parentTransform = manager.transform;
        GameObject newItemObject = Instantiate(currentItemData.data.modelPrefab, parentTransform);

        // Устанавливаем позицию на месте слота
        newItemObject.transform.position = transform.position;

        // Получаем компонент ItemInstance
        ItemInstance itemInstance = newItemObject.GetComponent<ItemInstance>();
        if (itemInstance != null)
        {
            // Восстанавливаем сохраненное состояние
            ItemState state = itemInstance.GetItemState();
            if (state != null)
            {
                state.SetStateData(currentItemData);
            }
        }

        // Очищаем слот
        ClearSlot();

        Debug.Log($"Unequipped: {currentItemData.data.itemName}");

        return itemInstance;
    }

    // Получение данных экипированного предмета
    public ItemStateData GetEquippedItemData()
    {
        return currentItemData;
    }

    // Очистка слота
    private void ClearSlot()
    {
        currentItemData = default;
        hasItem = false;
        UpdateVisuals();
    }

    // Обновление визуализации слота
    private void UpdateVisuals()
    {
        //По дефолту устанавливаем дефолтный спрайт
        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconImage.sprite = defaultSprite;

        if (hasItem && currentItemData.data != null && currentItemData.data.icon != null)
        {
            // Показываем иконку предмета
            iconImage.sprite = currentItemData.data.icon;
            iconRect = currentItemData.data.modelPrefab.GetComponent<RectTransform>();
        }

        // Корректировка поворота для вертикальных иконок
        if (iconRect != null && iconRect.sizeDelta.x > iconRect.sizeDelta.y)
        {
            iconRect = iconImage.GetComponent<RectTransform>();
            iconRect.localRotation = Quaternion.Euler(0, 0, 90);
            iconRect.sizeDelta = new Vector2(iconRect.sizeDelta.y, iconRect.sizeDelta.x);
        }
    }

    // Метод для внешней проверки совместимости категорий
    public bool AcceptsCategory(ItemCategory category)
    {
        return ((int)allowedCategories & (int)category) != 0;
    }
}
