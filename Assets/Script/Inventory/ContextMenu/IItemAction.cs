using UnityEngine;

public interface IItemAction
{
    string ActionName { get; }
    bool CanExecute(ItemInstance item);
    void Execute(ItemInstance item);
}