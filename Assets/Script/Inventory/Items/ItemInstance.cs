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
    public Transform GetSavedParent() => savedParent;
    [SerializeField] private List<InventoryCell> occupiedCells = new List<InventoryCell>();

    [Header("Связь с миром")]
    public GameObject WorldSourceObject;

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
        // Инициализация визуала
        if (itemState != null && itemState.data != null)
        {
            UpdateVisuals();
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
        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        HandleEndDrag(target);
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

        // Сохраняем текущее состояние поворота
        StartRotated = isRotated;

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
        InventoryGrid grid = targetCell.GetComponentInParent<InventoryGrid>();
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

        //RequestRotate(); // Вернем к исходному повороту
        //Возвращаем поворот, если он был изменен
       
        if (isRotated != StartRotated)
        {
            RequestRotate(); // Вернем к исходному повороту
        }

        // Визуальный эффект тряски и возврат
        if (itemView != null)
        {
            itemView.PlayShakeEffect(() =>
            {
                transform.SetParent(startParent, false);
                transform.localPosition = startPosition;
                itemView.EndDragVisuals();
            });
        }
        else
        {
            transform.SetParent(startParent);
            if (itemGrid.listGrid.Count > 1)
            
            transform.localPosition = startPosition;

            if (itemGrid.listGrid.Count > 1)
            {
                if (isRotated)
                    transform.position = new Vector3(transform.position.x, transform.position.y + 25f);
                else
                    transform.position = new Vector3(transform.position.x - 25f, transform.position.y);
            }

            //OccupyCellsAtCurrentPosition();
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

        // Вычисляем целевую позицию
        Vector3 targetLocalPos = savedParent.InverseTransformPoint(placementData.targetCell.transform.position);

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
    }

    private void FreeOccupiedCells()
    {
        if (occupiedCells != null && occupiedCells.Count > 0)
        {
            ItemPlacementValidator.FreeCells(occupiedCells);
            occupiedCells.Clear();
        }
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
