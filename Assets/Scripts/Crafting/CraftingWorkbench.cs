using UnityEngine;

public class CraftingWorkbench : InteractionTarget
{
    [Min(.5f)]
    public float interactionRange = 2;
    public override float Range => Mathf.Max(.5f, interactionRange);
    public override bool Available => true;
    public override string Prompt => GetComponent<SawmillStation>() ? (GetComponent<SawmillStation>().Unlocked ? "[E] Sawmill - craft and upgrade" : "[E] Unlock sawmill - 10 Wood Logs") : "[E] Craft Items";

    public override void Use(PlayerInteractionController p)
    {
        if (PlayerInteractionController.InputBlocked || !Reachable(p.transform.position) || p.actions.Busy)
            return;
        p.GetComponent<BuildingController>().OpenWorkbench(this);
    }
}
