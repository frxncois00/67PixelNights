using UnityEngine;

public class BerryBush : InteractionTarget
{
    public ItemDefinition berry;
    [Min(1)] public int minimumYield = 2, maximumYield = 4;
    [Min(.1f)] public float respawnSeconds = 90;
    public int Remaining { get; private set; } = -1;
    public bool Depleted { get; private set; }
    float readyAt;
    public override bool Available => berry && !Depleted;
    public override string Prompt => "[E] Gather Berries" + (Remaining >= 0 ? " (" + Remaining + ")" : "");
    public override void Use(PlayerInteractionController player)
    {
        if (PlayerInteractionController.InputBlocked || !Reachable(player.transform.position) || !berry) return;
        var inventory = player.GetComponent<InventoryController>();
        if (!inventory) return;
        if (Remaining < 0) Remaining = Random.Range(Mathf.Clamp(minimumYield, 1, 20), Mathf.Clamp(maximumYield, Mathf.Clamp(minimumYield, 1, 20), 20) + 1);
        int accepted = inventory.Add(berry, Remaining);
        Remaining -= accepted;
        player.Notice(accepted > 0 ? "+" + accepted + " BERRIES" : "INVENTORY FULL!");
        if (Remaining > 0) return;
        Depleted = true;
        readyAt = Time.time + Mathf.Max(.1f, respawnSeconds);
    }
    void Update()
    {
        if (!Depleted || Time.time < readyAt) return;
        Depleted = false;
        Remaining = -1;
    }
}
