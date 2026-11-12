using UnityEngine;

public class PlaceableStructure : InteractionTarget
{
    public BuildingDefinition definition;
    public ForestNavigation navigation;
    public SpriteRenderer visual;
    public StructureStorage storage;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => definition ? Mathf.Clamp(definition.maximumHealth, 1, 10000) : 50;
    public float NormalizedHealth => CurrentHealth / MaxHealth;
    public bool IsDestroyed => CurrentHealth <= 0;
    public override bool Available => !IsDestroyed || (storage && !storage.Empty);
    public override string Prompt => IsDestroyed ? "[E] Recover storage contents" : "[E] Manage structure (" + CurrentHealth.ToString("0") + " / " + MaxHealth + ")";

    Color normal;
    float flashUntil;
    void Awake()
    {
        CurrentHealth = MaxHealth;
        if (visual)
            normal = visual.color;
    }

    public override void Use(PlayerInteractionController p)
    {
        if (PlayerInteractionController.InputBlocked || !Reachable(p.transform.position) || p.actions.Busy)
            return;
        p.GetComponent<BuildingController>().OpenStructure(this);
    }

    public bool TakeDamage(float amount)
    {
        if (IsDestroyed || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0)
            return false;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        flashUntil = Time.time + .15f;
        if (visual)
            visual.color = Color.white;
        if (IsDestroyed)
        {
            GetComponent<Collider2D>().enabled = false;
            if (navigation)
                navigation.Invalidate();
            if (!storage || storage.Empty)
                Destroy(gameObject);
            else if (visual)
                normal = new Color(.4f, .4f, .4f);
        }

        return true;
    }

    public float Repair(InventoryController inv)
    {
        if (IsDestroyed || CurrentHealth >= MaxHealth)
            return 0;
        var kit = inv.FindItem("repair_kit");
        if (!kit || inv.Count(kit) < 1)
            return 0;
        float amount = Mathf.Min(25, MaxHealth - CurrentHealth);
        if (!inv.Remove(kit, 1))
            return 0;
        CurrentHealth += amount;
        return amount;
    }

    public bool Dismantle()
    {
        if (storage && !storage.Empty)
            return false;
        GetComponent<Collider2D>().enabled = false;
        if (navigation)
            navigation.Invalidate();
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    void Update()
    {
        if (visual && Time.time >= flashUntil)
            visual.color = normal;
    }

    void OnDestroy()
    {
        if (navigation)
            navigation.Invalidate();
    }
}
