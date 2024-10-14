using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseInInventory: MonoBehaviour
{
    //private GameObject _this;
    public void Use(GameObject _using_item)
    {
        
        ItemInventory _item_inventory = GetComponent<ItemInventory>();
        Debug.Log(_item_inventory._is_gameobject);
    }

    

}
