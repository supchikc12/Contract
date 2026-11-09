using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// InventoryManager менеджер для всей системы перетаскивания
public class InventoryManager : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private PlayerManager player;
    [SerializeField] private GameObject CollectionZone;
    [SerializeField] private ContextMenuUI contextMenu;

    [Header("Контейнеры")]
    [SerializeField] private Transform containerParent; // Куда перемещать открытый контейнер (Canvas)
    [SerializeField] public ItemInstance openContainer;  // Текущий открытый контейнер
    [SerializeField] private InitializeGrid openContainerGrid;
    [Header("Состояние перетаскивания")]
    private ItemInstance draggedItem;
    private bool isDragging;
    private List<GameObject> activeCollectionObjects = new List<GameObject>();

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

    public bool IsInCollectionZone(ItemInstance item)
    {
        if (CollectionZone == null || item == null) return false;
        return item.transform.IsChildOf(CollectionZone.transform);
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

        bool fromCollectionZone = item.GetSavedParent() != null
            && item.GetSavedParent().IsChildOf(CollectionZone.transform);

        if (placed) item.OnDragEnd();
        else if (target == null && !fromCollectionZone) DropItem(item);
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
                    if (player != null && player.SelectedObj != null)
                    {
                        player.SelectedObj.Remove(worldObj);
                    }
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

            bool isCollectionZone = CollectionZone != null && grid.transform.IsChildOf(CollectionZone.transform);
            if (!isCollectionZone)
            {
                HandlePickedUpItem(draggedItem);
            }

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

    // Публичный метод для проверки состояния перетаскивания
    public bool IsDragging() => isDragging;

    // Получение текущего перетаскиваемого предмета
    public ItemInstance GetDraggedItem() => draggedItem;

    //Находит все обьекты на 3D сцене и добавляет их 2D версию в инвентарь для дальнейшего использования
    public void AddCollectionZone(List<GameObject> objects) {
        activeCollectionObjects.Clear();
        List<ItemInstance> active2DList = new List<ItemInstance>(CollectionZone.GetComponentsInChildren<ItemInstance>());

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            ItemState state3D = obj.GetComponent<ItemState>();
            if (state3D == null || state3D.childrenUI == null) continue;

            activeCollectionObjects.Add(obj);

            int remainingStack = state3D.currentStack;
            ItemData data3D = state3D.data;

            // 1. Пытаемся влить в существующие 2D стаки
            foreach (var ui2D in active2DList)
            {
                if (remainingStack <= 0) break;
                if (ui2D == null) continue;
                ItemState state2D = ui2D.GetItemState();

                if (state2D != null && state2D.data == data3D && state2D.currentStack < data3D.maxStackSize)
                {
                    int space = data3D.maxStackSize - state2D.currentStack;
                    int toAdd = Mathf.Min(remainingStack, space);

                    state2D.currentStack += toAdd;
                    ui2D.UpdateStackUI();
                    ui2D.WorldSourceObjects.Add(obj);

                    remainingStack -= toAdd;
                }
            }

            // 2. Если остались излишки
            while (remainingStack > 0)
            {
                ScrollRectNoDrag scrollRectNoDrag = CollectionZone.GetComponentInChildren<ScrollRectNoDrag>();
                if (scrollRectNoDrag == null) break;

                Transform viewport = scrollRectNoDrag.viewport;

                GameObject item = Instantiate(state3D.childrenUI, viewport);

                ItemInstance uiInstance = item.GetComponent<ItemInstance>();
                if (uiInstance == null) { /*...*/ break; }

                ItemState state2D = uiInstance.GetItemState();
                if (state2D != null)
                {
                    state2D.SetStateData(state3D.GetStateData());

                    int stackForThisItem = Mathf.Min(remainingStack, data3D.maxStackSize);
                    state2D.currentStack = stackForThisItem;
                    remainingStack -= stackForThisItem;

                    uiInstance.WorldSourceObjects.Add(obj);
                }

                // ? ДОБАВЛЯЕМ ИНИЦИАЛИЗАЦИЮ ВРУЧНУЮ (сетка сгенерируется здесь)
                uiInstance.Init();

                active2DList.Add(uiInstance);

                // Размещаем
                draggedItem = uiInstance;
                InitializeGrid initializeGrid = scrollRectNoDrag.viewport.GetComponentInChildren<InitializeGrid>();
                List<GameObject> cells = initializeGrid.listGrid;

                bool placedSuccessfully = false;
                foreach (GameObject cell in cells)
                {
                    if (TryPlaceInGrid(cell))
                    {
                        placedSuccessfully = true;
                        break;
                    }
                }

                if (!placedSuccessfully)
                {
                    Destroy(item); // Если не влезло, удаляем
                }

                draggedItem = null;
                isDragging = false;
            }
        }
    }

    //Удаляет обьекты с 3D сцены которые подобрал игрок
    public void ReplaceCollectionZone(List<GameObject> objects)
    {
        // Если контейнер был открыт — закрываем, чтобы его содержимое вернулось в предмет
        if (openContainer != null) CloseContainer();

        ItemInstance[] all2D = CollectionZone.GetComponentsInChildren<ItemInstance>(true);

        // Сохраняем содержимое контейнеров в связанные 3D-объекты,
        // иначе при следующем открытии инвентаря предметы внутри пропадут
        foreach (var ui in all2D)
        {
            if (ui != null) SaveContainerContentsToWorld(ui);
        }

        HashSet<GameObject> stillLinked3DObjects = new HashSet<GameObject>();

        // Собираем все 3D объекты, которые еще привязаны к 2D стакам в зоне
        foreach (var ui in all2D)
        {
            if (ui.WorldSourceObjects != null)
            {
                foreach (var worldObj in ui.WorldSourceObjects)
                {
                    if (worldObj != null) stillLinked3DObjects.Add(worldObj);
                }
            }
        }

        // Удаляем только те 3D объекты, которые игрок забрал (их нет в списке привязанных)
        foreach (GameObject obj in activeCollectionObjects)
        {
            if (obj == null) continue;
            if (!stillLinked3DObjects.Contains(obj))
            {
                Destroy(obj);
            }
        }

        activeCollectionObjects.Clear();

        // Уничтожаем 2D копии
        foreach (var ui in all2D)
        {
            Destroy(ui.gameObject);
        }
    }

    // Сохраняет содержимое контейнера обратно в связанные 3D-объекты
    private void SaveContainerContentsToWorld(ItemInstance item)
    {
        if (item == null || item.WorldSourceObjects == null || item.WorldSourceObjects.Count == 0) return;

        ItemState itemState = item.GetItemState();
        if (itemState == null || itemState.data == null) return;

        bool isContainer = (itemState.data.category & (ItemCategory.Container | ItemCategory.Backpack)) != 0;
        if (!isContainer) return;

        // Берем содержимое из UI. Если оно еще не восстанавливалось,
        // используем данные, лежащие в ItemState.containerItems.
        List<ItemStateData> contents = CaptureContainerContents(item);
        if (contents.Count > 0)
        {
            if (itemState.containerItems != null && itemState.containerItems.Count > 0)
                contents.AddRange(itemState.containerItems);
        }
        else if (itemState.containerItems != null)
        {
            contents = itemState.containerItems;
        }

        foreach (GameObject worldObj in item.WorldSourceObjects)
        {
            if (worldObj == null) continue;
            ItemState worldState = worldObj.GetComponent<ItemState>();
            if (worldState == null) continue;
            worldState.containerItems = new List<ItemStateData>(contents);
        }
    }

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
            containerPanel.SetParent(containerParent, false);

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
