using UnityEngine;

// Общие данные контейнера: рюкзак, разгрузка, ящик, сумка.
[CreateAssetMenu(fileName = "New Container", menuName = "Inventory/Container Data")]
public class ContainerData : EquippableData
{
    [Header("Сетка контейнера")]
    public ContainerGridData grid;
}
