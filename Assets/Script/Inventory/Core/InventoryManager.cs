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
            targetCanvas = FindFirstObjectByType<Canvas>();

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
                equipmentSlot.EquipItem(draggedItem);
                HandlePickedUpItem(draggedItem);
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

                // ← ДОБАВЛЯЕМ ИНИЦИАЛИЗАЦИЮ ВРУЧНУЮ (сетка сгенерируется здесь)
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
        ItemInstance[] all2D = CollectionZone.GetComponentsInChildren<ItemInstance>();
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

        ItemStateData newItemData = item.GetStateData();

        //Очищаем ячейки
        item.ReleaseCells();

        if (item == openContainer) CloseContainer();

        HandlePickedUpItem(item); // ← ИСПОЛЬЗУЕМ НАШ НОВЫЙ МЕТОД ДЛЯ ОЧИСТКИ 3D

        Destroy(item.gameObject);

        if (newItemData.data.modelPrefab3D != null)
        {
            GameObject obj = Instantiate(newItemData.data.modelPrefab3D, dropPosition, Quaternion.identity);
            obj.name = newItemData.data.itemName + "_3D";

            ItemState spawnedState = obj.GetComponent<ItemState>();
            if (spawnedState != null)
            {
                spawnedState.SetStateData(newItemData);
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
        // Перемещаем в Canvas (или оставляем внутри, если containerParent == null)
        if (containerParent != null)
            containerPanel.SetParent(containerParent, false);

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
