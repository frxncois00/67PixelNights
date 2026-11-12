using UnityEngine;

public enum ItemCategory { Tool, Resource, Food, FoodMaterial, Building, Utility }

[CreateAssetMenu(menuName = "Survival/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public bool craftedWeapon;
    public bool rangedWeapon;
    public Sprite projectileSprite;
    public float weaponDamage = 15, weaponRange = 1.5f, weaponCooldown = .6f;
    [SerializeField] string itemId, displayName;
    [SerializeField, TextArea] string description;
    [SerializeField] Sprite icon;
    [SerializeField] ItemCategory category;
    [SerializeField] bool stackable = true, equippable, droppable = true;
    [SerializeField, Min(1)] int maximumStack = 99;
    [SerializeField] ResourceType resourceType;
    [Header("Food (unused for tools and resources)")]
    [SerializeField] bool consumable;
    [SerializeField, Min(0)] float hungerRestored, healthRestored;
    [SerializeField] Color iconTint = Color.white;
    public bool Consumable => category == ItemCategory.Food && consumable;
    public float HungerRestored => float.IsNaN(hungerRestored) || float.IsInfinity(hungerRestored) ? 0 : Mathf.Max(0, hungerRestored);
    public float HealthRestored => float.IsNaN(healthRestored) || float.IsInfinity(healthRestored) ? 0 : Mathf.Max(0, healthRestored);
    public Color IconTint => iconTint;
    public string Id => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemCategory Category => category;
    public bool Stackable => stackable;
    public bool Equippable => equippable && category == ItemCategory.Tool;
    public bool Droppable => droppable;
    public int MaximumStack => stackable ? Mathf.Clamp(maximumStack, 1, 9999) : 1;
    public ResourceType Resource => resourceType;
}
