using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ItemDropController : MonoBehaviour
{
    public InventoryController inventory;
    public EquipmentController equipment;
    public PlayerGatherAnimation actions;
    public Tilemap ground;
    public Material pickupMaterial;

    public bool Drop(int index, int amount)
    {
        if (!inventory || inventory.ManagementLocked || (actions && actions.Busy)) return false;
        var slot = inventory.GetSlot(index);
        if (slot == null || slot.Empty || amount <= 0 || amount > slot.Quantity) return false;
        var item = slot.Item;
        if (!item.Droppable)
        {
            inventory.Notify("CANNOT DROP THIS ITEM");
            return false;
        }
        if (!ground || !item.Icon)
        {
            inventory.Notify("DROP UNAVAILABLE");
            Debug.LogError("Item drop requires a ground Tilemap and item icon.", this);
            return false;
        }
        Vector3 position = default;
        bool found = false;
        for (int attempt = 0; attempt < 48; attempt++)
        {
            float angle = attempt * 2.4f;
            position = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (.8f + attempt * .025f);
            if (!ground.HasTile(ground.WorldToCell(position)) || Physics2D.OverlapCircle(position, .25f)) continue;
            found = true;
            break;
        }
        if (!found) { inventory.Notify("NO CLEAR SPACE TO DROP"); return false; }
        GameObject drop = null;
        try
        {
            drop = new GameObject("Dropped " + item.DisplayName);
            drop.SetActive(false);
            drop.transform.position = position;
            var renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = item.Icon;
            renderer.color = item.IconTint;
            renderer.sharedMaterial = pickupMaterial;
            renderer.sortingLayerName = "Objects";
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            var pickup = drop.AddComponent<ResourcePickup>();
            pickup.type = item.Resource;
            pickup.item = item;
            pickup.quantity = amount;
        }
        catch (System.Exception exception)
        {
            if (drop) Destroy(drop);
            Debug.LogException(exception, this);
            return false;
        }
        if (!inventory.RemoveFromSlot(index, amount)) { Destroy(drop); return false; }
        drop.SetActive(true);
        inventory.Notify("DROPPED " + amount + " " + item.DisplayName.ToUpperInvariant());
        return true;
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.gKey.wasPressedThisFrame && equipment && equipment.Selected >= 0 &&
            !PlayerInteractionController.InputBlocked && !EquipmentController.Typing)
            Drop(InventoryController.MainSlots + equipment.Selected, 1);
    }
}

