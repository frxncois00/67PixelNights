using System;
using UnityEngine;

[Serializable]
public class ItemAmount
{
    public ItemDefinition item;
    [Min(1)]
    public int amount = 1;
}

[CreateAssetMenu(menuName = "Survival/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Range(1,3)] public int requiredSawmillLevel = 1;
    public string recipeId, displayName;
    [TextArea]
    public string description;
    public ItemAmount[] ingredients;
    public ItemDefinition result;
    public Sprite Icon => result ? result.Icon : null;
    [Min(1)]
    public int quantity = 1;
}
