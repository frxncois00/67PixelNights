using System;
using UnityEngine;

// Slots are read-only to callers. Only InventoryController changes their contents.
[Serializable]
public class InventorySlot
{
    [SerializeField] ItemDefinition item;
    [SerializeField] int quantity;
    public ItemDefinition Item => item;
    public int Quantity => quantity;
    public bool Empty => !item || quantity <= 0;
    internal void Set(ItemDefinition value, int count)
    {
        item = count > 0 ? value : null;
        quantity = item ? count : 0;
    }
}
