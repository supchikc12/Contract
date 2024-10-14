using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class Drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private PointerEventData pt;
    private GraphicRaycaster ray;
    private EventSystem eventSystem;
    private InventoryController inventoryController;
    private ItemInventory inventory;

    private void Awake()
    {
        ray = GameObject.Find("Canvas").GetComponent<GraphicRaycaster>();
        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        inventory = GetComponent<ItemInventory>();
        inventoryController = GameObject.Find("Inventory").GetComponent<InventoryController>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;

        inventoryController._itemContainer = transform.gameObject;
    }

    public void OnEndDrag(PointerEventData eventData)
    {

        pt = new PointerEventData(eventSystem);
        pt.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        LayerMask mask = LayerMask.GetMask("UI Inventory");
        ray.Raycast(pt, results);
        
        results.Remove(results[0]);
        
        if (inventory._is_gameobject.Count == 0 )
        {
            _Grid grid = results[0].gameObject.GetComponent<_Grid>();
            inventory.Remove_isGrid(grid);

            Debug.Log(inventory._isGrid[0]._index);
            inventoryController.MoveItemOnGrid(inventory._isGrid, gameObject);
            List<_Grid> _grid = inventory._isGrid;
            SortingCells(_grid);
            inventoryController._itemContainer = null;
        }
        else if (results[0].gameObject.tag == "Magazin" && gameObject.tag == "Bullet")
        {
            Debug.Log(results[0].gameObject);
            UseInInventory item = results[0].gameObject.GetComponent<UseInInventory>();
            item.Use(results[0].gameObject);
            Debug.Log(results[0].gameObject.name);
        }
        //if (results[0].gameObject.tag == "Grid")
        //{

        //}


    }

    //ѕеребор листа с €чейками
    //List<Grid> grid - Ћист с €чейками которые занимает обьект на насто€щий момент
    public void SortingCells (List<_Grid> grid)
    {
        if (grid.Count > 0)
        {
            for (int i = 0; i < grid.Count - 1; i++)
            {
                _Grid _grid = grid[i]/*.GetComponent<Grid>()*/;
                _Grid _next_grid = grid[i + 1]/*.GetComponent<Grid>()*/;
                //Debug.Log(_next_grid._index < grid._index + _next_grid._index + grid._index);
                if (_next_grid._index < _grid._index)
                {
                    grid[i] = grid[i + 1];
                    grid[i + 1] = grid[i];
                }
            }

            for (int i = 0; i < grid.Count - 1; i++)
            {
                _Grid _grid = grid[i]/*.GetComponent<Grid>()*/;

                //Debug.Log(_grid._index);
            }
        }
    }
}