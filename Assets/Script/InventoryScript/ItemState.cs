using System;
using UnityEngine;

public class ItemState : MonoBehaviour
{
    // —сылка на ассет с неизмен€емыми данными
    public ItemData data;

    // »змен€емые в рантайме пол€
    public int currentStack = 1;
    public float currentDurability = 100f;

    // ”никальный идентификатор экземпл€ра (можно устанавливать извне при создании)
    public string uniqueId;
}
