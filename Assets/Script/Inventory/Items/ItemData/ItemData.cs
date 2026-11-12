using UnityEngine;

// Базовые неизменяемые данные предмета.
// Runtime-состояние хранится отдельно в ItemState.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Идентификация")]
    public string id;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Категория и размеры")]
    public ItemCategory category;
    [Tooltip("Размер предмета в ячейках: X — строки, Y — столбцы")]
    public Vector2Int size;

    [Header("Характеристики")]
    public float weight;
    public int maxStackSize = 1;
    public float maxDurability = 100f;

    [Header("Визуализация")]
    public GameObject modelPrefab3D;
    public GameObject modelPrefab2D;
}
