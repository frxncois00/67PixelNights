using UnityEngine;
public class SawmillStation : MonoBehaviour
{
    public const int UnlockCost = 10;
    [SerializeField] bool unlocked;
    public bool Unlocked => unlocked;
    public bool TryUnlock(InventoryController inventory)
    {
        if (unlocked || !inventory || inventory.ManagementLocked) return false;
        var wood = inventory.FindItem("wood_log");
        if (!wood || !inventory.Remove(wood, UnlockCost)) return false;
        unlocked = true;
        return true;
    }
    public SpriteRenderer visual;
    public Sprite[] level1, level2, level3;
    public ItemAmount[] upgrade2, upgrade3;
    public int Level { get; private set; } = 1;
    public ItemAmount[] NextCost => Level == 1 ? upgrade2 : upgrade3;
    public bool TryUpgrade(InventoryController inventory)
    {
        if (!unlocked || Level >= 3 || !inventory || inventory.ManagementLocked) return false;
        foreach (var c in NextCost) if (inventory.Count(c.item) < c.amount) return false;
        foreach (var c in NextCost) inventory.Remove(c.item,c.amount);
        Level++; return true;
    }
    void Update(){var frames=Level==1?level1:Level==2?level2:level3;if(visual&&frames!=null&&frames.Length>0)visual.sprite=frames[(int)(Time.time*10)%frames.Length];}
}
