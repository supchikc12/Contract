using UnityEngine;

// ItemState теперь содержит ТОЛЬКО изменяемые данные предмета (runtime state)
// Вся UI логика перенесена в ItemInstance
public class ItemState : MonoBehaviour
{
    [Header("Неизменяемые данные")]
    public ItemData data;
    public GameObject childrenUI; //Копия этого же обьекта но изменённая под работу с инвентарём
    [Header("Изменяемые характеристики")]
    public int currentStack = 1;
    public float currentDurability = 100f;
    
    [Header("Уникальная информация")]
    public string uniqueId;

    private void Awake()
    {
        // Генерируем уникальный ID при создании, если не задан
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }
    }

    // Метод для проверки валидности состояния
    public bool IsValid()
    {
        return data != null && currentStack > 0 && currentDurability > 0;
    }

    // Создание копии данных (для экипировки/снятия)
    public ItemStateData GetStateData()
    {
        return new ItemStateData
        {
            data = this.data,
            currentStack = this.currentStack,
            currentDurability = this.currentDurability,
            uniqueId = this.uniqueId
        };
    }

    // Применение сохраненных данных
    public void SetStateData(ItemStateData stateData)
    {
        this.data = stateData.data;
        this.currentStack = stateData.currentStack;
        this.currentDurability = stateData.currentDurability;
        this.uniqueId = stateData.uniqueId;
    }
}

// Структура для передачи данных между компонентами (без MonoBehaviour)
[System.Serializable]
public struct ItemStateData
{
    public ItemData data;
    public int currentStack;
    public float currentDurability;
    public string uniqueId;
}
