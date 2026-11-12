using UnityEngine;

public class HiddenCabinEntrance : InteractionTarget
{
    public Collider2D barrier;
    public Transform barrierVisual;
    public ForestNavigation navigation;
    public LootChest concealedChest;
    public Vector3 openOffset = new Vector3(.9f, 0, 0);
    [SerializeField] bool opened;
    Vector3 closedPosition;
    public bool Opened => opened;
    public override float Range => 1.15f;
    public override bool Available => !opened && barrier;
    public override string Prompt => "[E] Move loose planks";

    void Awake()
    {
        if (barrierVisual) closedPosition = barrierVisual.localPosition;
        ApplyState();
    }
    public override void Use(PlayerInteractionController player)
    {
        if (!player || !player.actions || player.actions.Busy || PlayerInteractionController.InputBlocked || !Reachable(player.transform.position)) return;
        opened = true;
        ApplyState();
        player.Notice("A HIDDEN PATH — FOLLOW THE CABIN'S REAR FENCE TO THE CACHE.");
    }
    void ApplyState()
    {
        if (barrier) barrier.enabled = !opened;
        if (concealedChest) concealedChest.enabled = opened;
        if (barrierVisual) barrierVisual.localPosition = closedPosition + (opened ? openOffset : Vector3.zero);
        Physics2D.SyncTransforms();
        if (navigation) navigation.Invalidate();
    }
}
