using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public GameObject _this;

    private void OnTriggerEnter2D(Collider2D other)
    {
        _this = other.gameObject;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        _this = null;
    }
}
