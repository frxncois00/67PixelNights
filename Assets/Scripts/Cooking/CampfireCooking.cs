using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CampfireCooking : MonoBehaviour
{
    public CampfireController fire;
    public InventoryController inventory;
    public PlayerHealth health;
    public PlayerGatherAnimation actions;
    public Transform player;
    public ItemDefinition rawMeat, cookedMeat;
    [Min(.1f)] public float cookingSeconds = 5, range = 2;
    public bool IsCooking { get; private set; }
    public float Elapsed { get; private set; }
    public float Duration => Mathf.Max(.1f, cookingSeconds);
    public float Progress => Mathf.Clamp01(Elapsed / Duration);
    public bool InRange => player && Vector2.Distance(player.position, transform.position) <= Mathf.Max(.1f, range);
    public bool Paused => IsCooking && fire && !fire.IsBurning;
    public event Action Changed;
    void Start()
    {
        if (fire && inventory && health && player && rawMeat && cookedMeat) return;
        Debug.LogError("Campfire cooking is missing required references.", this);
        enabled = false;
    }
    public bool Begin()
    {
        if (!isActiveAndEnabled || IsCooking || !inventory || !health || health.IsDead ||
            PlayerInteractionController.InputBlocked || (actions && actions.Busy)) return false;
        if (!InRange || !fire || !fire.IsBurning) { inventory.Notify("COOK NEAR A BURNING CAMPFIRE"); return false; }
        if (!inventory.CanExchangeOne(rawMeat, cookedMeat))
        {
            inventory.Notify(inventory.Count(rawMeat) == 0 ? "RAW MEAT REQUIRED" : "INVENTORY FULL!");
            return false;
        }
        IsCooking = true;
        Elapsed = 0;
        Changed?.Invoke();
        return true;
    }
    public void Advance(float seconds)
    {
        if (!IsCooking) return;
        if (!health || health.IsDead || !InRange) { Cancel(); return; }
        if (!fire || !fire.IsBurning || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        Elapsed = Mathf.Min(Duration, Elapsed + seconds);
        Changed?.Invoke();
        if (Elapsed < Duration) return;
        // Commit ingredient removal and output insertion together, with one inventory event.
        bool completed = inventory.ExchangeOne(rawMeat, cookedMeat);
        Cancel();
        inventory.Notify(completed ? "COOKED MEAT READY!" : "COOKING CANCELED — INGREDIENT OR SPACE UNAVAILABLE");
    }
    public void Cancel()
    {
        IsCooking = false;
        Elapsed = 0;
        Changed?.Invoke();
    }
    void OnDisable() => Cancel();
    void Update()
    {
        var key = Keyboard.current;
        if (key != null && key.cKey.wasPressedThisFrame && !EquipmentController.Typing) Begin();
        Advance(Time.deltaTime);
    }
}
