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
        return (itemState.data.category & (ItemCategory.Container | ItemCategory.Backpack | ItemCategory.Rig)) != 0;
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

        return (itemState.data.category & (ItemCategory.Backpack | ItemCategory.Armor | ItemCategory.Weapon | ItemCategory.SecondaryWeapons | ItemCategory.LightWeapon | ItemCategory.Helmet | ItemCategory.Rig)) != 0;
    }

    public void Execute(ItemInstance item)
    {
        // Ищем все слоты на сцене
        EquipmentSlot[] slots = Object.FindObjectsByType<EquipmentSlot>();
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

// Открывает окно модификации оружия
public class ModifyWeaponAction : IItemAction
{
    public string ActionName => "Модифицировать";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        return (itemState.data.category & ItemCategory.Weapon) != 0;
    }

    public void Execute(ItemInstance item)
    {
        if (WeaponModdingUI.Instance == null)
        {
            Debug.LogWarning("WeaponModdingUI не найден в сцене!");
            return;
        }

        WeaponModdingUI.Instance.Open(item);
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

public class SplitAction : IItemAction
{
    public string ActionName => "Разделить";

    public bool CanExecute(ItemInstance item)
    {
        if (item == null) return false;
        ItemState state = item.GetItemState();

        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInCollectionZone(item)) return false;

        // Разделять можно только если предметов больше 1 и предмет не надет в слот
        return state != null && state.currentStack > 1 && item.transform.parent.GetComponent<EquipmentSlot>() == null;
    }

    public void Execute(ItemInstance item)
    {
        ItemState state = item.GetItemState();

        // Вычисляем половину (округляем вниз, чтобы не потерять патроны)
        int halfAmount = state.currentStack / 2;

        // Отнимаем у оригинала
        state.currentStack -= halfAmount;
        item.UpdateStackUI();

        // КЛОНИРУЕМ сам объект (вместе со всей его сеткой и UI)
        GameObject cloneObj = Object.Instantiate(item.gameObject, item.transform.parent);
        ItemInstance cloneItem = cloneObj.GetComponent<ItemInstance>();

        // Настраиваем клон
        ItemState cloneState = cloneItem.GetItemState();
        cloneState.currentStack = halfAmount;
        cloneState.uniqueId = System.Guid.NewGuid().ToString(); // Даем новый ID
        cloneItem.WorldSourceObjects.Clear();

        cloneItem.Init();

        // Пытаемся положить клон в первую свободную ячейку этой же сетки
        InitializeGrid grid = item.GetComponentInParent<InitializeGrid>();
        InventoryGrid filter = item.GetComponentInParent<InventoryGrid>();

        if (grid != null && filter != null)
        {
            bool placed = InventoryManager.Instance.TryPlaceInGridFirstFree(cloneItem, grid, filter);
            if (!placed)
            {
                // Если места нет — выкидываем на землю
                InventoryManager.Instance.DropItem(cloneItem);
            }
        }
    }
}
