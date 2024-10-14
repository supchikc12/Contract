using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    public GameObject _item;
    public int _index = 0;
    private Vector4 DefaltColor;

    private void Awake()
    {
        Image image = gameObject.GetComponent<Image>();
        DefaltColor = image.color;
        BoxCollider2D boxCollider2D = GetComponent<BoxCollider2D>();
        RectTransform rectTransform = GetComponent<RectTransform>();
        boxCollider2D.size = new Vector2( rectTransform.sizeDelta.x - 0.5f, rectTransform.sizeDelta.y - 0.5f);
        boxCollider2D.offset = new Vector2 (rectTransform.sizeDelta.x/2, -rectTransform.sizeDelta.y/2);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Weapon" || collision.tag == "Item")
        {
            Vector4 Color = new Vector4(253f, 197f, 197f);
            GetComponent<Image>().color = new Color(1,0,0,1);
            //gameObject.GetComponent<Image>().color = Color;
            _item = collision.gameObject;
        }
            
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _item = null;
        GetComponent<Image>().color = DefaltColor;
    }
}
