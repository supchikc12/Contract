using UnityEngine;

// Базовые неизменяемые данные предмета.
// Runtime-состояние хранится отдельно в ItemState.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Идентификация")]
    public string id;
    public string itemName;
    [Tooltip("Имя папки и файлов prefabs. Если пусто — используется itemName.")]
    public string prefabName;
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
    [Tooltip("Исходная модель/префаб для генерации 3D. Генератор берёт из неё меш и материалы.")]
    public GameObject sourceModel3D;

    [Tooltip("Сгенерированный prefab предмета для мира. Заполняется генератором.")]
    public GameObject modelPrefab3D;

    [Tooltip("Сгенерированный 2D-prefab для инвентаря. Заполняется генератором.")]
    public GameObject modelPrefab2D;
}
