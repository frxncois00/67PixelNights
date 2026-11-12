using UnityEngine;

public class StructureStorage : MonoBehaviour
{
    [SerializeField]
    InventorySlot[] slots = new InventorySlot[12];
    public bool Empty
    {
        get
        {
            Ensure();
            foreach (var s in slots)
                if (!s.Empty)
                    return false;
            return true;
        }
    }

    void Ensure()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                slots[i] = new InventorySlot();
    }

    public InventorySlot GetSlot(int i)
    {
        Ensure();
        return i >= 0 && i < slots.Length ? slots[i] : null;
    }

    public int Deposit(InventoryController inv, int index, int amount)
    {
        Ensure();
        var source = inv.GetSlot(index);
        if (inv.ManagementLocked || source == null || source.Empty || amount <= 0 || !source.Item.Droppable)
            return 0;
        var item = source.Item;
        int capacity = 0;
        foreach (var s in slots)
            if (s.Empty)
                capacity += item.MaximumStack;
            else if (s.Item == item)
                capacity += item.MaximumStack - s.Quantity;
        int accepted = Mathf.Min(amount, Mathf.Min(source.Quantity, capacity));
        if (accepted == 0)
            return 0;
        int left = accepted;
        // Container mutations precede inventory's notification; both states are then final.
        for (int pass = 0; pass < 2; pass++)
            foreach (var s in slots)
            {
                if (left == 0)
                    break;
                if (pass == 0 ? s.Empty || s.Item != item : !s.Empty)
                    continue;
                int n = Mathf.Min(left, item.MaximumStack - s.Quantity);
                s.Set(item, s.Quantity + n);
                left -= n;
            }

        inv.RemoveFromSlot(index, accepted);
        return accepted;
    }

    public int Withdraw(InventoryController inv, int index, int amount)
    {
        Ensure();
        var source = GetSlot(index);
        if (inv.ManagementLocked || source == null || source.Empty || amount <= 0)
            return 0;
        var item = source.Item;
        int accepted = Mathf.Min(amount, Mathf.Min(source.Quantity, inv.Capacity(item)));
        if (accepted <= 0)
            return 0;
        source.Set(item, source.Quantity - accepted);
        return inv.Add(item, accepted);
    }
}
