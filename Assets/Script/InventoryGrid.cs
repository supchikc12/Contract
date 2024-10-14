using UnityEngine;
using System.Collections.Generic;

public class InventoryGrid : MonoBehaviour
{
    public List<RectTransform> _grid;
    public  RectTransform _icon, _item;
    public int X,Y;

    private void Awake()
    {
        RectTransform cell = Resources.Load<RectTransform>("Prefab/Grid");
        _grid.Add(Instantiate(cell, gameObject.transform));
        RectTransform _transform = _grid[_grid.Count - 1].GetComponent<RectTransform>();
        if (_icon != null)
            _icon.sizeDelta = new Vector2(X * _transform.sizeDelta.x, Y * _transform.sizeDelta.y);
        _item = gameObject.GetComponent<RectTransform>();
        _item.sizeDelta = new Vector2(X * _transform.sizeDelta.x, Y * _transform.sizeDelta.y);
        for (int i = 1; i <= Y; i++)
        {            
            for (int j = 1; j <= X; j++)
            {                
                _grid.Add(Instantiate(cell, gameObject.transform));
                Grid _id = _grid[_grid.Count - 1].GetComponent<Grid>();
                Grid _id_previous = _grid[_grid.Count - 2].GetComponent<Grid>();
                int previous = _id_previous._index;
                _id._index = previous + 1;
                _transform = _grid[_grid.Count - 1].GetComponent<RectTransform>();
                _transform.anchoredPosition = _grid[_grid.Count - 2].anchoredPosition;
                _transform.anchoredPosition += new Vector2(_transform.sizeDelta.x, 0);
                
            }
            RectTransform _transformY = _grid[_grid.Count - 1].GetComponent<RectTransform>();
            _transformY.anchoredPosition = _grid[_grid.Count - X -1].anchoredPosition;            
            _transformY.anchoredPosition += new Vector2(0, -_transformY.sizeDelta.y);            
        }
        Destroy(_grid[_grid.Count - 1].gameObject);
        _grid.Remove(_grid[_grid.Count - 1]);
    }
}
