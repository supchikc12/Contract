using UnityEngine;

// Ётот скрипт висит на каждой маленькой €чейке в инвентаре (1x1)
// ѕереименован в InventoryCell чтобы избежать конфликта с UnityEngine.Grid
public class InventoryCell : MonoBehaviour
{
    [Header("—осто€ние €чейки")]
    // —сылка на объект предмета (ItemInstance), который сейчас перекрывает эту €чейку
    // ≈сли null Ч €чейка свободна.
    public GameObject fullness;
    public Vector2Int gridSize;

    [Header(" арманы и доступность")]
    public bool isAvailable = true;
    public int pocketId = -1;
    public ItemCategory allowedCategories = ItemCategory.All;

    // —войство дл€ быстрой проверки из других скриптов
    public bool IsOccupied => fullness != null;

    private void Awake()
    {
        // ѕри старте €чейка всегда должна быть пустой
        fullness = null;
    }

    // ћетод дл€ очистки €чейки
    public void Clear()
    {
        fullness = null;
    }
}
