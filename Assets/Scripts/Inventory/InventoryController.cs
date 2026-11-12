using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class InventoryController : MonoBehaviour
{
    public const int MainSlots = 20, HotbarSlots = 5, SlotCount = 25;
    [SerializeField] ItemDefinition[] definitions;
    [SerializeField] ItemDefinition startingTool;
    // Unity must retain runtime stacks across a script/domain reload during Play Mode.
    [SerializeField, HideInInspector] InventorySlot[] slots = new InventorySlot[SlotCount];
    bool initialized;
    public event Action Changed;
    public event Action<string> Feedback;
    public bool MenuOpen { get; private set; }
    public bool ManagementLocked { get; private set; }
    public void LockManagement() { ManagementLocked = true; MenuOpen = false; }
    public ItemDefinition FindItem(string id)
    {
        if (definitions != null) foreach (var item in definitions) if (item && item.Id == id) return item;
        return null;
    }

    void Awake() => Initialize();
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot();
        if (Known(startingTool) && startingTool.Equippable) slots[MainSlots].Set(startingTool, 1);
        else if (startingTool) Debug.LogError("Inventory starting tool must be registered and equippable.", this);
    }
    bool Valid(int index) => index >= 0 && index < SlotCount;
    bool Known(ItemDefinition item)
    {
        if (!item || string.IsNullOrWhiteSpace(item.Id) || definitions == null) return false;
        foreach (var definition in definitions) if (definition == item) return true;
        return false;
    }
    public InventorySlot GetSlot(int index) { Initialize(); return Valid(index) ? slots[index] : null; }
    public ItemDefinition ResourceItem(ResourceType type)
    {
        if (definitions == null) return null;
        foreach (var item in definitions)
            if (item && item.Category == ItemCategory.Resource && item.Resource == type) return item;
        return null;
    }
    public int Count(ItemDefinition item)
    {
        Initialize();
        if (!Known(item)) return 0;
        int total = 0;
        foreach (var slot in slots) if (slot.Item == item) total += slot.Quantity;
        return total;
    }
    public int Capacity(ItemDefinition item)
    {
        Initialize();
        if (!Known(item)) return 0;
        int capacity = 0;
        foreach (var slot in slots)
            if (slot.Empty) capacity += item.MaximumStack;
            else if (slot.Item == item) capacity += item.MaximumStack - slot.Quantity;
        return capacity;
    }
    public int Add(ItemDefinition item, int amount)
    {
        Initialize();
        if (ManagementLocked || !Known(item) || amount <= 0) return 0;
        int accepted = Math.Min(amount, Capacity(item)), remaining = accepted;
        // Merge stacks first, then fill hotbar slots before bag slots.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int order = 0; order < SlotCount; order++)
            {
                var slot = slots[(MainSlots + order) % SlotCount];
                if (remaining == 0) break;
                if (pass == 0 ? slot.Empty || slot.Item != item : !slot.Empty) continue;
                int add = Math.Min(remaining, item.MaximumStack - slot.Quantity);
                slot.Set(item, slot.Quantity + add);
                remaining -= add;
            }
        }
        if (accepted > 0) Changed?.Invoke();
        return accepted;
    }
    public bool TryAdd(ItemDefinition item, int amount)
    {
        if (amount <= 0 || Capacity(item) < amount) return false;
        return Add(item, amount) == amount;
    }
    public bool Remove(ItemDefinition item, int amount)
    {
        if (ManagementLocked || !Known(item) || amount < 0 || Count(item) < amount) return false;
        if (amount == 0) return true;
        int remaining = amount;
        foreach (var slot in slots)
        {
            if (slot.Item != item) continue;
            int take = Math.Min(remaining, slot.Quantity);
            slot.Set(item, slot.Quantity - take);
            remaining -= take;
            if (remaining == 0) break;
        }
        Changed?.Invoke();
        return true;
    }
    public bool RemoveFromSlot(int index, int amount)
    {
        var slot = GetSlot(index);
        if (ManagementLocked || slot == null || slot.Empty || amount <= 0 || amount > slot.Quantity) return false;
        slot.Set(slot.Item, slot.Quantity - amount);
        Changed?.Invoke();
        return true;
    }
    public bool Move(int from, int to)
    {
        var source = GetSlot(from); var destination = GetSlot(to);
        if (ManagementLocked || source == null || destination == null || from == to || source.Empty) return false;
        if (source.Item == destination.Item && source.Item.Stackable)
        {
            int moved = Math.Min(source.Quantity, source.Item.MaximumStack - destination.Quantity);
            if (moved <= 0) return false;
            destination.Set(source.Item, destination.Quantity + moved);
            source.Set(source.Item, source.Quantity - moved);
        }
        else
        {
            var item = destination.Item; int count = destination.Quantity;
            destination.Set(source.Item, source.Quantity);
            source.Set(item, count);
        }
        Changed?.Invoke();
        return true;
    }
    public bool Split(int from)
    {
        var source = GetSlot(from);
        if (ManagementLocked || source == null || source.Empty || source.Quantity < 2) return false;
        foreach (var destination in slots)
        {
            if (!destination.Empty) continue;
            int amount = source.Quantity / 2;
            destination.Set(source.Item, amount);
            source.Set(source.Item, source.Quantity - amount);
            Changed?.Invoke();
            return true;
        }
        Notify("INVENTORY FULL!");
        return false;
    }
    // Prefer an ingredient stack of one so its vacated slot can hold the output.
    int IngredientSlot(ItemDefinition ingredient)
    {
        int found = -1;
        for (int i = 0; i < SlotCount; i++)
            if (slots[i].Item == ingredient && slots[i].Quantity > 0)
            { found = i; if (slots[i].Quantity == 1) break; }
        return found;
    }
    public bool CanExchangeOne(ItemDefinition ingredient, ItemDefinition output)
    {
        Initialize();
        if (ManagementLocked || !Known(ingredient) || !Known(output) || ingredient == output) return false;
        int index = IngredientSlot(ingredient);
        return index >= 0 && (Capacity(output) > 0 || slots[index].Quantity == 1);
    }
    public bool ExchangeOne(ItemDefinition ingredient, ItemDefinition output)
    {
        if (!CanExchangeOne(ingredient, output)) return false;
        int index = IngredientSlot(ingredient);
        slots[index].Set(ingredient, slots[index].Quantity - 1);
        InventorySlot destination = null;
        foreach (var slot in slots)
            if (slot.Item == output && slot.Quantity < output.MaximumStack) { destination = slot; break; }
        if (destination == null) foreach (var slot in slots) if (slot.Empty) { destination = slot; break; }
        destination.Set(output, destination.Quantity + 1);
        Changed?.Invoke();
        return true;
    }
    // Simulate the complete recipe in copies; publish one change only after commit.
    public bool CraftBatch(ItemAmount[] ingredients, ItemDefinition output, int amount, bool commit = true)
    {
        Initialize();
        if (ManagementLocked || ingredients == null || ingredients.Length == 0 || !Known(output) || amount <= 0 || amount > 9999) return false;
        var copy = new InventorySlot[SlotCount];
        for (int i=0;i<SlotCount;i++) { copy[i]=new InventorySlot(); copy[i].Set(slots[i].Item,slots[i].Quantity); }
        foreach(var cost in ingredients) {
            if(cost == null || !Known(cost.item) || cost.amount<=0 || cost.amount>9999) return false;
            int left=cost.amount;
            // Empty smaller stacks first to make room for the crafted output.
            while(left>0) {
                InventorySlot smallest=null;
                foreach(var slot in copy) if(!slot.Empty && slot.Item==cost.item && (smallest==null || slot.Quantity<smallest.Quantity)) smallest=slot;
                if(smallest==null)break;
                int take=Math.Min(left,smallest.Quantity);
                smallest.Set(smallest.Item,smallest.Quantity-take);
                left-=take;
            }
            if(left>0)return false;
        }
        int remaining=amount;
        for(int pass=0;pass<2;pass++) foreach(var slot in copy) {
            if(remaining==0)break;
            if(pass==0 ? slot.Empty || slot.Item!=output : !slot.Empty)continue;
            int add=Math.Min(remaining,output.MaximumStack-slot.Quantity); slot.Set(output,slot.Quantity+add); remaining-=add;
        }
        if(remaining>0)return false;
        if(commit) { for(int i=0;i<SlotCount;i++)slots[i].Set(copy[i].Item,copy[i].Quantity);Changed?.Invoke(); }
        return true;
    }
    public void SetMenuOpen(bool value) => MenuOpen = value && !ManagementLocked;
    public void Notify(string message) => Feedback?.Invoke(message);
    void OnDisable() => MenuOpen = false;
}
