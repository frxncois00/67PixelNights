using UnityEngine;

[CreateAssetMenu(menuName = "Survival/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    public ItemDefinition item;
    public PlaceableStructure prefab;
    public Vector2 footprint = new Vector2(.9f, .45f);
    [Min(1)]
    public float maximumHealth = 50;
    public bool storage;
}
