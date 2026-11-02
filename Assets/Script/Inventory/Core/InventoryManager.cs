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
    [SerializeField] private ItemInstance openContainer; // Текущий открытый контейнер
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
        if (item != null && item.WorldSourceObject != null)
        {
            // 1. Удаляем 3D объект из мира
            Destroy(item.WorldSourceObject);

            // 2. Убираем его из списка подобранных у игрока, чтобы ReplaceCollectionZone не искал его снова
            if (player != null && player.SelectedObj != null)
            {
                player.SelectedObj.Remove(item.WorldSourceObject);
            }

            // 3. Обнуляем ссылку, чтобы не удалить его дважды
            item.WorldSourceObject = null;
        }
    }
    private bool TryPlaceInGrid(GameObject target)
    {
        if (draggedItem == null) return false;

        // Попытка экипировать в слот
        EquipmentSlot equipmentSlot = target.GetComponent<EquipmentSlot>();
        if (equipmentSlot != null)
        {
            if (equipmentSlot.CanEquip(draggedItem.GetItemState()))
            {
                equipmentSlot.EquipItem(draggedItem);

                // При экипировке предмет всегда считается подобранным
                HandlePickedUpItem(draggedItem);
                return true;
            }
        }
        // Попытка поместить в инвентарь/зону
        else
        {
            InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
            if (grid != null && draggedItem.CanPlaceInGrid(target, grid))
            {
                draggedItem.PlaceInGrid(target);

                // Проверяем, является ли эта сетка CollectionZone
                bool isCollectionZone = CollectionZone != null && grid.transform.IsChildOf(CollectionZone.transform);

                // Если положили НЕ в CollectionZone, значит предмет подобрали
                if (!isCollectionZone)
                {
                    HandlePickedUpItem(draggedItem);
                }

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
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                ItemState itemState3D = obj.GetComponent<ItemState>();
                if (itemState3D != null && itemState3D.childrenUI != null)
                {
                    ScrollRectNoDrag scrollRectNoDrag = CollectionZone.GetComponentInChildren<ScrollRectNoDrag>();
                    if (scrollRectNoDrag != null)
                    {
                        Transform viewport = scrollRectNoDrag.viewport;

                        // Создаем 2D копию
                        GameObject item = Instantiate(itemState3D.childrenUI, viewport);
                        ItemInstance uiInstance = item.GetComponent<ItemInstance>();

                        if (uiInstance == null)
                        {
                            Debug.LogError("ItemInstance не найден на childrenUI!");
                            continue;
                        }

                        uiInstance.WorldSourceObject = obj;

                        // ПЕРЕНОСИМ ДАННЫЕ (включая uniqueId, прочность, содержимое)
                        ItemState state2D = uiInstance.GetItemState();
                        if (state2D != null) state2D.SetStateData(itemState3D.GetStateData());

                        draggedItem = uiInstance;
                        InitializeGrid initializeGrid = CollectionZone.GetComponentInChildren<InitializeGrid>();
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
                            draggedItem.ReturnToStart();
                        }

                        draggedItem = null;
                        isDragging = false;
                    }
                }
            }
        }

    }

    //Удаляет обьекты с 3D сцены которые подобрал игрок
    public void ReplaceCollectionZone(List<GameObject> objects)
    {
        // 1. Собираем ID всех 2D-предметов, которые сейчас лежат в CollectionZone (игрок их не взял)
        ItemInstance[] all2D = CollectionZone.GetComponentsInChildren<ItemInstance>();
        List<string> unpickedIds = new List<string>();

        foreach (var ui in all2D)
        {
            ItemState state = ui.GetItemState();
            if (state != null && !string.IsNullOrEmpty(state.uniqueId))
            {
                unpickedIds.Add(state.uniqueId);
            }
        }

        // 2. Проверяем 3D-объекты
        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            ItemState state3D = obj.GetComponent<ItemState>();
            if (state3D != null)
            {
                // Если ID предмета НЕТ в списке оставшихся в CollectionZone, значит его подобрали!
                if (!unpickedIds.Contains(state3D.uniqueId))
                {
                    Destroy(obj); // Удаляем 3D объект из мира
                }
            }
        }

        // 3. Очищаем CollectionZone от 2D-копий (при следующем открытии они создадутся заново)
        foreach (var ui in all2D)
        {
            Destroy(ui.gameObject);
        }
    }

    //Выбрасывает предмет
    public void DropItem(ItemInstance item) 
    {
        if (item == null) return;

        float distance = 5f;

        //Вычисляем направление взгяла игрока
        Vector3 forward = player.transform.forward;
        Vector3 dropPosition = player.transform.position + forward * distance;
        
        ItemStateData newItemData = item.GetStateData();

        //Очищаем ячейки
        item.ReleaseCells();

        if (item == openContainer) CloseContainer();

        if (item.WorldSourceObject != null)
        {
            Destroy(item.WorldSourceObject);
            if (player != null && player.SelectedObj != null) player.SelectedObj.Remove(item.WorldSourceObject);
        }

        Destroy(item.gameObject);

        if (newItemData.data.modelPrefab3D != null)
        {
            GameObject obj = Instantiate(newItemData.data.modelPrefab3D, dropPosition, Quaternion.identity);
            obj.GetComponent<ItemState>().SetStateData(newItemData);
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
        }

        openContainer = null;
        openContainerGrid = null;
    }    
}
