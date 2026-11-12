using UnityEngine;
using UnityEngine.UI;

public class SurvivalHUD : MonoBehaviour
{
    public PlayerHealth health;
    public PlayerHunger hunger;
    public InventoryController inventory;
    public Text healthText, hungerText;
    public Image healthBar, hungerBar;
    [Header("Optional styling")]
    public Text healthValueText, hungerValueText; // when set, labels show the name and these show the numbers
    public RectTransform healthIcon, hungerIcon;   // pulse when low
    int hungerState, healthState;
    void OnEnable()
    {
        if (!health || !hunger) { Debug.LogError("SurvivalHUD needs health and hunger.", this); return; }
        health.Changed += Refresh; hunger.Changed += Refresh;
    }
    void Start() => Refresh();
    void Update()
    {
        // Heartbeat pulse on the icons when a stat runs low.
        if (healthIcon) healthIcon.localScale = Vector3.one * (health && health.NormalizedHealth <= .25f ? 1 + .12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6)) : 1);
        if (hungerIcon) hungerIcon.localScale = Vector3.one * (hunger && hunger.NormalizedHunger <= .2f ? 1 + .1f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4)) : 1);
    }
    void OnDisable()
    {
        if (health) health.Changed -= Refresh;
        if (hunger) hunger.Changed -= Refresh;
    }
    void Refresh()
    {
        if (!health || !hunger || !healthText || !hungerText || !healthBar || !hungerBar) return;
        string hpStr = Mathf.CeilToInt(health.CurrentHealth) + " / " + health.MaxHealth.ToString("0");
        string hgStr = Mathf.CeilToInt(hunger.CurrentHunger) + " / " + hunger.MaxHunger.ToString("0");
        if (healthValueText) { healthText.text = "HEALTH"; healthValueText.text = hpStr; } else healthText.text = "HEALTH  " + hpStr;
        if (hungerValueText) { hungerText.text = "HUNGER"; hungerValueText.text = hgStr; } else hungerText.text = "HUNGER  " + hgStr;
        healthBar.fillAmount = health.NormalizedHealth; hungerBar.fillAmount = hunger.NormalizedHunger;
        healthBar.color = health.NormalizedHealth <= .25f ? new Color(.55f,.12f,.12f) : new Color(.85f,.24f,.2f);
        hungerBar.color = hunger.NormalizedHunger <= .2f ? new Color(.65f,.3f,.08f) : new Color(.95f,.65f,.2f);
        int h = hunger.IsStarving ? 2 : hunger.NormalizedHunger <= .2f ? 1 : 0;
        int hp = health.NormalizedHealth <= .25f ? 1 : 0;
        if (inventory && health.IsAlive)
        {
            if (h > hungerState) inventory.Notify(h == 2 ? "YOU ARE STARVING!" : "LOW HUNGER!");
            if (hp > healthState) inventory.Notify("LOW HEALTH!");
        }
        hungerState = h; healthState = hp;
    }
}
