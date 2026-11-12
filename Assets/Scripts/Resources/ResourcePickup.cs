using UnityEngine;

public class ResourcePickup : InteractionTarget {
    public ResourceType type;
    public ItemDefinition item;
    [Min(1)] public int quantity = 1;
    bool collected;
    public override bool Available => !collected && quantity > 0;
    public override string Prompt => "[E] Pick up " + quantity + " " + (item ? item.DisplayName : type.ToString());
    public override void Use(PlayerInteractionController player) {
        if (PlayerInteractionController.InputBlocked || !Reachable(player.transform.position)) return;
        collected=true;
        var inventory = player.GetComponent<InventoryController>();
        int accepted = item ? (inventory ? inventory.Add(item, quantity) : 0) : player.wallet.AddResource(type, quantity);
        quantity -= accepted;
        if (accepted > 0) AudioManager.Instance?.PlayPickup();
        player.Notice(accepted > 0 ? "+" + accepted + " " + (item ? item.DisplayName : type.ToString()).ToUpperInvariant() : "INVENTORY FULL!");
        if (quantity > 0) { collected=false; return; }
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
