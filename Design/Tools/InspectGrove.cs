using System;using System.Linq;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEngine.Tilemaps;using UnityEditor;
public static class InspectGrove {
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static string Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var lines=new List<string>();
 foreach(var t in transforms){
  if(t.name=="Grid"||t.GetComponent<Tilemap>()|| (t.position.x>=-34&&t.position.x<=-8&&t.position.y>=1&&t.position.y<=29&&t.GetComponent<Renderer>())||t.GetComponent<ExplorationManager>()||t.GetComponent<EnemySpawner>()){
   lines.Add(PathOf(t)+" pos="+t.position+" scale="+t.lossyScale+" active="+t.gameObject.activeInHierarchy+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
   var grid=t.GetComponent<Grid>();if(grid)lines.Add(" GRID "+grid.cellSize);
   var sr=t.GetComponent<SpriteRenderer>();if(sr&&sr.sprite){var imp=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sr.sprite));lines.Add(" SPRITE "+AssetDatabase.GetAssetPath(sr.sprite)+"#"+sr.sprite.name+" ppu="+sr.sprite.pixelsPerUnit+" filter="+imp.filterMode+" layer="+sr.sortingLayerName+":"+sr.sortingOrder+" pivot="+sr.spriteSortPoint+" mat="+AssetDatabase.GetAssetPath(sr.sharedMaterial));}
   var tm=t.GetComponent<Tilemap>();if(tm){var r=tm.GetComponent<TilemapRenderer>();var tiles=new TileBase[tm.GetUsedTilesCount()];tm.GetUsedTilesNonAlloc(tiles);lines.Add(" TILEMAP bounds="+tm.cellBounds+" tiles="+tm.GetTilesBlock(tm.cellBounds).Count(x=>x)+" sorting="+(r?r.sortingLayerName+":"+r.sortingOrder+" mode="+r.mode:"collision only")+" assets="+string.Join(";",tiles.Select(x=>AssetDatabase.GetAssetPath(x))));
    var cells=new List<string>{"x,y,worldX,worldY,tile,collider"};foreach(var c in tm.cellBounds.allPositionsWithin)if(tm.HasTile(c)){var w=tm.GetCellCenterWorld(c);cells.Add($"{c.x},{c.y},{w.x},{w.y},{tm.GetTile(c).name},{tm.GetColliderType(c)}");}File.WriteAllLines("Design/DrownedGrove/Cells-"+t.GetEntityId().ToString().Replace(":","_")+".csv",cells);
   }
   foreach(var col in t.GetComponents<Collider2D>())lines.Add(" COLLIDER "+col.GetType().Name+" bounds="+col.bounds+" enabled="+col.enabled+" trigger="+col.isTrigger);
  }
 }
 lines.Add("SORTING "+string.Join(",",SortingLayer.layers.Select(x=>x.name)));
 foreach(var g in transforms.Where(t=>t.GetComponent<ExplorationManager>()||t.GetComponent<EnemySpawner>()||t.GetComponent<ForestNavigation>()))lines.Add("CONFIG "+PathOf(g)+" "+string.Join("\n",g.GetComponents<MonoBehaviour>().Select(c=>c?EditorJsonUtility.ToJson(c):"missing")));
 File.WriteAllLines("Design/DrownedGrove/Inspection.txt",lines);
 // Snapshot every pre-existing component by GlobalObjectId to prove later preservation.
 var records=new List<string>();foreach(var t in transforms)foreach(var c in t.GetComponents<Component>())if(c)records.Add(GlobalObjectId.GetGlobalObjectIdSlow(c)+"\t"+PathOf(t)+"\t"+EditorJsonUtility.ToJson(c));File.WriteAllLines("Design/Backups/DrownedGrove/Components.jsonl",records);
 var sv=SceneView.lastActiveSceneView;if(sv){sv.in2DMode=true;sv.LookAt(new Vector3(-22,14,0),Quaternion.identity,16,true,true);}
 return "Inspected "+transforms.Length+" objects; wrote detailed tile, collider, asset and preservation snapshots.";
 }
}



