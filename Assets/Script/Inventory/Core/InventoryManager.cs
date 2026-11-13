using UnityEngine;
using UnityEngine.UI;

// InventoryManager Ч координатор инвентар€.
// ќтвечает за ссылки, маршрутизацию drag-событий и публичный API.
// ’ранение и контейнеры Ч InventoryStorage, карточки дока Ч InventoryContainerDock.
public class InventoryManager : MonoBehaviour
{
    [Header("—сылки на компоненты")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Transform player;
    [SerializeField] private ContextMenuUI contextMenu;

    private InventoryStorage storage;
    private InventoryContainerDock dock;

    private ItemInstance draggedItem;
    private bool isDragging;

    public static InventoryManager Instance { get; private set; }

    internal Transform Player => player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (GetComponent<EffectsHUD>() == null)
            gameObject.AddComponent<EffectsHUD>();

        ScrollRect scrollRect = gameObject.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) scrollRect.enabled = true;

        if (targetCanvas == null)
            targetCanvas = FindAnyObjectByType<Canvas>();

        if (player == null)
            player = transform.root;

        if (contextMenu == null && targetCanvas != null)
            contextMenu = targetCanvas.GetComponentInChildren<ContextMenuUI>();

        dock = GetComponentInChildren<InventoryContainerDock>(true);
        storage = new InventoryStorage(this, dock);
    }

    private void Update()
    {
        if (isDragging && draggedItem != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
                draggedItem.RequestRotate();
        }
    }

    public void HandleLeftClick(ItemInstance item)
    {
        // Ћ ћ без drag Ч ничего не делаем.
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
        bool placed = target != null && item.TryPlaceOn(target);

        if (placed) item.OnDragEnd();
        else if (target == null) DropItem(item);
        else item.ReturnToStart();

        draggedItem = null;
        isDragging = false;
    }

    public void ClearDragState()
    {
        draggedItem = null;
        isDragging = false;
    }

    public bool IsDragging() => isDragging;

    public ItemInstance GetDraggedItem() => draggedItem;

    // --- ѕубличный API хранени€ ---

    public bool TryPlaceInGridFirstFree(ItemInstance item, InitializeGrid grid, InventoryGrid filter)
    {
        return storage != null && storage.TryPlaceInGridFirstFree(item, grid, filter);
    }

    public bool TryAddItemToInventory(ItemInstance item)
    {
        return storage != null && storage.TryAddItemToInventory(item);
    }

    public bool TryStoreItem(ItemInstance item)
    {
        return storage != null && storage.TryStoreItem(item);
    }

    public bool TryPickupWorldItem(GameObject worldObj)
    {
        return storage != null && storage.TryPickupWorldItem(worldObj);
    }

    public bool TryPlaceInFirstFreeCell(ItemInstance item)
    {
        return storage != null && storage.TryPlaceInFirstFreeCell(item);
    }

    public ItemInstance FindEquippedContainer(ItemCategory category)
    {
        return storage != null ? storage.FindEquippedContainer(category) : null;
    }

    public void DropItem(ItemInstance item)
    {
        if (storage != null) storage.DropItem(item);
    }

    public void RefreshContainerDock()
    {
        if (dock != null) dock.Refresh();
    }
}
