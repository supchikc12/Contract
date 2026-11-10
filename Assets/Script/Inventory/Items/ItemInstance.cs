using System.Collections.Generic;
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
    public void ReleaseCells() => FreeOccupiedCells();
    [Header("Состояние")]
    private bool isRotated, StartRotated = false;
    [SerializeField] private Vector3 savedPosition;
    [SerializeField] private Transform savedParent;
    private bool isInitialized = false;
    private bool dropHandled = false;

    public Transform GetSavedParent() => savedParent;
    [SerializeField] private List<InventoryCell> occupiedCells = new List<InventoryCell>();
    // Ячейки, которые предмет занимал до начала перетаскивания (для возврата на место)
    private List<InventoryCell> savedOccupiedCells = new List<InventoryCell>();

    [Header("Связь с миром")]
    public List<GameObject> WorldSourceObjects = new List<GameObject>();

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
        // Если мы тащим открытый контейнер, сначала закрываем его
        if (InventoryManager.Instance != null && InventoryManager.Instance.openContainer == this)
        {
            InventoryManager.Instance.CloseContainer();
        }

        // Сохраняем текущую позицию
        savedPosition = transform.localPosition;
        savedParent = transform.parent;

        // Сохраняем текущее состояние поворота
        StartRotated = isRotated;

        // Запоминаем ячейки, чтобы вернуть предмет на место при неудачном перетаскивании
        savedOccupiedCells.Clear();
        if (occupiedCells != null) savedOccupiedCells.AddRange(occupiedCells);

        // Освобождаем занятые ячейки
        FreeOccupiedCells();

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
            InventoryManager.Instance.HandlePickedUpItem(this);
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

        InventoryManager.Instance.HandlePickedUpItem(this);
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
        // Переключаем состояние поворота
        isRotated = !isRotated;

        // Визуальный поворот
        float targetAngle = isRotated ? -90f : 0f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        if (itemView != null)
        {
            itemView.SmoothRotateTo(targetRotation);
        }
        else
        {
            transform.localRotation = targetRotation;
        }

        // Логический поворот сетки
        RotateGridCells();
    }

    public bool CanPlaceInGrid(GameObject targetCell, InventoryGrid grid)
    {
        if (itemState == null || itemState.data == null || itemGrid == null)
        {
            return false;
        }

        PlacementData placementData;
        if (ItemPlacementValidator.CanPlaceItem(targetCell, itemGrid, grid, itemState.data, out placementData))
            return true;
        else if (ItemPlacementValidator.TryFindNearbyPosition(targetCell, itemGrid, grid, itemState.data, out placementData))
            return true;

        return false;
        
    }

    public void PlaceInGrid(GameObject targetCell)
    {
        InventoryGrid grid = InventoryGrid.FindInParents(targetCell.transform);
        if (grid == null || itemState == null || itemState.data == null)
        {
            return;
        }

        PlacementData placementData;
        
        // Пробуем разместить в целевой ячейке или рядом
        if (ItemPlacementValidator.CanPlaceItem(targetCell, itemGrid, grid, itemState.data, out placementData) || 
            ItemPlacementValidator.TryFindNearbyPosition(targetCell, itemGrid, grid, itemState.data, out placementData))
        {
            savedParent = placementData.inventoryInit.transform;
            ExecutePlacement(placementData);
        }

    }

    public void ReturnToStart(Vector3 startPosition, Transform startParent)
    {
        // Возвращаем поворот, если он был изменен
        if (isRotated != StartRotated)
        {
            RequestRotate();
        }

        // Визуальный эффект тряски и возврат
        if (itemView != null)
        {
            itemView.PlayShakeEffect(() =>
            {
                transform.SetParent(startParent, false);
                transform.localPosition = startPosition;
                itemView.EndDragVisuals();
                RestoreSavedCells();
            });
        }
        else
        {
            transform.SetParent(startParent, false);
            transform.localPosition = startPosition;

            if (itemGrid != null && itemGrid.listGrid.Count > 1)
            {
                if (isRotated)
                    transform.position = new Vector3(transform.position.x, transform.position.y + 25f);
                else
                    transform.position = new Vector3(transform.position.x - 25f, transform.position.y);
            }

            RestoreSavedCells();
        }
    }

    // --- Приватные вспомогательные методы ---

    private void ExecutePlacement(PlacementData placementData)
    {
        // Сохраняем текущие RectTransform/scale чтобы восстановить после SetParent
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 savedSize = rt != null ? rt.sizeDelta : Vector2.zero;
        Vector3 savedScale = transform.localScale;
        Vector2 savedAnchorMin = rt != null ? rt.anchorMin : Vector2.zero;
        Vector2 savedAnchorMax = rt != null ? rt.anchorMax : Vector2.zero;
        Vector2 savedPivot = rt != null ? rt.pivot : Vector2.zero;

        // Добавляем LayoutElement и просим игнорировать Layout, чтобы GridLayoutGroup не менял size/pos
        LayoutElement layoutEl = GetComponent<LayoutElement>();

        if (layoutEl == null)
        {
            layoutEl = gameObject.AddComponent<LayoutElement>();
        }
        layoutEl.ignoreLayout = true;
        if (rt != null)
        {
            layoutEl.preferredWidth = savedSize.x;
            layoutEl.preferredHeight = savedSize.y;
        }

        // Устанавливаем родителя
        transform.SetParent(savedParent, worldPositionStays: true);

        rt.sizeDelta = savedSize;

        // Вычисляем целевую позицию. В закрытом контейнере LayoutGroup ещё не
        // расставил ячейки, поэтому берём позицию из параметров сетки.
        Vector3 targetLocalPos = savedParent.InverseTransformPoint(placementData.targetCell.transform.position);
        if (placementData.inventoryInit != null && placementData.targetCell != null)
        {
            InventoryCell targetCellData = placementData.targetCell.GetComponent<InventoryCell>();
            if (targetCellData != null)
                targetLocalPos = placementData.inventoryInit.GetCellLocalPosition(targetCellData.gridSize);
        }

        // Корректировка для многоячеечных предметов
        if (itemGrid.listGrid.Count > 1 && (itemState.data.size.x % 2 == 0 || itemState.data.size.y % 2 == 0))
        {
            if (itemState.data.size.x != itemState.data.size.y)
            {
                if (isRotated)
                    targetLocalPos = new Vector3(targetLocalPos.x, targetLocalPos.y + 25f);
                else
                    targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
            }
            else 
            {
                targetLocalPos = new Vector3(targetLocalPos.x, targetLocalPos.y + 25f);
                targetLocalPos = new Vector3(targetLocalPos.x - 25f, targetLocalPos.y);
            }
        }

        // Восстанавливаем размеры и масштаб (чтобы избежать адаптации LayoutGroup)
        if (rt != null)
        {
            rt.anchorMin = savedAnchorMin;
            rt.anchorMax = savedAnchorMax;
            rt.pivot = savedPivot;
            rt.sizeDelta = savedSize;
        }
        transform.localScale = savedScale;

        // Плавное перемещение
        if (itemView != null)
        {
            itemView.SmoothMoveTo(targetLocalPos);
        }
        else
        {
            transform.localPosition = targetLocalPos;
        }

        // Занимаем ячейки
        occupiedCells = placementData.targetGrids;
        ItemPlacementValidator.OccupyCells(occupiedCells, gameObject);
        savedOccupiedCells.Clear();
    }

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

    private void FreeOccupiedCells()
    {
        if (occupiedCells != null && occupiedCells.Count > 0)
        {
            ItemPlacementValidator.FreeCells(occupiedCells);
            occupiedCells.Clear();
        }
    }

    // Возвращает занятие ячеек, которые предмет занимал до перетаскивания
    private void RestoreSavedCells()
    {
        if (savedOccupiedCells == null || savedOccupiedCells.Count == 0) return;

        occupiedCells = new List<InventoryCell>(savedOccupiedCells);
        ItemPlacementValidator.OccupyCells(occupiedCells, gameObject);
        savedOccupiedCells.Clear();
    }

    // Возвращает центр блока ячеек, которые занимает предмет
    public bool TryGetOccupiedCellsCenter(out Vector3 center)
    {
        center = Vector3.zero;
        if (occupiedCells == null || occupiedCells.Count == 0) return false;

        bool hasCell = false;
        float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

        foreach (InventoryCell cell in occupiedCells)
        {
            if (cell == null) continue;

            // В неактивной сетке localPosition ячеек ещё нулевой — считаем по сетке.
            Vector3 cellPosition = cell.transform.localPosition;
            InitializeGrid grid = InitializeGrid.FindInParents(cell.transform);
            if (grid != null) cellPosition = grid.GetCellLocalPosition(cell.gridSize);

            if (!hasCell)
            {
                minX = maxX = cellPosition.x;
                minY = maxY = cellPosition.y;
                hasCell = true;
            }
            else
            {
                if (cellPosition.x < minX) minX = cellPosition.x;
                if (cellPosition.x > maxX) maxX = cellPosition.x;
                if (cellPosition.y < minY) minY = cellPosition.y;
                if (cellPosition.y > maxY) maxY = cellPosition.y;
            }
        }

        if (!hasCell) return false;

        center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        return true;
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
        clone.WorldSourceObjects.Clear();

        // Исходный стак уменьшается на отделенное количество
        sourceState.currentStack -= amount;
        UpdateStackUI();

        clone.Init();
        return clone;
    }

    private void RotateGridCells()
    {
        if (itemGrid == null || itemState == null || itemState.data == null) return;

        int W = itemState.data.size.x;
        int H = itemState.data.size.y;

        // Проходимся по всем ячейкам по порядку их создания в InitializeGrid
        for (int i = 0; i < itemGrid.listGrid.Count; i++)
        {
            InventoryCell cell = itemGrid.listGrid[i].GetComponent<InventoryCell>();
            if (cell == null) continue;

            // Изначальные координаты ячейки (до любого поворота)
            int baseX = i / H;
            int baseY = i % H;

            if (isRotated)
            {
                // Поворот на 90 градусов
                cell.gridSize = new Vector2Int(baseY, W - 1 - baseX);
            }
            else
            {
                // Возврат в исходное состояние
                cell.gridSize = new Vector2Int(baseX, baseY);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (itemView != null && itemState != null && itemState.data != null)
        {
            // Здесь можно обновить иконку, если нужно
            // itemView.UpdateIcon(itemState.data.icon);
        }
    }

    // Метод для получения данных состояния (для экипировки)
    public ItemStateData GetStateData()
    {
        return itemState != null ? itemState.GetStateData() : default;
    }

    private void OnDestroy()
    {
        FreeOccupiedCells();
    }
}
