using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlotsManager : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private List<EquipmentSlotData> slotDatas;

    private void Awake()
    {
        foreach (var data in slotDatas)
        {
            GameObject slotObj = Instantiate(slotPrefab, transform);
            EquipmentSlot slot = slotObj.GetComponent<EquipmentSlot>();

            if (slot != null && data != null)
                slot.Setup(data);
        }
    }
}