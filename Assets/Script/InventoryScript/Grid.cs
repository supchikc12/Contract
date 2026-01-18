using UnityEngine;

// Ётот скрипт висит на каждой маленькой €чейке в инвентаре (1x1)
public class Grid : MonoBehaviour
{
    [Header("—осто€ние €чейки")]
    // —сылка на объект предмета (ItemInstance), который сейчас перекрывает эту €чейку
    // ≈сли null Ч €чейка свободна.
    public GameObject fullness;

    // —войство дл€ быстрой проверки из других скриптов
    // “еперь можно писать: if(cell.IsOccupied) вместо проверки на null
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
