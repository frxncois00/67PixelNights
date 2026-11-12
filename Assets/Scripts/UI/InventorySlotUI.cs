using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public InventoryUI owner;
    public int index;
    public Image icon, background;
    public Text quantity, fallback;
    public Image highlight; // optional gold frame for the equipped/selected slot
    public void Refresh()
    {
        var slot = owner.inventory.GetSlot(index);
        icon.sprite = slot.Item ? slot.Item.Icon : null;
        icon.color = slot.Item ? slot.Item.IconTint : Color.white;
        icon.enabled = icon.sprite;
        quantity.text = !slot.Empty && slot.Item.Stackable ? slot.Quantity.ToString() : "";
        fallback.text = !slot.Empty && !slot.Item.Icon ? slot.Item.DisplayName : "";
        bool selected = owner.IsWindowOpen && owner.SelectedSlot == index;
        bool equipped = owner.equipment.Selected >= 0 && index == InventoryController.MainSlots + owner.equipment.Selected;
        if (highlight)
        {
            // Framed slot sprite: keep the art's colours, mark selection with the gold frame.
            highlight.enabled = selected || equipped;
            background.color = selected ? new Color(1f, .86f, .62f) : Color.white;
        }
        else
        background.color = selected ? new Color(.58f, .39f, .15f) :
            equipped ? new Color(.36f, .43f, .23f) : new Color(.16f, .19f, .14f);
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left) owner.Click(index);
        else if (data.button == PointerEventData.InputButton.Right) owner.Split(index);
    }
    public void OnPointerEnter(PointerEventData data) => owner.Hover(index);
    public void OnPointerExit(PointerEventData data) => owner.Hover(-1);
}

