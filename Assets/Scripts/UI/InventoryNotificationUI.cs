using UnityEngine;
using UnityEngine.UI;

public class InventoryNotificationUI : MonoBehaviour
{
    public InventoryController inventory;
    public Text label;
    float hideAt;
    void OnEnable() { if (inventory) inventory.Feedback += Show; }
    void OnDisable() { if (inventory) inventory.Feedback -= Show; }
    void Show(string text) { label.text = text; hideAt = Time.unscaledTime + 2.5f; }
    void Update() { if (label && Time.unscaledTime >= hideAt) label.text = ""; }
}
