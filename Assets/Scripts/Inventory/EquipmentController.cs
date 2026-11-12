using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipmentController : MonoBehaviour
{
    public InventoryController inventory;
    public PlayerGatherAnimation actions;
    public int Selected { get; private set; } = -1;
    public ItemDefinition Equipped => inventory && Selected >= 0 ? inventory.GetSlot(InventoryController.MainSlots + Selected).Item : null;
    public bool AxeEquipped => Equipped && Equipped.Equippable && Equipped.Id == "basic_axe";
    public bool PickaxeEquipped => Equipped && Equipped.Equippable && Equipped.Id == "basic_pickaxe";
    public bool SwordEquipped => Equipped && Equipped.Equippable && (Equipped.Id == "basic_sword" || Equipped.craftedWeapon);
    public bool CanHarvest(ResourceType type) => type == ResourceType.Wood ? AxeEquipped : type == ResourceType.Stone && PickaxeEquipped;
    public event Action Changed;
    public static bool Typing => EventSystem.current && EventSystem.current.currentSelectedGameObject &&
        EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
    void OnEnable() { if (inventory) inventory.Changed += Refresh; }
    void OnDisable() { if (inventory) inventory.Changed -= Refresh; }
    void Refresh() => Changed?.Invoke();
    public bool Select(int index)
    {
        if (!inventory || inventory.ManagementLocked || index < 0 || index >= InventoryController.HotbarSlots || (actions && actions.Busy)) return false;
        if (index == Selected) return true;
        Selected = index;
        Changed?.Invoke();
        inventory.Notify(Equipped && Equipped.Equippable ? Equipped.DisplayName.ToUpperInvariant() + " EQUIPPED" : "TOOL UNEQUIPPED");
        return true;
    }
    public bool EquipFromBag(int index)
    {
        if (!inventory || inventory.ManagementLocked || (actions && actions.Busy)) return false;
        var slot = inventory.GetSlot(index);
        if (slot == null || slot.Empty) return false;
        if (index >= InventoryController.MainSlots) return Select(index - InventoryController.MainSlots);
        int destination = -1;
        if (destination < 0)
            for (int i = 0; i < InventoryController.HotbarSlots; i++)
                if (inventory.GetSlot(InventoryController.MainSlots + i).Empty) { destination = i; break; }
        if (destination < 0) destination = Mathf.Max(0, Selected);
        // Swap returns an occupied hotbar stack to the source bag slot, without loss.
        if (!inventory.Move(index, InventoryController.MainSlots + destination)) return false;
        return Select(destination);
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || TradingUI.IsOpen || BuildingController.ModalOpen || Typing || !inventory || inventory.MenuOpen) return;
        for (int i = 0; i < 5; i++)
            if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Toggle(i);
    }
    public bool Toggle(int index)
    {
        if (!inventory || inventory.ManagementLocked || index < 0 || index >= InventoryController.HotbarSlots || (actions && actions.Busy)) return false;
        if (Selected != index) return Select(index);
        Selected = -1;
        Changed?.Invoke();
        inventory.Notify("UNEQUIPPED");
        return true;
    }
}
