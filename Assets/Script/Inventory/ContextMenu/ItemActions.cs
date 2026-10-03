using System.ComponentModel;
using UnityEngine;


public class DropAction : IItemAction
{
    public string ActionName => "Выбросить";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;

        return item != null; // любой предмет можно выбросить
    }

    public void Execute(ItemInstance item)
    {
        InventoryManager.Instance.DropItem(item);
    }
}

public class OpenAction : IItemAction
{
    public string ActionName => "Открыть";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        //Только для контейнеров
        return (itemState.data.category & (ItemCategory.Container | ItemCategory.Backpack)) != 0;
    }

    public void Execute(ItemInstance item)
    {
        InventoryManager.Instance.OpenContainer(item);
    }
}

public class UseAction : IItemAction
{
    public string ActionName => "Использовать";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;

        return item != null; // любой предмет можно использовать
    }

    public void Execute(ItemInstance item)
    {
        Debug.Log($"Использовать: {item.name}");
        // Логику выброса добавишь потом
    }
}
