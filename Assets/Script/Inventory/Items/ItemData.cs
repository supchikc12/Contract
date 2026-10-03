using System.Security.Cryptography.X509Certificates;
using UnityEngine;

// Позволяет создавать ассеты предметов через меню в Project: Right Click -> Create -> Inventory -> Item Data
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Базовые настройки")]
    public string itemName;      // Название предмета
    public Sprite icon;          // Иконка для отображения в инвентаре

    // Используем общую категорию из нашего отдельного файла ItemCategory.cs
    public ItemCategory category;

    [Header("Размеры (в ячейках)")]
    // Cколько ячеек занимает предмет (Высота,Ширина)
    public Vector2Int size;

    [Header("Контейнер")]
    public Vector2Int containerSize;

    [Header("Характеристики")]
    public int maxStackSize = 1;      // Максимальный стак (для патронов/денег)
    public float maxDurability = 100f; // Максимальная прочность

    [Header("Визуализация")]
    public GameObject modelPrefab3D;    // 3D модель для выбрасывания в мир или осмотра
    public GameObject modelPrefab2D;    // 2D модель для интвенторя
}
