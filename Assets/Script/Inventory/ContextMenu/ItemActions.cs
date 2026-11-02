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
    }
}

public class EquipeItem : IItemAction
{
    public string ActionName => "Экипировать";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        // Если предмет уже лежит в слоте — нельзя его "экипировать" еще раз
        if (item.transform.parent != null && item.transform.parent.GetComponent<EquipmentSlot>() != null)
            return false;

        return (itemState.data.category & (ItemCategory.Backpack | ItemCategory.Armor | ItemCategory.Weapon | ItemCategory.SecondaryWeapons | ItemCategory.LightWeapon | ItemCategory.Helmet)) != 0;
    }

    public void Execute(ItemInstance item)
    {
        // Ищем все слоты на сцене
        EquipmentSlot[] slots = Object.FindObjectsByType<EquipmentSlot>(FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            if (slot.CanEquip(item.GetItemState()) && slot.IsEmpty)
            {
                slot.EquipItem(item);

                InventoryManager.Instance.HandlePickedUpItem(item);

                return;
            }
        }
        Debug.LogWarning("Нет подходящего свободного слота для этого предмета!");
    }
}

// Новый класс для снятия предмета из слота
public class UnequipAction : IItemAction
{
    public string ActionName => "Снять";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;
        // Доступно только если предмет находится внутри слота
        return item.transform.parent != null && item.transform.parent.GetComponent<EquipmentSlot>() != null;
    }

    public void Execute(ItemInstance item)
    {
        EquipmentSlot slot = item.transform.parent.GetComponent<EquipmentSlot>();
        if (slot != null)
        {
            slot.UnequipItem();
        }
    }
}
