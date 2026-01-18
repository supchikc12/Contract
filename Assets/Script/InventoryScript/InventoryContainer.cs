using UnityEngine;

// Этот скрипт теперь служит простым фильтром для контейнеров (рюкзаков, слотов)
public class InventoryGrid : MonoBehaviour
{
    [Header("Настройки фильтра")]
    // Используем общий тип из твоего нового файла ItemCategory
    // Теперь здесь будут те же галочки, что и в данных предмета
    public ItemCategory allowedTypes;

    [Header("Ссылки")]
    // СОВЕТ: Если этот объект — корень сетки, лучше переименовать для ясности
    public GameObject gridRoot;

    // Метод для быстрой проверки: можно ли положить предмет в эту сетку
    public bool CanAcceptItem(ItemCategory itemCategory)
    {
        // Побитовая проверка: входит ли категория предмета в список разрешенных
        return (allowedTypes & itemCategory) != 0;
    }
}