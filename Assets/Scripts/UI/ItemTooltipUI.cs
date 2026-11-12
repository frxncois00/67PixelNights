using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    public Text label;
    public void Show(InventorySlot slot)
    {
        if (slot == null || slot.Empty) { label.text = "Select an item to inspect it."; return; }
        var item = slot.Item;
        label.text = item.DisplayName.ToUpperInvariant() + "\n\n" + item.Category +
            "\nQuantity  " + slot.Quantity + (item.Stackable ? " / " + item.MaximumStack : "") +
            "\n\n" + item.Description + (item.Droppable ? "" : "\n\nCannot be dropped.");
    }
}
