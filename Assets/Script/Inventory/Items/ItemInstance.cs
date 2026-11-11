using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ItemInstance теперь координатор между данными (ItemState) и представлением (ItemView)
// Управляет логикой предмета, но делегирует визуализацию и проверки другим компонентам
public class ItemInstance : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IInventoryClickHandler
{
    [Header("Компоненты")]
    private ItemState itemState;
    private ItemView itemView;
    private InitializeGrid itemGrid;
    private ItemGridPlacement placement;

    [Header("Состояние")]
    [SerializeField] private Vector3 savedPosition;
    [SerializeField] private Transform savedParent;
    private bool isInitialized = false;
    private bool dropHandled = false;

    public Transform GetSavedParent() => savedParent;
    public void ReleaseCells() => placement?.ReleaseCells();

    private void Awake()
    {
        // Получаем ссылки на компоненты
        itemState = GetComponent<ItemState>();
        if (itemState == null)
        {
            Debug.LogError("ItemInstance requires ItemState component!");
        }

        itemView = GetComponent<ItemView>();
        if (itemView == null)
        {
            itemView = gameObject.AddComponent<ItemView>();
        }

        itemGrid = GetComponentInChildren<InitializeGrid>();

        placement = new ItemGridPlacement(this, itemState, itemView);
        placement.BindGrid(itemGrid);
    }

    private void Start()
    {
        if (itemState != null && itemState.data != null)
        {
            Init(); // Вызовется автоматически, если не был вызван вручную
        }
    }

    public void Init()
    {
        if (isInitialized) return;
        isInitialized = true;

        itemGrid = GetComponentInChildren<InitializeGrid>();
        if (placement != null) placement.BindGrid(itemGrid);
        if (itemGrid != null)
        {
            itemGrid.TryInit();
            HideItemGridVisuals();
        }

        UpdateStackUI();

        Transform container = transform.Find("Conteyner");
        if (container != null)
        {
            container.gameObject.SetActive(false); // Полностью выключаем внутренности рюкзака
        }
    }

    public void UpdateStackUI()
    {
        if (itemView != null && itemState != null)
        {
            itemView.UpdateStackDisplay(itemState.currentStack);
        }
    }

    public void ReturnToStart()
    {
        ReturnToStart(savedPosition, savedParent);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            HandleLeftClick();
        else if (eventData.button == PointerEventData.InputButton.Right)
            HandleRightClick();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        HandleBeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Если предмет уже обработан слотом модификации, не возвращаем его в инвентарь
        if (dropHandled)
        {
            dropHandled = false;
            return;
        }

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        HandleEndDrag(target);
    }

    // Сообщает, что предмет уже обработан другим обработчиком (например, слотом модификации)
    public void MarkDropHandled()
    {
        dropHandled = true;
    }

    // IInventoryClickHandler
    public void HandleLeftClick() { InventoryManager.Instance.HandleLeftClick(this); }
    public void HandleRightClick() { InventoryManager.Instance.HandleRightClick(this); }
    public void HandleBeginDrag() 
    {
        OnDragStart();
        InventoryManager.Instance.HandleBeginDrag(this); 
    }
    public void HandleDrag() { InventoryManager.Instance.HandleDrag(this); }
    public void HandleEndDrag(GameObject target) 
    {
        InventoryManager.Instance.HandleEndDrag(this, target); 
    }
    // --- Публичные методы для InventoryManager ---

    public bool CanStartDrag()
    {
        return itemState != null && itemState.IsValid();
    }

    public ItemState GetItemState()
    {
        return itemState;
    }

    public void OnDragStart()
    {
        // Сохраняем текущую позицию
        savedPosition = transform.localPosition;
        savedParent = transform.parent;

        // Запоминаем поворот и ячейки, чтобы вернуть предмет на место
        placement.CaptureStartState();
        placement.ReleaseCells();

        // Визуальные эффекты начала перетаскивания
        if (itemView != null)
        {
            itemView.StartDragVisuals();
        }

        transform.SetParent(GetComponentInParent<InventoryManager>().transform);
    }

    public void UpdateDragPosition(Vector2 screenPosition)
    {
        if (itemView != null)
        {
            itemView.SetPosition(screenPosition);
        }
    }

    public void OnDragEnd()
    {
        if (itemView != null)
        {
            itemView.EndDragVisuals();
        }
    }

    // Пытается положить предмет в слот, стак или сетку под курсором.
    public bool TryPlaceOn(GameObject target)
    {
        if (target == null) return false;

        // 1. Слот экипировки
        EquipmentSlot equipmentSlot = target.GetComponentInParent<EquipmentSlot>();
        if (equipmentSlot != null && equipmentSlot.CanEquip(itemState))
        {
            EquipFromDrag(equipmentSlot);
            return true;
        }

        // 2. Стак
        ItemInstance targetItem = target.GetComponentInParent<ItemInstance>();
        if (targetItem != null && targetItem != this)
        {
            ItemState targetState = targetItem.GetItemState();
            ItemState sourceState = itemState;

            if (targetState != null && sourceState != null &&
                targetState.data == sourceState.data && targetState.data.maxStackSize > 1)
            {
                int spaceLeft = targetState.data.maxStackSize - targetState.currentStack;

                if (spaceLeft > 0)
                {
                    int amountToMove = Mathf.Min(sourceState.currentStack, spaceLeft);

                    targetState.currentStack += amountToMove;
                    sourceState.currentStack -= amountToMove;
                    targetItem.UpdateStackUI();

                    if (sourceState.currentStack <= 0)
                    {
                        ReleaseOwnerSlot();
                        Destroy(gameObject);
                        return true;
                    }

                    UpdateStackUI();
                }
            }
        }

        // 3. Сетка
        InventoryGrid grid = target.GetComponentInParent<InventoryGrid>();
        if (grid != null && CanPlaceInGrid(target, grid))
        {
            PlaceInGrid(target);
            return true;
        }

        return false;
    }

    private void EquipFromDrag(EquipmentSlot slot)
    {
        slot.EquipItem(this);

        if (transform.parent != slot.transform)
        {
            ReturnToStart();
        }

    }

    // Сообщает слоту, что предмет больше не принадлежит ему.
    public void ReleaseOwnerSlot()
    {
        if (transform.parent == null) return;

        EquipmentSlot slot = transform.parent.GetComponent<EquipmentSlot>();
        if (slot != null) slot.NotifyItemRemoved(this);
    }

    public void RequestRotate()
    {
        if (placement != null) placement.RequestRotate();
    }

    public bool CanPlaceInGrid(GameObject targetCell, InventoryGrid grid)
    {
        return placement != null && placement.CanPlaceInGrid(targetCell, grid);
    }

    public void PlaceInGrid(GameObject targetCell)
    {
        if (placement != null) placement.PlaceInGrid(targetCell);
    }

    public void ReturnToStart(Vector3 startPosition, Transform startParent)
    {
        // Возвращаем поворот, если он был изменен
        if (placement != null) placement.RestoreStartRotation();

        // Визуальный эффект тряски и возврат
        if (itemView != null)
        {
            itemView.PlayShakeEffect(() =>
            {
                transform.SetParent(startParent, false);
                transform.localPosition = startPosition;
                itemView.EndDragVisuals();
                if (placement != null) placement.RestoreSavedCells();
            });
        }
        else
        {
            transform.SetParent(startParent, false);
            transform.localPosition = startPosition;

            if (placement != null)
            {
                placement.ApplyReturnOffset();
                placement.RestoreSavedCells();
            }
        }
    }

    // --- Приватные вспомогательные методы ---

    // Скрывает служебную сетку предмета, чтобы она не перекрывала иконку
    private void HideItemGridVisuals()
    {
        if (itemGrid == null) return;

        foreach (GameObject cellObj in itemGrid.listGrid)
        {
            if (cellObj == null) continue;

            Image cellImage = cellObj.GetComponent<Image>();
            if (cellImage != null) cellImage.enabled = false;
        }
    }

    // Возвращает центр блока ячеек, которые занимает предмет
    public bool TryGetOccupiedCellsCenter(out Vector3 center)
    {
        if (placement != null) return placement.TryGetOccupiedCellsCenter(out center);

        center = Vector3.zero;
        return false;
    }

    // Отделяет от стака указанное количество предметов в отдельную копию.
    // Исходный стак уменьшается, копия сразу инициализируется.
    public ItemInstance CreateStackCopy(int amount)
    {
        ItemState sourceState = GetItemState();
        if (sourceState == null || amount <= 0 || sourceState.currentStack <= 1) return null;

        if (amount > sourceState.currentStack - 1)
            amount = sourceState.currentStack - 1;

        GameObject cloneObj = Instantiate(gameObject, transform.parent, false);
        cloneObj.name = gameObject.name;

        ItemInstance clone = cloneObj.GetComponent<ItemInstance>();
        if (clone == null)
        {
            Destroy(cloneObj);
            return null;
        }

        ItemState cloneState = clone.GetItemState();
        cloneState.currentStack = amount;
        cloneState.uniqueId = System.Guid.NewGuid().ToString();

        // Исходный стак уменьшается на отделенное количество
        sourceState.currentStack -= amount;
        UpdateStackUI();

        clone.Init();
        return clone;
    }

    // Метод для получения данных состояния (для экипировки)
    public ItemStateData GetStateData()
    {
        return itemState != null ? itemState.GetStateData() : default;
    }

    private void OnDestroy()
    {
        if (placement != null) placement.ReleaseCells();
    }
}
