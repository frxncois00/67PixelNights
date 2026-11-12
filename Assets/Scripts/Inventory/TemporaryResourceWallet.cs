using System;
using UnityEngine;

public enum ResourceType { Wood, Stone, Scrap }
public interface IWoodWallet { int Wood { get; } bool TrySpend(int amount); }

// Compatibility facade. All quantities live exclusively in InventoryController.
public class TemporaryResourceWallet : MonoBehaviour, IWoodWallet
{
    public InventoryResourceAdapter adapter;
    [SerializeField, Min(0)] int startingWood, startingStone, startingScrap;
    public int Wood => GetResourceCount(ResourceType.Wood);
    public event Action Changed;
    void OnEnable()
    {
        if (!adapter) adapter = GetComponent<InventoryResourceAdapter>();
        if (!adapter || !adapter.inventory)
        {
            Debug.LogError("Resource wallet needs an adapter with an inventory.", this);
            return;
        }
        adapter.inventory.Changed += Refresh;
    }
    void Start()
    {
        Migrate(ResourceType.Wood, startingWood);
        Migrate(ResourceType.Stone, startingStone);
        Migrate(ResourceType.Scrap, startingScrap);
        Refresh();
    }
    void Migrate(ResourceType type, int amount)
    {
        if (amount > 0 && !TryAddResource(type, amount))
            Debug.LogError("Starting resources do not fit the inventory: " + type, this);
    }
    void OnDisable() { if (adapter && adapter.inventory) adapter.inventory.Changed -= Refresh; }
    void Refresh() => Changed?.Invoke();
    public int GetResourceCount(ResourceType type) => adapter ? adapter.Count(type) : 0;
    public int AddResource(ResourceType type, int amount) => adapter ? adapter.Add(type, amount) : 0;
    public bool TryAddResource(ResourceType type, int amount) => adapter && adapter.TryAdd(type, amount);
    public bool TryConsumeResource(ResourceType type, int amount) => adapter && adapter.Consume(type, amount);
    public void AddWood(int amount) => TryAddResource(ResourceType.Wood, amount);
    public bool TrySpend(int amount) => TryConsumeResource(ResourceType.Wood, amount);
    [ContextMenu("Development/Restore 30 Test Wood")]
    public void RestoreTestWood()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Wood < 30) TryAddResource(ResourceType.Wood, 30 - Wood);
#endif
    }
}
