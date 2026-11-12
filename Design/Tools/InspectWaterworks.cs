using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

return InspectWaterworks.Main();

public static class InspectWaterworks
{
    public static string Main()
    {
        var sb = new StringBuilder();

        // 1. Check all existing EnemyArchetypes and their sprite paths
        sb.AppendLine("=== EXISTING ENEMY ARCHETYPES ===");
        foreach (var g in AssetDatabase.FindAssets("t:EnemyArchetype"))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ea = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            var spPath = ea.sprite ? AssetDatabase.GetAssetPath(ea.sprite) : "null";
            sb.AppendLine($"  {path}: name='{ea.displayName}', behavior={ea.behavior}, hp={ea.health}, dmg={ea.damage}, sprite={spPath} ({ea.sprite?.name})");
        }

        // 2. Inspect Grid/HiddenPondClearing hierarchy and tilemaps
        sb.AppendLine("\n=== HIDDEN POND CLEARING HIERARCHY ===");
        var hpc = GameObject.Find("Grid/HiddenPondClearing") ?? GameObject.Find("EnvironmentExpansion/HiddenPondClearing");
        if (hpc)
        {
            foreach (var t in hpc.GetComponentsInChildren<Transform>(true))
            {
                var tm = t.GetComponent<Tilemap>();
                var sr = t.GetComponent<SpriteRenderer>();
                var col = t.GetComponent<Collider2D>();
                if (tm || sr || col || t.parent == hpc.transform)
                {
                    sb.AppendLine($"  {GetPath(t)} pos={t.position} active={t.gameObject.activeInHierarchy} tm={(tm ? tm.GetUsedTilesCount().ToString() : "-")} sr={(sr && sr.sprite ? sr.sprite.name : "-")} col={(col ? col.GetType().Name : "-")}");
                }
            }
        }

        // 3. Inspect EnvironmentExpansion/DrownedGrove hierarchy
        sb.AppendLine("\n=== DROWNED GROVE HIERARCHY ===");
        var dg = GameObject.Find("EnvironmentExpansion/DrownedGrove");
        if (dg)
        {
            for (int i = 0; i < dg.transform.childCount; i++)
            {
                var ch = dg.transform.GetChild(i);
                sb.AppendLine($"  Group {ch.name} ({ch.childCount} children):");
                for (int j = 0; j < Mathf.Min(ch.childCount, 25); j++)
                {
                    var sub = ch.GetChild(j);
                    var sr = sub.GetComponent<SpriteRenderer>();
                    var col = sub.GetComponent<Collider2D>();
                    sb.AppendLine($"    - {sub.name} @ {sub.position} sr={(sr && sr.sprite ? sr.sprite.name : "-")} col={(col ? col.GetType().Name : "-")}");
                }
            }
        }

        return sb.ToString();
    }

    static string GetPath(Transform t) => t.parent ? GetPath(t.parent) + "/" + t.name : t.name;
}
