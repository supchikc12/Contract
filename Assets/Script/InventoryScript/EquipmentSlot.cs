using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour
{
    [Header("Настройки слота")]
    // Теперь используем общие категории из ItemCategory.cs
    [SerializeField] private ItemCategory allowedCategories;

    [Header("Визуализация")]
    [SerializeField] private Image itemIcon; // Картинка внутри слота, которая покажет предмет

    // Ссылка на данные предмета, который сейчас в слоте
    private ItemData currentItemData;

    // Удобное свойство для проверки пустоты слота
    public bool IsEmpty => currentItemData == null;

    private void Awake()
    {
        // Если предмет не назначен, скрываем иконку при старте
        if (IsEmpty)
        {
            itemIcon.enabled = false;
        }
    }

    // Проверка: подходит ли предмет по категории
    public bool CanEquip(ItemState state)
    {
        if (state == null || state.data == null) return false;

        // Побитовая проверка: совпадает ли категория предмета с разрешенными в этом слоте
        return ((int)allowedCategories & (int)state.data.category) != 0;

    }

    // Метод для "надевания" предмета
    public void EquipItem(ItemInstance itemInstance)
    {
        ItemState state = itemInstance.state;
        if (CanEquip(state))
        {
            currentItemData = state.data;

            // Устанавливаем иконку из данных предмета
            itemIcon.sprite = currentItemData.icon;
            itemIcon.enabled = true;

            // В Таркове предмет в слоте обычно уничтожает свой "физический" объект-иконку в руках 
            // и становится просто картинкой в слоте.
            Destroy(itemInstance.gameObject);

            Debug.Log($"Экипировано: {currentItemData.itemName}");
        }
    }

    // Метод для снятия предмета
    public void UnequipItem()
    {
        // Тут должна быть логика спавна ItemInstance обратно в руки или в инвентарь
        currentItemData = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false;
    }
}
