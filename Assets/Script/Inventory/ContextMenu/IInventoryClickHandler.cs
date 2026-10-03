using UnityEngine;

public interface IInventoryClickHandler
{
    void HandleLeftClick();
    void HandleRightClick();
    void HandleBeginDrag();
    void HandleDrag();
    void HandleEndDrag(GameObject target);
}
