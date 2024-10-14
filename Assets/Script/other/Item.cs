using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Item : MonoBehaviour
{
    public GameObject _itemPrefabIcon;
    public int _id;
    public string _itemName;
    public int _price;
    public Sprite icon;
    public int _stac = 30;
    public float NutritionalValue; //пищевая ценность для еды



    //Использование предмета
    //User - Обьект который вызывает функцию
    public void Use(GameObject User)
    {
        if (gameObject.tag == "Eat")
        {
            Characteristic characteristic = User.GetComponent<Characteristic>();
            characteristic._hunger += NutritionalValue;
        }
        else if (gameObject.tag == "Magazin")
        { 
        
        }
        Debug.Log("Использовал");
    }


}


