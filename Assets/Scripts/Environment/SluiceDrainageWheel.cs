using UnityEngine;
using UnityEngine.Tilemaps;

public class SluiceDrainageWheel : InteractionTarget
{
    public Transform wheelVisual;
    public Transform channelGrateVisual;
    public Collider2D channelGrateCollider;
    public GameObject[] floodedChannelObjects;
    public GameObject[] drainedChannelObjects;
    public SpriteRenderer[] waterLevelMarkers;
    public Color floodedMarkerColor = new Color(0.95f, 0.42f, 0.25f, 0.95f);
    public Color drainedMarkerColor = new Color(0.32f, 0.95f, 0.78f, 0.95f);
    public Tilemap waterTilemap;
    public TileBase walkableWaterTile;
    public TileBase blockedWaterTile;
    public Vector3Int[] boundedChannelCells;
    public ForestNavigation navigation;
    public bool isDrained;

    float targetWheelAngle;
    float currentWheelAngle;

    public override float Range => 1.85f;
    public override bool Available => true;
    public override string Prompt => isDrained
        ? "[E] Inspect Drainage Wheel (Spillway Channel Drained)"
        : "[E] Turn Drainage Wheel (Open Grate & Lower Sluice Channel)";

    void Start()
    {
        targetWheelAngle = currentWheelAngle = isDrained ? -180f : 0f;
        if (wheelVisual != null)
            wheelVisual.localRotation = Quaternion.Euler(0f, 0f, currentWheelAngle);
        ApplyVisualState(isDrained, true);
    }

    void Update()
    {
        if (wheelVisual != null && Mathf.Abs(currentWheelAngle - targetWheelAngle) > 0.1f)
        {
            currentWheelAngle = Mathf.MoveTowards(currentWheelAngle, targetWheelAngle, Time.deltaTime * 260f);
            wheelVisual.localRotation = Quaternion.Euler(0f, 0f, currentWheelAngle);
        }
    }

    public override void Use(PlayerInteractionController player)
    {
        if (!isDrained)
        {
            ActivateDrainage();
            if (player != null)
                player.Notice("DRAINAGE WHEEL TURNED — SPILLWAY GRATE OPENED & CHANNEL LOWERED!");
        }
        else if (player != null)
        {
            player.Notice("SPILLWAY CHANNEL DRAINED — WALKABLE SLUICE CROSSING IS OPEN");
        }
    }

    public void ActivateDrainage()
    {
        isDrained = true;
        targetWheelAngle = -180f;
        ApplyVisualState(true, true);
    }

    public void ApplyVisualState(bool drained, bool updateTilemap)
    {
        if (channelGrateVisual != null)
            channelGrateVisual.localPosition = drained ? new Vector3(0f, 0.52f, 0f) : Vector3.zero;
        if (channelGrateCollider != null)
        {
            channelGrateCollider.enabled = !drained;
            if (channelGrateCollider.gameObject != gameObject && (channelGrateVisual == null || channelGrateCollider.gameObject != channelGrateVisual.gameObject))
                channelGrateCollider.gameObject.SetActive(!drained);
        }

        if (floodedChannelObjects != null)
        {
            foreach (var go in floodedChannelObjects)
                if (go != null) go.SetActive(!drained);
        }

        if (drainedChannelObjects != null)
        {
            foreach (var go in drainedChannelObjects)
                if (go != null) go.SetActive(drained);
        }

        if (waterLevelMarkers != null)
        {
            foreach (var sr in waterLevelMarkers)
                if (sr != null) sr.color = drained ? drainedMarkerColor : floodedMarkerColor;
        }

        if (updateTilemap && waterTilemap != null && boundedChannelCells != null)
        {
            foreach (var cell in boundedChannelCells)
            {
                if (drained)
                {
                    if (walkableWaterTile != null)
                        waterTilemap.SetTile(cell, walkableWaterTile);
                    waterTilemap.SetColliderType(cell, Tile.ColliderType.None);
                }
                else
                {
                    if (blockedWaterTile != null)
                        waterTilemap.SetTile(cell, blockedWaterTile);
                    waterTilemap.SetColliderType(cell, Tile.ColliderType.Grid);
                }
            }
            waterTilemap.RefreshAllTiles();
            var tmCol = waterTilemap.GetComponent<TilemapCollider2D>();
            if (tmCol != null)
            {
                tmCol.ProcessTilemapChanges();
                tmCol.enabled = false;
                tmCol.enabled = true;
            }
            var compCol = waterTilemap.GetComponent<CompositeCollider2D>();
            if (compCol != null)
                compCol.GenerateGeometry();
        }

        Physics2D.SyncTransforms();

        if (navigation == null)
            navigation = Object.FindAnyObjectByType<ForestNavigation>();
        if (navigation != null && updateTilemap)
            navigation.Invalidate();
    }
}
