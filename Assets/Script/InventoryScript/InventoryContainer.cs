using UnityEngine;

// Этот скрипт теперь служит простым фильтром для контейнеров (рюкзаков, слотов)
public class InventoryGrid : MonoBehaviour
{
    [Header("Настройки фильтра")]
    public ItemCategory allowedTypes;

    // Метод для быстрой проверки: можно ли положить предмет в эту сетку
    public bool CanAcceptItem(ItemCategory itemCategory)
    {
        // Побитовая проверка: входит ли категория предмета в список разрешенных
        return (allowedTypes & itemCategory) != 0;
    }
}