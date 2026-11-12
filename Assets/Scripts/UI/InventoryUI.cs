using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Selection never removes a stack. Closing the window simply clears selection.
[DefaultExecutionOrder(-100)]
public class InventoryUI : MonoBehaviour
{
    public InventoryController inventory;
    public EquipmentController equipment;
    public ItemDropController drops;
    public PlayerGatherAnimation actions;
    public GameObject panel;
    public InventorySlotUI[] slots;
    public ItemTooltipUI tooltip;
    public Button closeButton, dropOneButton, dropStackButton;
    public Button useButton, equipButton;
    public FoodConsumption food;
    public static bool IsOpen => active && active.IsWindowOpen;
    static InventoryUI active;
    public bool IsWindowOpen => inventory && inventory.MenuOpen;
    public int SelectedSlot { get; private set; } = -1;
    int hovered = -1;

    void OnEnable()
    {
        active = this;
        if (inventory) inventory.Changed += Refresh;
        if (equipment) equipment.Changed += Refresh;
    }
    void Start()
    {
        if (!inventory || !equipment || !drops || !panel || !tooltip || slots == null || slots.Length != 25)
        {
            Debug.LogError("Inventory UI has missing references.", this);
            enabled = false;
            return;
        }
        closeButton.onClick.AddListener(Close);
        dropOneButton.onClick.AddListener(DropOne);
        dropStackButton.onClick.AddListener(DropStack);
        if (useButton) useButton.onClick.AddListener(UseSelected);
        if (equipButton) equipButton.onClick.AddListener(EquipSelected);
        SetOpen(false);
        Refresh();
    }
    void OnDisable()
    {
        if (inventory) { inventory.Changed -= Refresh; inventory.SetMenuOpen(false); }
        if (equipment) equipment.Changed -= Refresh;
        if (panel) panel.SetActive(false);
        if (active == this) active = null;
        SelectedSlot = hovered = -1;
    }
    void OnDestroy()
    {
        if (closeButton) closeButton.onClick.RemoveListener(Close);
        if (dropOneButton) dropOneButton.onClick.RemoveListener(DropOne);
        if (dropStackButton) dropStackButton.onClick.RemoveListener(DropStack);
        if (useButton) useButton.onClick.RemoveListener(UseSelected);
        if (equipButton) equipButton.onClick.RemoveListener(EquipSelected);
    }
    public bool SetOpen(bool value)
    {
        if (!inventory || !panel || (value && (TradingUI.IsOpen || BuildingController.ModalOpen || BuildingController.Active || inventory.ManagementLocked || (actions && actions.Busy)))) return false;
        inventory.SetMenuOpen(value);
        panel.SetActive(value);
        SelectedSlot = hovered = -1;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        Refresh();
        return true;
    }
    public void Close() => SetOpen(false);
    public void Click(int index)
    {
        if (inventory.ManagementLocked) return;
        if (!IsWindowOpen)
        {
            if (index >= 20) equipment.Select(index - 20);
            return;
        }
        if (SelectedSlot < 0) { if (!inventory.GetSlot(index).Empty) SelectedSlot = index; }
        else if (SelectedSlot == index) SelectedSlot = -1;
        else { inventory.Move(SelectedSlot, index); SelectedSlot = -1; }
        Refresh();
    }
    public void Split(int index)
    {
        if (!IsWindowOpen || inventory.ManagementLocked) return;
        inventory.Split(index);
        SelectedSlot = index;
        Refresh();
    }
    public void Hover(int index) { hovered = index; RefreshTooltip(); }
    void RefreshTooltip()
    {
        if (!tooltip || !inventory) return;
        int index = hovered >= 0 ? hovered : SelectedSlot;
        tooltip.Show(inventory.GetSlot(index));
    }
    public void Refresh()
    {
        if (!inventory || !equipment) return;
        if (SelectedSlot >= 0 && inventory.GetSlot(SelectedSlot).Empty) SelectedSlot = -1;
        if (slots != null) foreach (var slot in slots) if (slot) slot.Refresh();
        RefreshTooltip();
        var selected = inventory.GetSlot(SelectedSlot);
        if (equipButton) equipButton.interactable = selected != null && !selected.Empty && !inventory.ManagementLocked;
        bool canDrop = selected != null && !selected.Empty && selected.Item.Droppable;
        if (dropOneButton) dropOneButton.interactable = canDrop;
        if (dropStackButton) dropStackButton.interactable = canDrop;
        if (useButton) useButton.interactable = selected != null && !selected.Empty && selected.Item.Consumable && !inventory.ManagementLocked;
    }
    public void EquipSelected() { if (IsWindowOpen && equipment.EquipFromBag(SelectedSlot)) { SelectedSlot = -1; Refresh(); } }
    void UseSelected() { if (IsWindowOpen && food) food.Consume(SelectedSlot, true); }
    void DropOne() { if (IsWindowOpen) drops.Drop(SelectedSlot, 1); }
    void DropStack()
    {
        var slot = inventory.GetSlot(SelectedSlot);
        if (IsWindowOpen && slot != null) drops.Drop(SelectedSlot, slot.Quantity);
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || EquipmentController.Typing) return;
        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame) SetOpen(!IsWindowOpen);
        else if (IsWindowOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
    }
}
