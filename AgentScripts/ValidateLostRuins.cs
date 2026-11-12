using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

// Independent, query-only scene validator. No component/asset/scene modifications.
// Execute with: unity command script run --file AgentScripts/ValidateLostRuins.cs --entry ValidateLostRuins.Main
public static class ValidateLostRuins
{
    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
    public static string Main()
    {
        var log = new StringBuilder();
        int failures = 0;
        void Check(bool pass, string message)
        {
            log.AppendLine((pass ? "PASS " : "FAIL ") + message);
            if (!pass) failures++;
        }
        var root = GameObject.Find("LostRuinsBiome");
        Check(root, "LostRuinsBiome exists");
        var nav = UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
        Check(nav && nav.ground && nav.safe, "Existing ForestNavigation ground and safezone references exist");
        if (!root || !nav || !nav.ground || !nav.safe) return log.ToString();
        var transforms = root.GetComponentsInChildren<Transform>(true);
        var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var colliders = root.GetComponentsInChildren<Collider2D>(true);
        var maps = root.GetComponentsInChildren<Tilemap>(true);
        Check(sprites.Length > 0, "Biome contains sprite artwork (" + sprites.Length + " renderers)");
        Check(sprites.All(s => s.sprite), "No missing sprite references");
        Check(renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported)), "All renderers use present, supported materials");
        Check(renderers.All(r => r.sharedMaterials.All(m => m.shader.name != "Hidden/InternalErrorShader")), "No error/pink shader assigned");
        Check(transforms.All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts on biome objects");
        Check(sprites.All(s => s.sprite == null || (s.sprite.pixelsPerUnit > 0 && s.sprite.texture.filterMode == FilterMode.Point)), "All sprite textures use Point filtering and valid pixels per unit");
        var stretched = sprites.Where(s => Mathf.Abs(Mathf.Abs(s.transform.lossyScale.x) - Mathf.Abs(s.transform.lossyScale.y)) > .0001f).ToArray();
        Check(stretched.Length == 0, "No nonuniform sprite stretching" + (stretched.Length > 0 ? ": " + string.Join(", ", stretched.Take(8).Select(s => Path(s.transform))) : ""));
        Check(colliders.Any(c => c.enabled && !c.isTrigger), "Biome has enabled solid obstacle colliders (" + colliders.Count(c => c.enabled && !c.isTrigger) + ")");
        var invalidComposite = root.GetComponentsInChildren<CompositeCollider2D>(true).Where(c => c.enabled && c.gameObject.activeInHierarchy && (c.pathCount == 0 || !c.attachedRigidbody || c.attachedRigidbody.bodyType != RigidbodyType2D.Static)).ToArray();
        Check(invalidComposite.Length == 0, "Enabled composite colliders contain geometry and use static Rigidbody2D" + (invalidComposite.Length > 0 ? ": " + string.Join(", ", invalidComposite.Select(c => Path(c.transform))) : ""));
        Check(colliders.Where(c => c.enabled && !c.isTrigger).All(c => !c.attachedRigidbody || c.attachedRigidbody.bodyType == RigidbodyType2D.Static), "Solid ruin obstacles are static and recognized by ForestNavigation");
        var unsafeSprites = sprites.Where(s => s.enabled && s.gameObject.activeInHierarchy && nav.safe.IsPositionSafe(s.transform.position)).ToArray();
        Check(unsafeSprites.Length == 0, "No active artwork pivots inside camp safezone");
        int wetOffGround = 0, missingTileSprite = 0;
        foreach (var map in maps)
        {
            foreach (var c in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(c)) continue;
                var p = map.GetCellCenterWorld(c);
                if (!nav.ground.HasTile(nav.ground.WorldToCell(p))) wetOffGround++;
                var tile = map.GetTile<Tile>(c);
                if (tile && !tile.sprite) missingTileSprite++;
            }
        }
        Check(missingTileSprite == 0, "No missing sprite references in ordinary Tile assets");
        log.AppendLine("INFO New tilemap cells outside original ground coverage: " + wetOffGround + " (coastal water may intentionally extend outside land)");
        log.AppendLine("INFO Material set: " + string.Join(", ", renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Select(m => m.name).Distinct()));
        log.AppendLine("INFO Sprite sorting: " + string.Join(", ", sprites.GroupBy(s => s.sortingLayerName + ":" + s.sortingOrder).Select(g => g.Key + "=" + g.Count())));

        var waypointRoot = root.transform.Find("Exploration Waypoints");
        Check(waypointRoot, "Exploration Waypoints group exists");
        if (waypointRoot)
        {
            var points = waypointRoot.Cast<Transform>().Select(t => new Destination { name=t.name, point=t.position }).ToList();
            Check(points.Count >= 5, "At least five exploration destinations (" + points.Count + ")");
            var entry = new Destination { name = "Main road entry", point = new Vector2(20, -2) };
            points.Insert(0, entry);
            foreach (var p in points)
            {
                bool open = nav.Open(nav.Cell(p.point));
                Check(nav.Terrain(p.point) && open, p.name + " is walkable at " + p.point.ToString("F2"));
                if (!open)
                    log.AppendLine("INFO Blocking colliders at " + p.name + ": " + string.Join(", ", Physics2D.OverlapCircleAll(nav.Point(nav.Cell(p.point)), .27f).Where(c => !c.isTrigger).Select(c => Path(c.transform))));
            }
            // Incrementally connect the nearest unreached marker to reached markers. Direct
            // queries warm the existing cache; one retry distinguishes cold-cache timing.
            // A successful route must finish near the target and contain only open cells.
            var reachable = new HashSet<int> { 0 };
            var route = new List<Vector2>();
            bool progress = true;
            while (progress)
            {
                progress = false;
                for (int to = 1; to < points.Count; to++)
                {
                    if (reachable.Contains(to)) continue;
                    foreach (int from in reachable.OrderBy(i => Vector2.Distance(points[i].point, points[to].point)).ToArray())
                    {
                        bool found = nav.Find(points[from].point, points[to].point, route);
                        if (!found) found = nav.Find(points[from].point, points[to].point, route);
                        if (found && route.Count > 0 && route.All(p => nav.Open(nav.Cell(p))))
                        {
                            reachable.Add(to); progress = true;
                            log.AppendLine("PASS Route " + points[from].name + " -> " + points[to].name + ": " + route.Count + " steps, " + nav.LastMilliseconds.ToString("F2") + " ms");
                            break;
                        }
                    }
                }
            }
            foreach (int i in Enumerable.Range(1, points.Count - 1))
                Check(reachable.Contains(i), points[i].name + " reachable from main road through waypoint network");
            log.AppendLine("INFO Navigation current budget: " + nav.searchBudgetMilliseconds + "ms; no navigation settings modified");
        }
        log.AppendLine("RESULT " + failures + " failures; " + transforms.Length + " biome objects, " + maps.Length + " tilemaps, " + colliders.Length + " colliders. No scene/assets modified by this validator.");
        return log.ToString();
    }
    sealed class Destination { public string name; public Vector2 point; }
}
