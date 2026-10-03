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
    private ItemInstance openContainer; // Текущий открытый контейнер

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
        bool placed = false;
        if (target != null) placed = TryPlaceInGrid(target);

        if (!placed)
            item.ReturnToStart();  // ← сам управляет визуалом (тряска + возврат)
        
        item.OnDragEnd();  // ← сброс визуала после успешного размещения

        draggedItem = null;
        isDragging = false;
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
                return true;
            }
        }
        // Попытка поместить в инвентарь
        else
        {
            InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
            if (grid != null && draggedItem.CanPlaceInGrid(target, grid))
            {
                draggedItem.PlaceInGrid(target);
                return true;
            }
        }
        return false;
    }

    // Попытка завершить перетаскивание
    //private void TryEndDrag()
    //{
    //    if (draggedItem == null)
    //    {
    //        isDragging = false;
    //        return;
    //    }

    //    PointerEventData pointerData = new PointerEventData(eventSystem) 
    //    { 
    //        position = Input.mousePosition 
    //    };

    //    GameObject target = GetTargetUnderMouse(pointerData);
    //    bool placedSuccessfully = false;

    //    if (target != null)
    //    {
    //        placedSuccessfully = TryPlaceInGrid(target);  
    //    }

    //    // Если не удалось разместить - возвращаем на место
    //    if (!placedSuccessfully)
    //    {
    //        draggedItem.ReturnToStart(dragStartPosition, dragStartParent);
    //    }

    //    //if (draggedItem != null)
    //    //{
    //    //    List<RaycastResult> results = new List<RaycastResult>();
    //    //    results = Raycaster();

    //    //    foreach (var result in results)
    //    //    {
    //    //        if (result.gameObject.GetComponent<ScrollRect>())
    //    //        {
    //    //            var sr = result.gameObject.GetComponent<ScrollRect>();
    //    //            if (sr != null)
    //    //            {
    //    //                sr.enabled = true;
    //    //            }
    //    //        }
    //    //    }
    //    //}
    //    draggedItem.OnDragEnd();
    //    ClearDragState();
    //}

    // Получение объекта под курсором
    //private GameObject GetTargetUnderMouse(PointerEventData pointerData)
    //{
    //    if (raycaster == null) return null;

    //    List<RaycastResult> results = new List<RaycastResult>();
    //    raycaster.Raycast(pointerData, results);

    //    foreach (var result in results)
    //    {
    //        // Пропускаем сам перетаскиваемый предмет и его дочерние объекты
    //        if (draggedItem != null && IsChildOf(result.gameObject, draggedItem.gameObject) || result.gameObject.layer == ITEM_LAYER)
    //        {
    //            continue;
    //        }

    //        // Проверяем, является ли объект ячейкой или слотом
    //        if (result.gameObject.GetComponent<InventoryCell>() != null || 
    //            result.gameObject.GetComponent<EquipmentSlot>() != null)
    //        {
    //            return result.gameObject;
    //        }
    //    }

    //    return null;
    //}

    // Проверка, является ли объект дочерним
    //private bool IsChildOf(GameObject obj, GameObject parent)
    //{
    //    Transform current = obj.transform;
    //    while (current != null)
    //    {
    //        if (current.gameObject == parent)
    //            return true;
    //        current = current.parent;
    //    }
    //    return false;
    //}

    // Очистка состояния перетаскивания
    //private void ClearDragState()
    //{
    //    draggedItem = null;
    //    dragStartPosition = Vector3.zero;
    //    dragStartParent = null;
    //    isDragging = false;
    //}

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
                ItemState itemState = obj.GetComponent<ItemState>();
                if (itemState != null)
                {
                    GameObject childrenUI = itemState.childrenUI;
                    ScrollRectNoDrag scrollRectNoDrag = CollectionZone.GetComponentInChildren<ScrollRectNoDrag>();
                    if (scrollRectNoDrag != null) {
                        Transform viewport = scrollRectNoDrag.viewport;
                        GameObject item = Instantiate(childrenUI, viewport);
                        draggedItem = item.GetComponent<ItemInstance>();

                        if (draggedItem == null)
                        {
                            Debug.LogError("ItemInstance не найден на childrenUI!");
                            continue;
                        }

                        InitializeGrid initializeGrid = CollectionZone.GetComponentInChildren<InitializeGrid>();
                        List <GameObject> cells = initializeGrid.listGrid;

                        bool placedSuccessfully = false;
                        foreach (GameObject cell in cells) {
                            if (placedSuccessfully = TryPlaceInGrid(cell)) {
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
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }

    //Открывает контекстое меню
    //public void OpenContextMenu()
    //{
    //    PointerEventData pointerData = new PointerEventData(eventSystem)
    //    {
    //        position = Input.mousePosition
    //    };

    //    List<RaycastResult> results = new List<RaycastResult>();
    //    raycaster.Raycast(pointerData, results);

    //    foreach (var result in results)
    //    {
    //        // Проверка слоя предметов
    //        if (result.gameObject.layer == ITEM_LAYER)
    //        {
    //            ItemInstance item = result.gameObject.GetComponent<ItemInstance>();
    //            if (item != null)
    //            {
    //                contextMenu.Show(item);
    //                return;
    //            }
    //        }
    //    }

    //    contextMenu.Hide();
    //}

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
        InventoryCell cell = item.GetComponentInParent<InventoryCell>();
        if (cell != null) cell.Clear();

        Destroy(item.gameObject);

        if (newItemData.data.modelPrefab3D != null)
        {
            Instantiate(newItemData.data.modelPrefab3D, dropPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"У предмета {newItemData.data.itemName} нет modelPrefab3D!");
        }        
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
    }    
}
