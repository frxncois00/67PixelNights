using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInteractionController : MonoBehaviour {
    public TemporaryResourceWallet wallet;
    public PlayerGatherAnimation actions;
    public Text prompt, message;
    PlayerToolController tools;
    void Awake() => tools = GetComponent<PlayerToolController>();
    float hideAt;
    void Start() {
        if (wallet && actions && prompt && message) return;
        Debug.LogError("Missing player interaction references", this);
        enabled = false;
    }
    public static bool InputBlocked => TradingUI.IsOpen || BuildingController.Active || BuildingController.ModalOpen || PlayerSurvivalController.InputLocked || InventoryUI.IsOpen || Time.timeScale == 0 || (EventSystem.current &&
        EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>());
    public InteractionTarget SelectTarget() {
        InteractionTarget best = null;
        float nearest = float.PositiveInfinity;
        foreach (var target in InteractionTarget.Active) {
            if (!target || !target.Reachable(transform.position)) continue;
            float d = ((Vector2)(target.transform.position-transform.position)).sqrMagnitude;
            if (d < nearest || (d == nearest && string.CompareOrdinal(target.name,best.name)<0)) {
                best = target;
                nearest = d;
            }
        }
        return best;
    }
    public bool Interact() {
        if (actions.Busy || InputBlocked) return false;
        var target = SelectTarget();
        if (!target) return false;
        target.Use(this);
        return true;
    }
    public void Notice(string value) { message.text=value; hideAt=Time.time+2; }
    void LateUpdate() {
        var target = SelectTarget();
        string hint = target ? target.Prompt : "";
        if (!target && tools && tools.FindTarget(ResourceType.Wood)) hint = "[SPACE / CLICK] Chop tree";
        if (tools && tools.FindTarget(ResourceType.Stone))
            hint += hint.Length == 0 ? "[R] Mine stone" : "   [R] Mine stone";
        prompt.text = actions.Busy || InputBlocked ? "" : hint;
        if (Time.time >= hideAt) message.text = "";
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.eKey.wasPressedThisFrame) Interact();
        else if (keyboard.rKey.wasPressedThisFrame && tools) tools.Swing(ResourceType.Stone);
    }
}
