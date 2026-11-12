using UnityEngine;
using UnityEngine.Tilemaps;

public class SluiceShaftEntrance : InteractionTarget
{
    public Transform grateVisual;
    public Collider2D grateCollider;
    public Transform walkwayEntryPoint;
    public ForestNavigation navigation;
    public bool grateOpened;

    public override float Range => 1.85f;
    public override bool Available => true;
    public override string Prompt => grateOpened
        ? "[E] Inspect Sluice Shaft & Depth Marker"
        : "[E] Open Sluice Shaft Grate & Enter Walkway";

    void Start()
    {
        ApplyState(false);
    }

    public override void Use(PlayerInteractionController player)
    {
        if (!grateOpened)
        {
            OpenGrate();
            if (player != null)
            {
                if (walkwayEntryPoint != null && Vector2.Distance(player.transform.position, walkwayEntryPoint.position) < 2.2f)
                {
                    player.transform.position = walkwayEntryPoint.position;
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.position = walkwayEntryPoint.position;
                    Physics2D.SyncTransforms();
                }
                player.Notice("SLUICE SHAFT GRATE OPENED — FOLLOW WALKWAY TO THE DRAINAGE WHEEL & CACHE");
            }
        }
        else if (player != null)
        {
            player.Notice("DEPTH MARKER III: TURN THE WALKWAY DRAINAGE WHEEL TO LOWER THE SPILLWAY CHANNEL");
        }
    }

    public void OpenGrate()
    {
        grateOpened = true;
        ApplyState(true);
    }

    public void ApplyState(bool invalidateNavigation)
    {
        if (grateVisual != null)
            grateVisual.localPosition = grateOpened ? new Vector3(0f, 0.42f, 0f) : Vector3.zero;
        if (grateCollider != null)
            grateCollider.enabled = !grateOpened;
        Physics2D.SyncTransforms();
        if (invalidateNavigation)
        {
            if (navigation == null)
                navigation = Object.FindAnyObjectByType<ForestNavigation>();
            if (navigation != null)
                navigation.Invalidate();
        }
    }
}
