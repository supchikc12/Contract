using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseInInventory: MonoBehaviour
{
    //private GameObject _this;
    public void Use(GameObject User)
    {

        if (gameObject.tag == "Eat")
        {
            Characteristic characteristic = User.GetComponent<Characteristic>();
            //characteristic._hunger += NutritionalValue;
        }
        else if (User.tag == "Magazin" && gameObject.tag == "Bullet")
        {
            LoadAmmoInMagazin(User);
        }
        Debug.Log("Использовал");

        ItemInventory _item_inventory = GetComponent<ItemInventory>();
        Debug.Log(_item_inventory._is_gameobject);
    }

    public void LoadAmmoInMagazin(GameObject Magazin)
    {
        AmmunitionMagazine ammunitionMagazine = Magazin.GetComponent<AmmunitionMagazine>();
        if (ammunitionMagazine.Ammunition.Count < ammunitionMagazine._maxAmmo)
        {
            Item bullet = GetComponent<Item>();

            for (int i = 0; i < ammunitionMagazine._maxAmmo; i++)
            {
                if (bullet._stac > 0)
                {
                    ammunitionMagazine.Ammunition.Add(bullet._gameobject_prefab);
                    bullet._stac--;
                    Debug.Log(ammunitionMagazine.Ammunition.Count);
                }
                else
                    break;
            }

            
             
           
        }
    }



}
