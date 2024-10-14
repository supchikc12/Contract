using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    public bool IsTriger = false;
    public RectTransform _icon, _item;
    public int X, Y;
    public GameObject _ammo;
    public List<Grid> _isGrid = new List<Grid>();
    public List<GameObject> _is_gameobject = new List<GameObject>();

    private void Start()
    {

        RectTransform rectTransform = Resources.Load<RectTransform>("Prefab/Grid");
        _item = gameObject.GetComponent<RectTransform>();
        _item.sizeDelta = new Vector2(X * rectTransform.sizeDelta.x, Y * rectTransform.sizeDelta.y);

        BoxCollider2D boxCollider2D = GetComponent<BoxCollider2D>();
        rectTransform = GetComponent<RectTransform>();
        boxCollider2D.size = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
        boxCollider2D.offset = new Vector2(rectTransform.sizeDelta.x / 2, -rectTransform.sizeDelta.y / 2);

    }

    public void Remove_isGrid(Grid element)
    {
        _isGrid.Clear();
        _isGrid.Add(element);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag != "Grid")
            _is_gameobject.Add(collision.gameObject);
        
        Grid gameobject = collision.GetComponent<Grid>();
        _isGrid.Add(gameobject);          
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag != "Grid")
            _is_gameobject.Remove(collision.gameObject);

        Grid gameobject = collision.GetComponent<Grid>();
        _isGrid.Remove(gameobject);
    }   

}
