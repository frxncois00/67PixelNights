using UnityEngine;
using UnityEngine.InputSystem;

public class FoodConsumption : MonoBehaviour
{
    public InventoryController inventory;
    public PlayerHealth health;
    public PlayerHunger hunger;
    public EquipmentController equipment;
    public PlayerGatherAnimation actions;
    bool consuming;
    float nextUse;
    void Start()
    {
        if (inventory && health && hunger && equipment && actions) return;
        Debug.LogError("FoodConsumption is missing inventory, survival or player references.", this);
        enabled = false;
    }
    public bool Consume(int index, bool fromInventory = false)
    {
        if (!isActiveAndEnabled || !inventory || !health || !hunger || health.IsDead || consuming ||
            Time.time < nextUse || (actions && actions.Busy)) return false;
        if (fromInventory ? !inventory.MenuOpen : PlayerInteractionController.InputBlocked) return false;
        var slot = inventory.GetSlot(index);
        if (slot == null || slot.Empty) return false;
        var item = slot.Item;
        if (!item.Consumable)
        {
            inventory.Notify("THIS ITEM CANNOT BE EATEN");
            return false;
        }
        if ((hunger.CurrentHunger >= hunger.MaxHunger || item.HungerRestored <= 0) &&
            (health.CurrentHealth >= health.MaxHealth || item.HealthRestored <= 0))
        {
            inventory.Notify("ALREADY FULL");
            return false;
        }
        consuming = true;
        try
        {
            if (!inventory.RemoveFromSlot(index, 1)) return false;
            float fed = hunger.Restore(item.HungerRestored);
            float healed = health.Heal(item.HealthRestored);
            if (fromInventory)
            {
                var survival = GetComponent<PlayerSurvivalController>();
                if (survival && survival.inventoryUI) survival.inventoryUI.Close();
            }
            actions.PlayEat(item);
            nextUse = Time.time + .2f;
            inventory.Notify("ATE " + item.DisplayName.ToUpperInvariant() +
                (fed > 0 ? "\nHUNGER +" + fed.ToString("0.#") : "") +
                (healed > 0 ? "  HEALTH +" + healed.ToString("0.#") : ""));
            return true;
        }
        finally { consuming = false; }
    }
    void Update()
    {
        var key = Keyboard.current;
        if (key != null && key.fKey.wasPressedThisFrame && equipment && equipment.Selected >= 0 && !EquipmentController.Typing)
            Consume(InventoryController.MainSlots + equipment.Selected);
    }
}

