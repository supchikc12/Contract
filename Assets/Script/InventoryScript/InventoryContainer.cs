using UnityEngine;

// Этот скрипт теперь служит простым фильтром для контейнеров (рюкзаков, слотов)
public class InventoryGrid : MonoBehaviour
{
    [Header("Настройки фильтра")]
    public ItemCategory allowedTypes;

    [Header("Тип контейнера")]
    [Tooltip("Это сетка контейнера (рюкзак, сундук)? Тогда внутрь нельзя класть другие контейнеры.")]
    public bool isContainerGrid = false;

    // Метод для быстрой проверки: можно ли положить предмет в эту сетку
    public bool CanAcceptItem(ItemCategory itemCategory)
    {
        // Побитовая проверка: входит ли категория предмета в список разрешенных
        return (allowedTypes & itemCategory) != 0;
    }
}