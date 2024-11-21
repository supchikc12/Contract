using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    public GameObject _backPack;
    public InventoryGrid _inventoryGrid;
    public GameObject _item;
    public GameObject _mouseItem;
    public List<_Grid> _gridList = new List<_Grid>();
    public List<RectTransform> _cellList = new List<RectTransform>();
    public GameObject _itemContainer;

    private void Awake()
    {
        _inventoryGrid = _backPack.GetComponent<InventoryGrid>();

    }
    private void Update()
    {
        if (_item != null)
        {

            IsGridBlank();
            
            if (_gridList != null)
            {
                GameObject _itemObj = Instantiate(_item, gameObject.transform/*_backPack.transform*/);
                MoveItemOnGrid(_gridList, _itemObj);
                _itemObj.transform.position = new Vector3(_itemObj.transform.position.x + 250, _itemObj.transform.position.y - 100);
                _gridList.Clear();
                _item = null;
            }

            //BoxCollider2D boxColider = _item.GetComponent<BoxCollider2D>();
            //ItemInventory _itemTriger = _item.GetComponent<ItemInventory>();

            //RectTransform _transform = _item.GetComponent<RectTransform>();
            //_transform.anchoredPosition = _cellList[0].anchoredPosition;
            //_cellList.Clear();

            //if (_itemTriger.IsTriger == true)
            //{
            //    Debug.Log("перемещение");
            //    _item.transform.position = new Vector2(+25, 0);
            //}

            

        }
        
        if (_itemContainer != null)
            if (Input.GetKeyDown(KeyCode.R))
                RotateItem(_itemContainer);


    }

    private void IsGridBlank()
    {
        ItemInventory item = _item.GetComponent<ItemInventory>();
        int j = 0;
        for (int i = 0; i <= _inventoryGrid._grid.Count - 1; i++)
        {
            

            if (j > 19)
                j = 0;
            _Grid grid = _inventoryGrid._grid[i].GetComponent<_Grid>();
            RectTransform cell = _inventoryGrid._grid[i];
            if (grid._item == null)
            {               
                for (int y = 0; y < item.Y; y++)
                {
                    int k = y * _inventoryGrid.X + j + i;

                    for (int x = 0; x < item.X; x++)
                    {
                        grid = _inventoryGrid._grid[k].GetComponent<_Grid>();

                        _gridList.Add(grid);
                        _cellList.Add(cell);
                        k++;
                        if (grid._item != null)
                        {
                            _gridList.Clear();
                            //_cellList.Clear();
                            break;
                        }

                    }
                }
                break;
            }
            j++;
        }
    }

    public void MoveItemOnGrid(List<_Grid> _gridList, GameObject _itemObj)
    {       
        _itemObj.transform.position = new Vector3(0, 0, 0);
        ItemInventory itemInventory = _itemObj.GetComponent<ItemInventory>();
        //Debug.Log(_gridList[0]);
        _itemObj.transform.position = new Vector3 (_gridList[0].transform.position.x, _gridList[0].transform.position.y,0);
        Drag dragDebug = _itemObj.GetComponent<Drag>();
        ItemInventory inventory = _itemObj.GetComponent<ItemInventory>();
        
        //dragDebug.SortingCells(inventory._isGrid);
        //int _next_posItem = (_gridList.Count / 2) + (itemInventory.X / 2) + itemInventory.X;
        //Debug.Log(_next_posItem);
        //_itemObj.transform.position = new Vector3(_gridList[_next_posItem].transform.position.x, _gridList[_next_posItem].transform.position.y,0);
    }
    private void RotateItem(GameObject Item)
    {
        _gridList.Clear();
        _cellList.Clear();
        Item.transform.Rotate(0, 0, 90f);
    }

}



