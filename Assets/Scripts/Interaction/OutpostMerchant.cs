using System;
using UnityEngine;

public class OutpostMerchant : InteractionTarget
{
    [Serializable]
    public class Exchange
    {
        public string label;
        public ItemDefinition payment, reward;
        [Min(1)] public int paymentCount = 3, rewardCount = 1;
        [Min(0)] public int stock = 5;
    }

    public Exchange[] exchanges;
    [SerializeField] int selectedExchange;
    bool transferring;
    public int SelectedExchange => selectedExchange;
    public override bool Available => exchanges != null && exchanges.Length > 0;
    public override string Prompt => "[E] Browse trades";

    public override void Use(PlayerInteractionController player)
    {
        if (!player || PlayerInteractionController.InputBlocked || !Reachable(player.transform.position)) return;
        var menu = player.GetComponent<TradingUI>();
        if (!menu) menu = player.gameObject.AddComponent<TradingUI>();
        menu.Open(this, player);
    }

    public bool TryTrade(PlayerInteractionController player, int offerIndex)
    {
        if (BuildingController.Active || BuildingController.ModalOpen || InventoryUI.IsOpen || PlayerSurvivalController.InputLocked || Time.timeScale == 0) return false;
        if (transferring || !player || !player.actions || player.actions.Busy || (PlayerInteractionController.InputBlocked && !TradingUI.Owns(this, player)) || !Reachable(player.transform.position)) return false;
        var inventory = player.GetComponent<InventoryController>();
        if (!inventory || inventory.ManagementLocked) return false;
        if (exchanges == null || offerIndex < 0 || offerIndex >= exchanges.Length) return false;
        var e = exchanges[offerIndex];
        if (e == null || !e.payment || !e.reward || inventory.FindItem(e.payment.Id) != e.payment || inventory.FindItem(e.reward.Id) != e.reward || e.paymentCount <= 0 || e.rewardCount <= 0 || e.paymentCount > 9999 || e.rewardCount > 9999)
        { player.Notice("THIS OFFER IS UNAVAILABLE."); return false; }
        if (e.stock <= 0) { player.Notice("SOLD OUT — CHOOSE ANOTHER OFFER WITH T."); return false; }
        if (inventory.Count(e.payment) < e.paymentCount)
        { player.Notice($"NEED {e.paymentCount} {e.payment.DisplayName.ToUpperInvariant()} TO TRADE."); return false; }
        // CraftBatch simulates payment and capacity, then commits once; failed exchanges never debit items.
        var cost = new[] { new ItemAmount { item = e.payment, amount = e.paymentCount } };
        transferring = true;
        try
        {
            if (!inventory.CraftBatch(cost, e.reward, e.rewardCount))
            { player.Notice("INVENTORY FULL — MAKE ROOM BEFORE TRADING."); return false; }
            e.stock--;
            player.Notice($"TRADED {e.paymentCount} {e.payment.DisplayName} FOR {e.rewardCount} {e.reward.DisplayName}.");
            return true;
        }
        finally { transferring = false; }
    }
}
