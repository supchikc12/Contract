using System.Collections.Generic;
using UnityEngine;

// Хранение и перемещение предметов между слотами и контейнерами.
// Не управляет UI-карточками: панель контейнера спрашивает у InventoryContainerDock.
public class InventoryStorage
{
    private readonly InventoryManager manager;
    private readonly InventoryContainerDock dock;

    public InventoryStorage(InventoryManager manager, InventoryContainerDock dock)
    {
        this.manager = manager;
        this.dock = dock;
    }

    // Подбор предмета из мира по нажатию клавиши.
    public bool TryPickupWorldItem(GameObject worldObj)
    {
        if (worldObj == null) return false;

        ItemState worldState = worldObj.GetComponent<ItemState>();
        if (worldState == null || worldState.data == null || worldState.childrenUI == null) return false;

        // Инвентарь может быть закрыт. Временно включаем его, чтобы Awake успел
        // построить сетки и ячейки, затем возвращаем исходное состояние.
        bool wasActive = manager.gameObject.activeSelf;
        if (!wasActive) manager.gameObject.SetActive(true);

        GameObject itemObj = UnityEngine.Object.Instantiate(worldState.childrenUI, manager.transform);
        ItemInstance item = itemObj.GetComponent<ItemInstance>();
        if (item == null)
        {
            UnityEngine.Object.Destroy(itemObj);
            if (!wasActive) manager.gameObject.SetActive(false);
            return false;
        }

        itemObj.SetActive(true);
        ItemState itemState = item.GetItemState();
        if (itemState == null)
        {
            UnityEngine.Object.Destroy(itemObj);
            if (!wasActive) manager.gameObject.SetActive(false);
            return false;
        }

        itemState.SetStateData(worldState.GetStateData());
        item.Init();

        if (!TryStoreItem(item))
        {
            UnityEngine.Object.Destroy(itemObj);
            if (!wasActive) manager.gameObject.SetActive(false);
            return false;
        }

        UnityEngine.Object.Destroy(worldObj);
        if (wasActive && dock != null) dock.Refresh();
        if (!wasActive) manager.gameObject.SetActive(false);
        return true;
    }

    // Пытается сохранить предмет: свободный слот, затем контейнер игрока.
    public bool TryStoreItem(ItemInstance item)
    {
        if (item == null) return false;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        // Стак в слот не кладем: сначала пробуем контейнер.
        if (itemState.currentStack <= 1)
        {
            EquipmentSlot slot = FindFreeSlotFor(itemState);
            if (slot != null)
            {
                slot.EquipItem(item);
                return true;
            }
        }

        return TryStoreInContainers(item);
    }

    public bool TryAddItemToInventory(ItemInstance item)
    {
        return TryStoreItem(item);
    }

    // Кладёт предмет в первый свободный слот надетых контейнеров.
    public bool TryPlaceInFirstFreeCell(ItemInstance item)
    {
        if (item == null) return false;

        ItemInstance backpack = FindEquippedContainer(ItemCategory.Backpack);
        ItemInstance rig = FindEquippedContainer(ItemCategory.Rig);

        if (TryPlaceInContainer(backpack, item)) return true;
        if (TryPlaceInContainer(rig, item)) return true;
        return false;
    }

    public bool TryPlaceInGridFirstFree(ItemInstance item, InitializeGrid grid, InventoryGrid filter)
    {
        if (item == null || grid == null || filter == null) return false;

        foreach (GameObject cell in grid.listGrid)
        {
            if (cell == null) continue;
            if (item.CanPlaceInGrid(cell, filter))
            {
                item.PlaceInGrid(cell);
                return true;
            }
        }

        return false;
    }

    public ItemInstance FindEquippedContainer(ItemCategory category)
    {
        foreach (EquipmentSlot slot in UnityEngine.Object.FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Include))
        {
            if (slot.IsEmpty) continue;
            if ((slot.allowedCategories & category) == 0) continue;

            ItemInstance equipped = slot.GetEquippedInstance();
            if (equipped != null) return equipped;
        }

        return null;
    }

    // Выбрасывает предмет в мир.
    public void DropItem(ItemInstance item)
    {
        if (item == null) return;

        Transform player = manager.Player;
        if (player == null)
        {
            Debug.LogError("InventoryStorage: корень игрока не найден");
            return;
        }

        const float distance = 3f;

        Vector3 forward = player.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 dropPosition = player.position + forward * distance;
        dropPosition.y += 1.5f;

        // Если выбрасываем надетый контейнер, возвращаем панель из дока,
        // иначе содержимое не получится сохранить.
        if (dock != null) dock.RestoreContainer(item);

        ItemStateData newItemData = item.GetStateData();
        ItemState sourceState = item.GetItemState();
        GameObject childrenUIPrefab = sourceState != null ? sourceState.childrenUI : null;

        List<ItemStateData> uiContents = CaptureContainerContents(item);
        if (uiContents.Count > 0)
        {
            newItemData.containerItems = uiContents;
            if (sourceState != null && sourceState.containerItems != null && sourceState.containerItems.Count > 0)
                newItemData.containerItems.AddRange(sourceState.containerItems);
        }

        item.ReleaseOwnerSlot();
        item.ReleaseCells();
        UnityEngine.Object.Destroy(item.gameObject);

        if (newItemData.data != null && newItemData.data.modelPrefab3D != null)
        {
            GameObject obj = UnityEngine.Object.Instantiate(newItemData.data.modelPrefab3D, dropPosition, Quaternion.identity);
            obj.name = newItemData.data.itemName + "_3D";

            ItemState spawnedState = obj.GetComponent<ItemState>();
            if (spawnedState != null)
            {
                spawnedState.SetStateData(newItemData);

                if (childrenUIPrefab != null)
                    spawnedState.childrenUI = childrenUIPrefab;
                else if (newItemData.data.modelPrefab2D != null)
                    spawnedState.childrenUI = newItemData.data.modelPrefab2D;
            }
        }
        else
        {
            Debug.LogWarning($"У предмета {newItemData.data?.itemName} нет modelPrefab3D!");
        }
    }

    private EquipmentSlot FindFreeSlotFor(ItemState state)
    {
        if (state == null) return null;

        foreach (EquipmentSlot slot in UnityEngine.Object.FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Include))
        {
            if (slot.IsEmpty && slot.CanEquip(state))
                return slot;
        }

        return null;
    }

    private bool TryStoreInContainers(ItemInstance item)
    {
        if (item == null) return false;

        ItemInstance rig = FindEquippedContainer(ItemCategory.Rig);
        if (rig != null && TryAddToContainer(rig, item))
            return true;

        ItemInstance backpack = FindEquippedContainer(ItemCategory.Backpack);
        if (backpack != null && TryAddToContainer(backpack, item))
            return true;

        ItemInstance container = FindEquippedContainer(ItemCategory.Container);
        if (container != null && TryAddToContainer(container, item))
            return true;

        return false;
    }

    // Панель контейнера: либо уже стоит в правом доке, либо лежит внутри предмета.
    private Transform FindContainerPanel(ItemInstance container)
    {
        if (container == null) return null;

        if (dock != null)
        {
            Transform docked = dock.GetContainerPanel(container);
            if (docked != null) return docked;
        }

        return container.transform.Find("Conteyner");
    }

    private bool TryAddToContainer(ItemInstance container, ItemInstance item)
    {
        if (container == null || item == null) return false;

        Transform panel = FindContainerPanel(container);
        if (panel == null) return false;

        InitializeGrid grid = panel.GetComponentInChildren<InitializeGrid>(true);
        if (grid == null) return false;
        if (grid.listGrid.Count == 0) grid.TryInit();

        InventoryGrid filter = panel.GetComponent<InventoryGrid>();
        if (filter == null) filter = panel.GetComponentInParent<InventoryGrid>();
        if (filter == null) return false;

        ItemState containerState = container.GetItemState();
        if (containerState != null && containerState.containerItems != null && containerState.containerItems.Count > 0)
            RestoreContainerContents(container, panel, grid);

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        // Сначала доливаем в существующие стаки.
        foreach (Transform child in grid.transform)
        {
            ItemInstance target = child.GetComponent<ItemInstance>();
            if (target == null || target == item) continue;

            ItemState targetState = target.GetItemState();
            if (targetState == null || targetState.data != itemState.data) continue;
            if (targetState.currentStack >= itemState.data.maxStackSize) continue;

            int space = itemState.data.maxStackSize - targetState.currentStack;
            int move = Mathf.Min(space, itemState.currentStack);

            targetState.currentStack += move;
            target.UpdateStackUI();

            itemState.currentStack -= move;
            if (itemState.currentStack <= 0)
            {
                UnityEngine.Object.Destroy(item.gameObject);
                return true;
            }

            item.UpdateStackUI();
        }

        if (!TryPlaceInGridFirstFree(item, grid, filter)) return false;

        Vector3 center;
        if (item.TryGetOccupiedCellsCenter(out center))
        {
            ItemView view = item.GetComponent<ItemView>();
            if (view != null) view.SnapToPosition(center);
            else item.transform.localPosition = center;
        }

        return true;
    }

    private bool TryPlaceInContainer(ItemInstance container, ItemInstance item)
    {
        if (container == null || item == null) return false;

        Transform panel = FindContainerPanel(container);
        if (panel == null) return false;

        InitializeGrid grid = panel.GetComponentInChildren<InitializeGrid>(true);
        if (grid == null) return false;
        if (grid.listGrid.Count == 0) grid.TryInit();

        InventoryGrid filter = panel.GetComponent<InventoryGrid>();
        if (filter == null) filter = panel.GetComponentInParent<InventoryGrid>();
        if (filter == null) return false;

        return TryPlaceInGridFirstFree(item, grid, filter);
    }

    private List<ItemStateData> CaptureContainerContents(ItemInstance item)
    {
        List<ItemStateData> contents = new List<ItemStateData>();
        if (item == null) return contents;

        Transform containerPanel = item.transform.Find("Conteyner");
        if (containerPanel == null) return contents;

        InitializeGrid containerGrid = containerPanel.GetComponentInChildren<InitializeGrid>(true);
        if (containerGrid == null) return contents;

        foreach (Transform child in containerGrid.transform)
        {
            ItemInstance content = child.GetComponent<ItemInstance>();
            if (content != null) contents.Add(content.GetStateData());
        }

        return contents;
    }

    private void RestoreContainerContents(ItemInstance item, Transform containerPanel, InitializeGrid containerGrid)
    {
        if (item == null || containerPanel == null || containerGrid == null) return;

        ItemState containerState = item.GetItemState();
        if (containerState == null || containerState.containerItems == null || containerState.containerItems.Count == 0) return;

        InventoryGrid filter = containerPanel.GetComponent<InventoryGrid>();
        if (filter == null) filter = containerPanel.GetComponentInParent<InventoryGrid>();

        List<ItemStateData> pending = new List<ItemStateData>(containerState.containerItems);
        containerState.containerItems.Clear();

        foreach (ItemStateData data in pending)
        {
            if (data.data == null || data.data.modelPrefab2D == null)
            {
                containerState.containerItems.Add(data);
                continue;
            }

            GameObject contentObj = UnityEngine.Object.Instantiate(data.data.modelPrefab2D, containerGrid.transform);
            ItemInstance content = contentObj.GetComponent<ItemInstance>();
            if (content == null)
            {
                UnityEngine.Object.Destroy(contentObj);
                containerState.containerItems.Add(data);
                continue;
            }

            ItemState contentState = content.GetItemState();
            if (contentState != null) contentState.SetStateData(data);
            content.Init();

            if (!TryPlaceInGridFirstFree(content, containerGrid, filter))
            {
                UnityEngine.Object.Destroy(contentObj);
                containerState.containerItems.Add(data);
            }
            else
            {
                Vector3 center;
                if (content.TryGetOccupiedCellsCenter(out center))
                {
                    ItemView contentView = content.GetComponent<ItemView>();
                    if (contentView != null) contentView.SnapToPosition(center);
                    else content.transform.localPosition = center;
                }
            }
        }
    }
}
