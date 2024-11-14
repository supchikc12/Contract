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
    public List<_Grid> _last_pos;

    private void Awake()
    {
        ray = GameObject.Find("Canvas").GetComponent<GraphicRaycaster>();
        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        inventory = GetComponent<ItemInventory>();
        inventoryController = GameObject.Find("Inventory").GetComponent<InventoryController>();
        _last_pos = new List<_Grid>();
        Debug.Log("dsflkgjsdlhgkjlda");
    }

   
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_last_pos.Count == 0)
        {

            _last_pos = inventory._isGrid;
        }
        //Debug.Log(_last_pos.Count);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //if (Input.GetAxis("Mouse X") > 0.2f)
        //{
        //    transform.position = new Vector2(transform.position.x + 25f, transform.position.y);
        //}
        //else if (Input.GetAxis("Mouse X") < -0.2f)
        //{
        //    transform.position = new Vector2(transform.position.x - 25f, transform.position.y);
        //}
        //if (Input.GetAxis("Mouse Y") > 0.2f)
        //{
        //    transform.position = new Vector2(transform.position.x, transform.position.y + 25f);
        //}
        //else if (Input.GetAxis("Mouse Y") < -0.2f)
        //{
        //    transform.position = new Vector2(transform.position.x, transform.position.y - 25f);
        //}

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

        foreach (RaycastResult result in results)
        {
            //Debug.Log(result);
            if (result.gameObject.tag == "Slot")
            {
                _Slot _slot = result.gameObject.GetComponent<_Slot>();
                //Debug.Log(gameObject);
                Debug.Log("Попал в слот");
                _slot.ChangeInSlot(gameObject);
                break;
            }
                
        }
        //if (results[0].gameObject.tag == "Slot")
        //{
        //    _Slot _slot = results[0].gameObject.GetComponent<_Slot>();
        //    //Debug.Log(gameObject);
        //    Debug.Log("Попал в слот");
        //    _slot.ChangeInSlot(gameObject);
        //}
        //else 
        //    results.Remove(results[0]);

        if (results.Count > 0)
        {
            if (inventory._is_gameobject.Count == 0)
            {
                _Grid grid = results[0].gameObject.GetComponent<_Grid>();
                inventory.Remove_isGrid(grid);


                inventoryController.MoveItemOnGrid(inventory._isGrid, gameObject);
                List<_Grid> _grid = inventory._isGrid;
                SortingCells(_grid);
                inventoryController._itemContainer = null;
            }
            else if (inventory._is_gameobject.Count > 0)
            {
                if (gameObject.tag == "Bullet" && inventory._is_gameobject[0].tag == "Magazin")
                {
                    //Debug.Log(inventory._is_gameobject[0]);
                    UseInInventory item = GetComponent<UseInInventory>();
                    item.Use(inventory._is_gameobject[0]);
                }

                //Debug.Log(results[0].gameObject.name);
            }
        }
        else
        {
            
            //Debug.Log(_last_pos.Count);
            inventoryController.MoveItemOnGrid(_last_pos, gameObject);
        }


    }

    //Перебор листа с ячейками
    //List<_Grid> grid - Лист с ячейками которые занимает обьект на настоящий момент
    public void SortingCells(List<_Grid> grid)
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