using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

return FrameWaterworks.Main();

public static class FrameWaterworks
{
    public static string Main()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.in2DMode = true;
            sv.LookAtDirect(new Vector3(-19.5f, 13.5f, 0f), Quaternion.identity, 12.2f);
            sv.Repaint();
        }

        var sb = new StringBuilder();
        var nav = Object.FindAnyObjectByType<ForestNavigation>();
        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        var grove = GameObject.Find("EnvironmentExpansion/DrownedGrove")?.GetComponent<AshenFrontierEncounterController>();

        nav.Invalidate();

        // Check all 13 spawn slots walkability
        sb.AppendLine("=== SPAWN SLOTS WALKABILITY AUDIT ===");
        int validSlots = 0;
        foreach (var slot in grove.rogueSlots)
        {
            Vector2 p = slot.point.position;
            bool w = spawner.Walkable(p);
            if (w) validSlots++;
            sb.AppendLine($"  {slot.label,-34} @ ({p.x:F2}, {p.y:F2}) prefab={slot.prefab.name,-18} walkable={w}");
        }
        sb.AppendLine($"  Valid slots: {validSlots}/{grove.rogueSlots.Length}");

        // Check loop pathfinding across all 5 landmarks + Central Islet stepping stones
        var routeWaypoints = new (string name, Vector2 pos)[]
        {
            ("South Road Entrance", new Vector2(-26.5f, 4.5f)),
            ("Fishermans Landing",  new Vector2(-26.5f, 8.5f)),
            ("Sluice Shaft Walk",   new Vector2(-26.5f, 15.8f)),
            ("Pumpkeepers Cabin",   new Vector2(-22.5f, 20.5f)),
            ("North Plank Bridge",  new Vector2(-18.5f, 20.5f)),
            ("East Upper Terrace",  new Vector2(-14.5f, 19.5f)),
            ("Flooded Pump Station",new Vector2(-12.0f, 10.8f)),
            ("Gauge Shrine Islet",  new Vector2(-17.0f, 12.8f)),
            ("South Peninsula",     new Vector2(-16.5f, 9.5f)),
            ("South Plank Bridge",  new Vector2(-18.5f, 6.5f)),
            ("Fishermans Landing",  new Vector2(-26.5f, 8.5f))
        };

        sb.AppendLine("\n=== FULL LOOP NAVIGATION AUDIT ===");
        var path = new List<Vector2>();
        for (int i = 0; i < routeWaypoints.Length - 1; i++)
        {
            var a = routeWaypoints[i];
            var b = routeWaypoints[i + 1];
            path.Clear();
            bool ok = nav.Find(a.pos, b.pos, path, false, false);
            sb.AppendLine($"  {a.name} -> {b.name}: found={ok} steps={path.Count}");
        }

        // Check bounded sluice channel before vs after turning Drainage Wheel
        var wheel = Object.FindAnyObjectByType<SluiceDrainageWheel>();
        bool blockedBefore = !spawner.Walkable(new Vector2(-18.5f, 16.0f));
        wheel.ActivateDrainage();
        var hitAfter = Physics2D.OverlapCircle(new Vector2(-18.5f, 16.0f), 0.24f);
        bool clearAfter = spawner.Walkable(new Vector2(-18.5f, 16.0f)) && spawner.Walkable(new Vector2(-17.5f, 16.0f));
        var wCellTest = wheel.waterTilemap.WorldToCell(new Vector3(-18.5f, 16.2f, 0f));
        sb.AppendLine($"  After drain OverlapCircle(-18.5,16.0): {(hitAfter!=null?hitAfter.name:"NONE")} tile={wheel.waterTilemap.GetTile(wCellTest)?.name} colType={wheel.waterTilemap.GetColliderType(wCellTest)}");
        // Reset wheel back to initial undrained state for saved scene
        BuildSunkenWaterworksResetWheel(wheel);

        sb.AppendLine($"\n=== DRAINAGE WHEEL CHANNEL PAYOFF ===");
        sb.AppendLine($"  Channel blocked before wheel turn: {blockedBefore}");
        sb.AppendLine($"  Channel walkable after wheel turn: {clearAfter}");

        return sb.ToString();
    }

    static void BuildSunkenWaterworksResetWheel(SluiceDrainageWheel wheel)
    {
        wheel.isDrained = false;
        if (wheel.wheelVisual != null) wheel.wheelVisual.localRotation = Quaternion.identity;
        if (wheel.channelGrateVisual != null) wheel.channelGrateVisual.localPosition = Vector3.zero;
        if (wheel.channelGrateCollider != null)
        {
            wheel.channelGrateCollider.gameObject.SetActive(true);
            wheel.channelGrateCollider.enabled = true;
        }
        foreach (var go in wheel.floodedChannelObjects) if (go != null) go.SetActive(true);
        foreach (var go in wheel.drainedChannelObjects) if (go != null) go.SetActive(false);
        foreach (var sr in wheel.waterLevelMarkers) if (sr != null) sr.color = wheel.floodedMarkerColor;
        Physics2D.SyncTransforms();
        if (wheel.navigation != null) wheel.navigation.Invalidate();
    }
}
