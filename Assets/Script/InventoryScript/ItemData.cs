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
    // КРИТИЧНО: сколько ячеек занимает предмет (Ширина, Высота)
    // Например: ПМ (1,1), АК-74 (4,2)
    public Vector2Int size = new Vector2Int(1, 1);

    [Header("Характеристики")]
    public int maxStackSize = 1;      // Максимальный стак (для патронов/денег)
    public float maxDurability = 100f; // Максимальная прочность

    [Header("Визуализация")]
    public GameObject modelPrefab;    // 3D модель для выбрасывания в мир или осмотра
}
