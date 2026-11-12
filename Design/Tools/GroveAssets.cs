using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.Tilemaps;using UnityEditor;
public static class GroveAssets {
 public static string Main(){var p="Design/DrownedGrove/AssetCatalog.txt";var lines=new System.Collections.Generic.List<string>();
 foreach(var path in new[]{"Assets/PixelCrawler/Environment/HiddenPondClearing/Decor.png","Assets/PixelCrawler/Environment/HiddenPondClearing/Vegetation.png","Assets/PixelCrawler/Environment/HiddenPondClearing/BuildingProps.png","Assets/PixelCrawler/Environment/HiddenPondClearing/Bridges.png","Assets/GoblinOutpost/Art/DungeonProps.png","Assets/RoadsideRestStop/Art/Furniture.png","Assets/AshenFrontier/Art/Farm.png"})foreach(var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())lines.Add(path+"#"+s.name+" rect="+s.rect+" size="+s.bounds.size+" pivot="+s.pivot);
 File.WriteAllLines(p,lines);
 var n=UnityEngine.Object.FindFirstObjectByType<ForestNavigation>();Physics2D.SyncTransforms();n.Invalidate();var map=new System.Collections.Generic.List<string>();
 for(int y=24;y>=1;y--){string row=y.ToString("00")+" ";for(int x=-32;x<=-8;x++){var w=new Vector2(x+.5f,y+.5f);var hits=Physics2D.OverlapCircleAll(w,.28f);char ch=n.Open(n.Cell(w))?'.':'#';if(hits.Any(c=>c.name.Contains("Tree")))ch='T';row+=ch;}map.Add(row);}File.WriteAllLines("Design/DrownedGrove/WalkabilityBefore.txt",map);
 return string.Join("\n",map);
 }
}
