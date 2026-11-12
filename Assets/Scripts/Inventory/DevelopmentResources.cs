using UnityEngine;
using UnityEngine.InputSystem;

// Testing shortcut, available in the Editor and development builds only.
[RequireComponent(typeof(InventoryController))]
public class DevelopmentResources : MonoBehaviour
{
    [Min(1)] public int woodAmount = 50, stoneAmount = 50, scrapAmount = 20;
    InventoryController inventory;
    void Awake() => inventory = GetComponent<InventoryController>();
    void Update()
    {
        if (!Debug.isDebugBuild || EquipmentController.Typing) return;
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) GiveResources();
    }
    public bool GiveResources()
    {
        if (!Debug.isDebugBuild || !inventory || inventory.ManagementLocked) return false;
        int wood = inventory.Add(inventory.FindItem("wood_log"), Mathf.Max(1, woodAmount));
        int stone = inventory.Add(inventory.FindItem("stone"), Mathf.Max(1, stoneAmount));
        int scrap = inventory.Add(inventory.FindItem("scrap"), Mathf.Max(1, scrapAmount));
        inventory.Notify("TEST RESOURCES: +" + wood + " WOOD, +" + stone + " STONE, +" + scrap + " SCRAP" +
            (wood < woodAmount || stone < stoneAmount || scrap < scrapAmount ? " — INVENTORY SPACE LIMITED" : ""));
        return wood + stone + scrap > 0;
    }
}
