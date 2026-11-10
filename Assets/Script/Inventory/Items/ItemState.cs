using System.Collections.Generic;
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
    [Header("Содержимое контейнера")]
    public List<ItemStateData> containerItems = new List<ItemStateData>();
    [Header("Установленные модификации")]
    [System.NonSerialized] public List<ItemStateData> installedMods = new List<ItemStateData>();
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
            uniqueId = this.uniqueId,
            containerItems = this.containerItems != null
            ? new List<ItemStateData>(this.containerItems)
            : new List<ItemStateData>(),
            installedMods = this.installedMods != null
            ? new List<ItemStateData>(this.installedMods)
            : new List<ItemStateData>()
        };
    }

    // Применение сохраненных данных
    public void SetStateData(ItemStateData stateData)
    {
        this.data = stateData.data;
        this.currentStack = stateData.currentStack;
        this.currentDurability = stateData.currentDurability;
        this.uniqueId = stateData.uniqueId;
        this.containerItems = stateData.containerItems != null
            ? new List<ItemStateData>(stateData.containerItems)
            : new List<ItemStateData>();
        this.installedMods = stateData.installedMods != null
            ? new List<ItemStateData>(stateData.installedMods)
            : new List<ItemStateData>();
    }

    public bool IsEmptyContainer()
    {
        return containerItems == null || containerItems.Count == 0;
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
    // Не сериализуем вложенные контейнеры: иначе Unity уходит в рекурсию
    // при сериализации (Serialization depth limit 10 exceeded)
    [System.NonSerialized] public List<ItemStateData> containerItems;
    // Установленные модификации оружия (только runtime)
    [System.NonSerialized] public List<ItemStateData> installedMods;
}
