using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public GameObject _backPack;
    public Transform _transformBackPack;
    public InventoryObject _backPackInventory;
    public Item _item;

    private void Update()
    {
        if (_backPack != null)
            _backPack.transform.position = _transformBackPack.position;
    }
    public void TakeItem(RaycastHit hit)
    {
        Item _itemTake = hit.collider.GetComponent<Item>();
        
        if (_backPack != null)
        {
            _backPackInventory = _backPack.GetComponent<InventoryObject>();
            if ( (_backPackInventory._item.Count + _itemTake._slot) <= _backPackInventory._maxSlots)
            {
                for (int i = 0; i < _itemTake._slot; i++)
                {
                    _backPackInventory._item.Add (_itemTake._id);
                    Destroy(hit.collider.gameObject);
                }
            }
                
        }
    }
}
