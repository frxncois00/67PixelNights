using UnityEngine;

public class PlayerSurvivalController : MonoBehaviour
{
    public PlayerHealth health;
    public InventoryController inventory;
    public InventoryUI inventoryUI;
    public PlayerGatherAnimation actions;
    public CampfireCooking cooking;
    public GameOverUI gameOver;
    static PlayerSurvivalController current;
    public static bool InputLocked => current && current.health && current.health.IsDead;
    void OnEnable()
    {
        current = this;
        if (!health) health = GetComponent<PlayerHealth>();
        if (health)
        {
            health.Died += HandleDeath;
            if (health.IsDead && inventory) HandleDeath();
        }
        else { Debug.LogError("Survival controller needs PlayerHealth.", this); enabled = false; }
    }
    void Start() { if (health && health.IsDead) HandleDeath(); }
    void OnDisable() { if (health) health.Died -= HandleDeath; }
    void OnDestroy() { if (current == this) current = null; }
    void HandleDeath()
    {
        if (actions) actions.enabled = false; // Cancels and restores an in-progress visual action first.
        var movement = GetComponent<PlayerMovement>();
        if (movement) movement.enabled = false;
        var body = GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity = Vector2.zero;
        var tools = GetComponent<PlayerToolController>();
        if (tools) tools.enabled = false;
        if (inventoryUI) inventoryUI.Close();
        if (inventory) inventory.LockManagement();
        if (cooking) cooking.Cancel();
        if (gameOver) gameOver.Show();
    }
}
