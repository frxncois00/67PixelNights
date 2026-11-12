using UnityEngine;

public class InventoryResourceAdapter : MonoBehaviour
{
    public InventoryController inventory;
    public int Count(ResourceType type) => inventory ? inventory.Count(inventory.ResourceItem(type)) : 0;
    public int Add(ResourceType type, int amount) => inventory ? inventory.Add(inventory.ResourceItem(type), amount) : 0;
    public bool TryAdd(ResourceType type, int amount) => inventory && inventory.TryAdd(inventory.ResourceItem(type), amount);
    public bool Consume(ResourceType type, int amount) => inventory && inventory.Remove(inventory.ResourceItem(type), amount);
}
