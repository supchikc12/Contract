using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Item : MonoBehaviour
{
    public GameObject _itemPrefabIcon;
    public GameObject _gameobject_prefab;
    public int _id;
    public string _itemName;
    public int _price;
    public Sprite icon;
    public int _stac;
    //public float NutritionalValue; //пищевая ценность для еды
    


    //Использование предмета
    //User - Обьект который вызывает функцию
    public void Use(GameObject User)
    {
    }

    


}


