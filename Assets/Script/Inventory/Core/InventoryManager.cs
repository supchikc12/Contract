using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// InventoryManager менеджер для всей системы перетаскивания
public class InventoryManager : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private PlayerManager player;
    [SerializeField] private ContextMenuUI contextMenu;

    [Header("Контейнеры")]
    [SerializeField] private Transform containerParent; // Куда перемещать открытый контейнер (Canvas)
    [SerializeField] public ItemInstance openContainer;  // Текущий открытый контейнер
    [SerializeField] private InitializeGrid openContainerGrid;
    [Header("Состояние перетаскивания")]
    private ItemInstance draggedItem;
    private bool isDragging;

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        ScrollRect scrollRect = gameObject.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) scrollRect.enabled = true;

        if (targetCanvas == null)
            targetCanvas = FindAnyObjectByType<Canvas>();

        if (player == null)
            player = gameObject.GetComponentInParent<PlayerManager>();

        if (contextMenu == null)
            contextMenu = targetCanvas.GetComponentInChildren<ContextMenuUI>();
    }

    private void Update()
    {
        // Если мы тащим предмет и нажали R
        if (isDragging && draggedItem != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                draggedItem.RequestRotate();
            }
        }

    }

    public void HandleLeftClick(ItemInstance item)
    {
        // ЛКМ без drag — ничего не делаем.
        // Перенос работает через OnBeginDrag / OnDrag / OnEndDrag.
    }

    public void HandleRightClick(ItemInstance item)
    {
        contextMenu.Show(item);
    }

    public void HandleBeginDrag(ItemInstance item)
    {
        draggedItem = item;
        isDragging = true;
    }

    public void HandleDrag(ItemInstance item)
    {
        item.UpdateDragPosition(Input.mousePosition);
    }

    public void HandleEndDrag(ItemInstance item, GameObject target)
    {
        bool placed = target != null && TryPlaceInGrid(target);

        if (placed) item.OnDragEnd();
        else if (target == null) DropItem(item);
        else item.ReturnToStart();
        
        draggedItem = null;
        isDragging= false;
    }

    public void HandlePickedUpItem(ItemInstance item)
    {
        if (item != null && item.WorldSourceObjects != null && item.WorldSourceObjects.Count > 0)
        {
            foreach (GameObject worldObj in item.WorldSourceObjects)
            {
                if (worldObj != null)
                {
                    Destroy(worldObj);
                }
            }
            item.WorldSourceObjects.Clear();
        }
    }
    private bool TryPlaceInGrid(GameObject target)
    {
        if (draggedItem == null) return false;

        // 1. Попытка экипировать в слот
        EquipmentSlot equipmentSlot = target.GetComponentInParent<EquipmentSlot>();
        if (equipmentSlot != null)
        {
            if (equipmentSlot.CanEquip(draggedItem.GetItemState()))
            {
                EquipFromDrag(equipmentSlot, draggedItem);
                return true;
            }
        }

        // 2. ПРОВЕРКА НА СТАКИ (если мы кинули предмет прямо на другой предмет)
        ItemInstance targetItem = target.GetComponentInParent<ItemInstance>();

        if (targetItem != null && targetItem != draggedItem)
        {
            ItemState targetState = targetItem.GetItemState();
            ItemState sourceState = draggedItem.GetItemState();

            // Если предметы одинаковые и могут стакаться
            if (targetState.data == sourceState.data && targetState.data.maxStackSize > 1)
            {
                int spaceLeft = targetState.data.maxStackSize - targetState.currentStack;

                if (spaceLeft > 0)
                {
                    int amountToMove = Mathf.Min(sourceState.currentStack, spaceLeft);

                    targetState.currentStack += amountToMove;
                    sourceState.currentStack -= amountToMove;

                    // Обновляем визуал у целевого предмета (на который кинули)
                    targetItem.UpdateStackUI();

                    // Если у перетаскиваемого не осталось ничего — уничтожаем его
                    if (sourceState.currentStack <= 0)
                    {
                        ClearOwnerSlot(draggedItem);
                        Destroy(draggedItem.gameObject);
                        return true;
                    }
                    else
                    {
                        // Если остались излишки, обновляем его UI и идем искать свободную ячейку
                        draggedItem.UpdateStackUI();
                    }
                }
            }
        }

        // 3. Попытка поместить в инвентарь (если стак не удался или остались излишки)
        InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
        if (grid != null && draggedItem.CanPlaceInGrid(target, grid))
        {
            draggedItem.PlaceInGrid(target);

            HandlePickedUpItem(draggedItem);
            return true;
        }

        return false;
    }

    // Экипирует перетаскиваемый предмет. Если это стак, EquipItem отделит
    // одну единицу, а остаток нужно вернуть на прежнее место в инвентаре.
    private void EquipFromDrag(EquipmentSlot slot, ItemInstance item)
    {
        slot.EquipItem(item);

        if (item != null && item.transform.parent != slot.transform)
        {
            item.ReturnToStart();
        }

        HandlePickedUpItem(item);
    }

    // Освобождает слот, если предмет был экипирован (например, при выбрасывании)
    private void ClearOwnerSlot(ItemInstance item)
    {
        if (item == null || item.transform.parent == null) return;

        EquipmentSlot slot = item.transform.parent.GetComponent<EquipmentSlot>();
        if (slot != null) slot.NotifyItemRemoved(item);
    }

    // Собирает данные предметов, которые лежат внутри контейнера ("Conteyner")
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

    // Восстанавливает предметы внутри открытого контейнера из ItemState.containerItems
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

            GameObject contentObj = Instantiate(data.data.modelPrefab2D, containerGrid.transform);
            ItemInstance content = contentObj.GetComponent<ItemInstance>();
            if (content == null)
            {
                Destroy(contentObj);
                containerState.containerItems.Add(data);
                continue;
            }

            ItemState contentState = content.GetItemState();
            if (contentState != null) contentState.SetStateData(data);
            content.Init();

            if (!TryPlaceInGridFirstFree(content, containerGrid, filter))
            {
                Destroy(contentObj);
                containerState.containerItems.Add(data);
            }
            else
            {
                // Ставим предмет ровно в центр занятых ячеек без анимации
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

    public bool TryPlaceInGridFirstFree(ItemInstance item, InitializeGrid grid, InventoryGrid filter)
    {
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

    // Сбрасывает состояние перетаскивания (например, после установки мода)
    public void ClearDragState()
    {
        draggedItem = null;
        isDragging = false;
    }

    // Публичный метод для проверки состояния перетаскивания
    public bool IsDragging() => isDragging;

    // Получение текущего перетаскиваемого предмета
    public ItemInstance GetDraggedItem() => draggedItem;

    //Выбрасывает предмет
    public void DropItem(ItemInstance item) 
    {
        if (item == null) return;

        float distance = 3f; // Сделаем чуть ближе к игроку

        // Берем направление взгляда, но УБИРАЕМ наклон вниз/вверх
        Vector3 forward = player.transform.forward;
        forward.y = 0;
        forward.Normalize();

        // Вычисляем позицию и поднимаем её на 1.5 метра над землей
        Vector3 dropPosition = player.transform.position + forward * distance;
        dropPosition.y += 1.5f;

        // Если контейнер открыт — закрываем его, чтобы панель вернулась внутрь предмета
        if (item == openContainer) CloseContainer();

        ItemStateData newItemData = item.GetStateData();

        // Запоминаем prefab UI до уничтожения предмета
        ItemState sourceState = item.GetItemState();
        GameObject childrenUIPrefab = sourceState != null ? sourceState.childrenUI : null;

        // Сохраняем содержимое контейнера. Если UI еще не восстанавливался,
        // данные уже лежат в ItemState.containerItems и остаются как есть.
        List<ItemStateData> uiContents = CaptureContainerContents(item);
        if (uiContents.Count > 0)
        {
            newItemData.containerItems = uiContents;
            if (sourceState != null && sourceState.containerItems != null && sourceState.containerItems.Count > 0)
                newItemData.containerItems.AddRange(sourceState.containerItems);
        }

        // Если предмет был экипирован — освобождаем слот
        ClearOwnerSlot(item);

        //Очищаем ячейки
        item.ReleaseCells();

        HandlePickedUpItem(item); // ? ИСПОЛЬЗУЕМ НАШ НОВЫЙ МЕТОД ДЛЯ ОЧИСТКИ 3D

        Destroy(item.gameObject);

        if (newItemData.data.modelPrefab3D != null)
        {
            GameObject obj = Instantiate(newItemData.data.modelPrefab3D, dropPosition, Quaternion.identity);
            obj.name = newItemData.data.itemName + "_3D";

            ItemState spawnedState = obj.GetComponent<ItemState>();
            if (spawnedState != null)
            {
                spawnedState.SetStateData(newItemData);

                // Сначала берем UI-префаб исходного предмета, затем — из ItemData.
                // Если оба пусты, остается значение из 3D-префаба.
                if (childrenUIPrefab != null)
                    spawnedState.childrenUI = childrenUIPrefab;
                else if (newItemData.data.modelPrefab2D != null)
                    spawnedState.childrenUI = newItemData.data.modelPrefab2D;
            }
        }
        else
        {
            Debug.LogWarning($"У предмета {newItemData.data.itemName} нет modelPrefab3D!");
        }        
    }

    // Пытается сохранить предмет: свободный слот, затем контейнер игрока
    public bool TryAddItemToInventory(ItemInstance item)
    {
        return TryStoreItem(item);
    }

    // Универсальное сохранение предмета после подбора/снятия мода
    public bool TryStoreItem(ItemInstance item)
    {
        if (item == null) return false;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return false;

        // Стак в слот не кладем: сначала пробуем контейнер
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

    // Подбор предмета из мира по нажатию клавиши
    public bool TryPickupWorldItem(GameObject worldObj)
    {
        if (worldObj == null) return false;

        ItemState worldState = worldObj.GetComponent<ItemState>();
        if (worldState == null || worldState.data == null || worldState.childrenUI == null) return false;

        // Инвентарь может быть закрыт. Временно включаем его, чтобы Awake успел
        // построить сетки и ячеек, а затем возвращаем исходное состояние.
        bool wasActive = gameObject.activeSelf;
        if (!wasActive) gameObject.SetActive(true);

        GameObject itemObj = Instantiate(worldState.childrenUI, transform);
        ItemInstance item = itemObj.GetComponent<ItemInstance>();
        if (item == null)
        {
            Destroy(itemObj);
            return false;
        }

        itemObj.SetActive(true);
        ItemState itemState = item.GetItemState();
        if (itemState == null)
        {
            Destroy(itemObj);
            return false;
        }

        itemState.SetStateData(worldState.GetStateData());
        item.Init();

        if (!TryStoreItem(item))
        {
            Destroy(itemObj);
            if (!wasActive) gameObject.SetActive(false);
            return false;
        }

        Destroy(worldObj);
        if (!wasActive) gameObject.SetActive(false);
        return true;
    }

    private EquipmentSlot FindFreeSlotFor(ItemState state)
    {
        if (state == null) return null;

        // Инвентарь может быть закрыт, поэтому слоты ищем и среди неактивных
        foreach (EquipmentSlot slot in FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Include))
        {
            if (slot.IsEmpty && slot.CanEquip(state))
                return slot;
        }

        return null;
    }

    private bool TryStoreInContainers(ItemInstance item)
    {
        if (item == null) return false;

        if (openContainer != null && TryAddToContainer(openContainer, item))
            return true;

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

    private ItemInstance FindEquippedContainer(ItemCategory category)
    {
        foreach (EquipmentSlot slot in FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Include))
        {
            if (slot.IsEmpty) continue;
            if ((slot.allowedCategories & category) == 0) continue;

            ItemInstance equipped = slot.GetEquippedInstance();
            if (equipped != null) return equipped;
        }

        return null;
    }

    private bool TryAddToContainer(ItemInstance container, ItemInstance item)
    {
        if (container == null || item == null) return false;

        Transform panel = container == openContainer && containerParent != null
            ? containerParent.Find("Conteyner")
            : container.transform.Find("Conteyner");
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

        // Сначала доливаем в существующие стаки
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
                Destroy(item.gameObject);
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

    public bool TryPlaceInFirstFreeCell(ItemInstance item)
    {
        if (item == null || openContainerGrid == null) return false;

        InventoryGrid filter = openContainerGrid.GetComponent<InventoryGrid>();
        if (filter == null) filter = openContainerGrid.GetComponentInParent<InventoryGrid>();
        if (filter == null) return false;

        foreach (GameObject cell in openContainerGrid.listGrid)
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

    //Открытие контейнера
    public void OpenContainer(ItemInstance item) 
    {
        if (item == null) return;

        // Если уже открыт другой контейнер — закрываем
        if (openContainer != null)
            CloseContainer();

        // Ищем дочерний объект "Conteyner" внутри предмета
        Transform containerPanel = item.transform.Find("Conteyner");
        if (containerPanel == null)
        {
            Debug.LogWarning($"У предмета {item.name} нет дочернего объекта 'Conteyner'!");
            return;
        }
        containerPanel.gameObject.SetActive(true);

        openContainerGrid = containerPanel.GetComponentInChildren<InitializeGrid>();
        if (openContainerGrid != null && openContainerGrid.listGrid.Count == 0)
            openContainerGrid.TryInit();

        // Перемещаем в Canvas (или оставляем внутри, если containerParent == null)
        if (containerParent != null)
        {
            containerPanel.SetParent(containerParent, false);
            containerParent.SetAsLastSibling();
        }

        // Принудительно раскладываем ячейки. Иначе при восстановлении предметов
        // координаты берутся у еще не разложенной сетки (все ячейки стоят в центре)
        Canvas.ForceUpdateCanvases();
        if (openContainerGrid != null)
        {
            RectTransform gridRect = openContainerGrid.GetComponent<RectTransform>();
            if (gridRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }

        // Возвращаем предметы, которые лежали в контейнере
        RestoreContainerContents(item, containerPanel, openContainerGrid);

        CanvasGroup cg = containerPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        Transform closeBtn = containerPanel.Find("Header/Button_Close");
        if (closeBtn != null)
        {
            Button btn = closeBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => CloseContainer());
            }
        }

        // Запоминаем открытый контейнер
        openContainer = item;
    }

    //Закрываем контейнер
    public void CloseContainer()
    {
        if (openContainer == null) return;

        // Ищем панель контейнера
        Transform containerPanel = containerParent != null
            ? containerParent.Find("Conteyner")
            : openContainer.transform.Find("Conteyner");

        if (containerPanel != null)
        {
            CanvasGroup cg = containerPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            // Возвращаем обратно в предмет
            containerPanel.SetParent(openContainer.transform, false);
            containerPanel.gameObject.SetActive(false);
        }

        openContainer = null;
        openContainerGrid = null;
    }

}
