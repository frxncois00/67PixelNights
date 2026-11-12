using System;using System.Linq;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEngine.Tilemaps;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;
public static class BuildGrove {
 const string Root="Assets/DrownedGrove";
 const string Pond="Assets/PixelCrawler/Environment/HiddenPondClearing/";
 static Transform area;static Material lit;static Tilemap ground,water,mud;static ForestNavigation nav;
 static List<string> log=new List<string>();static List<LootChest> chests=new List<LootChest>();
 static string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 static GameObject GO(string name,Transform parent,Vector2 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=new Vector3(p.x,p.y,0);return g;}
 static Transform Group(string name)=>area.Find(name);
 static Sprite S(string path,string name=null)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>name==null||s.name==name);
 static SpriteRenderer Prop(string name,string path,string sprite,Vector2 p,Transform parent,Vector2 size=default,Color? color=null){var g=GO(name,parent,p);var r=g.AddComponent<SpriteRenderer>();r.sprite=S(path,sprite);r.sharedMaterial=lit;r.sortingLayerName="Objects";r.spriteSortPoint=SpriteSortPoint.Pivot;r.color=color??Color.white;if(size!=Vector2.zero){var c=g.AddComponent<BoxCollider2D>();c.size=size;c.offset=new Vector2(0,size.y*.3f);}return r;}
 static GameObject Pref(string path,string name,Vector2 p,Transform parent){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);g.name=name;g.transform.position=p;return g;}
 static Tilemap Map(string name,Transform parent,int order){var g=GO(name,parent,Vector2.zero);var m=g.AddComponent<Tilemap>();var r=g.AddComponent<TilemapRenderer>();r.sortingLayerName="Ground";r.sortingOrder=order;r.sharedMaterial=lit;return m;}
 static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
 static List<Vector2[]> routes=new List<Vector2[]>{
 new[]{new Vector2(-27,2),new Vector2(-27,9),new Vector2(-24.5f,11.5f),new Vector2(-24.5f,16),new Vector2(-22,16.5f),new Vector2(-22,19),new Vector2(-23,21)},
 new[]{new Vector2(-27,9),new Vector2(-24,7),new Vector2(-20,6.5f),new Vector2(-16,6.5f),new Vector2(-12,8),new Vector2(-12,13),new Vector2(-12,16),new Vector2(-13,19),new Vector2(-17,21),new Vector2(-21,21),new Vector2(-23,21)},
 new[]{new Vector2(-24.5f,16),new Vector2(-27.5f,16),new Vector2(-28,13),new Vector2(-27,9)} };
 static Rect[] clearings={new Rect(-29,7,5,4),new Rect(-28.5f,15,5,2.5f),new Rect(-25,19,5.5f,4),new Rect(-13.5f,9,4,4.5f),new Rect(-15,17,4,4)};
 static bool InWork(Vector2 p)=>p.x>=-30&&p.x<=-9&&p.y>=2&&p.y<=24;
 static void ClearTrees(){foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude)){
 if(!r.sprite||!InWork(r.transform.position)||!AssetDatabase.GetAssetPath(r.sprite).Contains("/Trees/"))continue;
 var p=(Vector2)r.transform.position;bool clear=clearings.Any(rect=>rect.Contains(p));foreach(var route in routes)for(int i=1;i<route.Length;i++)if(Segment(p,route[i-1],route[i])<.95f)clear=true;
 if(clear){log.Add("Disabled overlapping tree: "+P(r.transform)+" @ "+p);r.gameObject.SetActive(false);EditorUtility.SetDirty(r.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);}
 }}
 static void Mud(){var cells=new HashSet<Vector3Int>();
 foreach(var route in routes)for(int i=1;i<route.Length;i++){var a=route[i-1];var b=route[i];int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*4);for(int j=0;j<=steps;j++){var p=Vector2.Lerp(a,b,j/(float)steps);var c=mud.WorldToCell(p);cells.Add(c);}}
 foreach(var rect in clearings)for(int x=Mathf.CeilToInt(rect.xMin);x<rect.xMax;x++)for(int y=Mathf.CeilToInt(rect.yMin);y<rect.yMax;y++){var p=new Vector2(x+.5f,y+.5f);if(Vector2.Distance(p,rect.center)<Mathf.Min(rect.width,rect.height)*.6f)cells.Add(new Vector3Int(x,y,0));}
 var ramp=GameObject.Find("Grid/HiddenPondClearing/Terrain/Grass Ramps").GetComponent<Tilemap>();
 foreach(var c in cells.ToArray()){var p=mud.GetCellCenterWorld(c);if(!ground.HasTile(ground.WorldToCell(p))||water.GetColliderType(water.WorldToCell(p))!=Tile.ColliderType.None||ramp.HasTile(ramp.WorldToCell(p))||!InWork(p))cells.Remove(c);}
 foreach(var c in cells){int sx=!cells.Contains(c+Vector3Int.left)?0:!cells.Contains(c+Vector3Int.right)?2:1;int sy=!cells.Contains(c+Vector3Int.down)?3:!cells.Contains(c+Vector3Int.up)?5:4;mud.SetTile(c,AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/GoblinOutpost/Tiles/DirtEdge_{sx}_{sy}.asset"));}
 mud.color=new Color(.62f,.76f,.70f,.85f);log.Add("Muddy trail/clearing overlay: "+cells.Count+" cells; existing path and terrain tiles retained.");
 }
 static void Lantern(string name,Vector2 p,Transform parent,bool glow){var g=Pref("Assets/RoadsideRestStop/Prefabs/LanternPost.prefab",name,p,parent);foreach(var r in g.GetComponentsInChildren<SpriteRenderer>())r.color=new Color(.67f,.95f,.89f);foreach(var l in g.GetComponentsInChildren<Light2D>()){l.color=new Color(.25f,.9f,.75f);l.intensity=.65f;l.pointLightOuterRadius=2.6f;l.enabled=glow;}}
 static void Pillar(string name,Vector2 p,Transform parent)=>Prop(name,Pond+"BuildingProps.png","OutpostPillar",p,parent,new Vector2(.65f,.4f),new Color(.72f,.91f,.82f));
 static void Chest(string name,Vector2 p){var g=Pref("Assets/Prefabs/Exploration/LootChest.prefab",name,p,Group("Loot"));var chest=g.GetComponent<LootChest>();chests.Add(chest);}
 static EnemyController Enemy(string id,string title,string source,float hp,float damage){var type=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/AshenFrontier/Enemies/"+source+".asset"));type.name=id;type.id=id;type.displayName=title;type.health=hp;type.damage=damage;type.tint=new Color(.66f,.92f,.88f);type.firstNight=99;type.spawnWeight=0;type.detectionRange=5;type.windup=Mathf.Max(type.windup,.5f);AssetDatabase.CreateAsset(type,Root+"/Enemies/"+id+".asset");
 string prefab=source=="rogue_captain"?"RogueCaptain":source=="rogue_raider"?"RogueRaider":"RogueScout";var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AshenFrontier/Prefabs/"+prefab+".prefab"));go.name=title;var ec=go.GetComponent<EnemyController>();ec.archetype=type;ec.activeDuringDaytime=true;ec.visual.color=type.tint;var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+id+".prefab");UnityEngine.Object.DestroyImmediate(go);return saved.GetComponent<EnemyController>();}
 static void Fire(Vector2 p,Transform parent){string path="Assets/PixelCrawler/Bonfire/Bonfire_02-Sheet.png";var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.rect.x).ToArray();var r=Prop("Small Hermit Fire",path,sprites[0].name,p,parent,new Vector2(.35f,.25f));var clip=new AnimationClip{frameRate=8};var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},sprites.Select((s,i)=>new ObjectReferenceKeyframe{time=i/8f,value=s}).Concat(new[]{new ObjectReferenceKeyframe{time=sprites.Length/8f,value=sprites[0]}}).ToArray());AssetDatabase.CreateAsset(clip,Root+"/SmallFire.anim");var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/SmallFire.controller");controller.AddMotion(clip);r.gameObject.AddComponent<Animator>().runtimeAnimatorController=controller;}
 public static string Main(){if(Application.isPlaying)throw new Exception("Edit mode only");if(GameObject.Find("EnvironmentExpansion/DrownedGrove"))throw new Exception("Grove exists: do not duplicate");
 Directory.CreateDirectory(Root+"/Enemies");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
 lit=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestDayNightLit.mat");ground=GameObject.Find("Grid/Tilemaps/Ground").GetComponent<Tilemap>();water=GameObject.Find("Grid/HiddenPondClearing/Water/Water").GetComponent<Tilemap>();nav=UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
 area=GO("DrownedGrove",GameObject.Find("EnvironmentExpansion").transform,Vector2.zero).transform;
 foreach(var n in new[]{"Tilemaps","FishermansLanding","SunkenShrine","PlankBridges","MossyRuins","HermitsCabin","Vegetation","Decorations","Loot","SpawnPoints","Enemies"})GO(n,area,Vector2.zero);
 var grid=Group("Tilemaps").gameObject.AddComponent<Grid>();grid.cellSize=Vector3.one;mud=Map("Muddy Pilgrim Trails",grid.transform,9);
 ClearTrees();
 // One narrow upper crossing adds a loop without removing either original bridge.
 var bridge=Map("Northern Plank Crossing",Group("PlankBridges"),10);var bg=Group("PlankBridges").gameObject.AddComponent<Grid>();bg.cellSize=new Vector3(.25f,.25f,1);
 var plank=ScriptableObject.CreateInstance<Tile>();plank.sprite=S(Pond+"Bridges.png","SolidPlank");plank.colliderType=Tile.ColliderType.None;AssetDatabase.CreateAsset(plank,Root+"/BridgePlank.asset");
 for(int x=-80;x<-66;x++)for(int y=80;y<88;y++)bridge.SetTile(new Vector3Int(x,y,0),plank);
 for(int y=20;y<=21;y++){var c=new Vector3Int(-19,y,0);log.Add("New bridge water opening "+c+" previous="+water.GetTile(c).name);water.SetTile(c,AssetDatabase.LoadAssetAtPath<TileBase>("Assets/ForestTiles/HiddenPondPlateau/WaterUnderBridge.asset"));}
 var boundary=GameObject.Find("Grid/HiddenPondClearing/Terrain/Cliff Boundary Grid/Cliffs - Side Boundaries").GetComponent<Tilemap>();foreach(var c in boundary.cellBounds.allPositionsWithin){var p=boundary.GetCellCenterWorld(c);if(p.x>=-20&&p.x<=-16.5f&&p.y>=20&&p.y<22&&boundary.HasTile(c)){boundary.SetTile(c,null);log.Add("Bridge bank opening in quarter-cell cliff boundary "+c);}}
 Mud();
 var landing=Group("FishermansLanding");
 Prop("Warning Totem",Pond+"BuildingProps.png","OutpostTimberPost",new Vector2(-28.5f,4.5f),landing,new Vector2(.25f,.25f));
 Prop("Warning Rune", "Assets/GoblinOutpost/Art/DungeonProps.png","RuneStone",new Vector2(-28.5f,5),landing);
 Lantern("Entrance Marsh Lantern",new Vector2(-25.5f,5),landing,true);
 // Fixed bank-side dock; its broken stub is decorative, with the intact bridges as detours.
 for(int i=0;i<3;i++)Prop("Landing Dock Planks "+i,Pond+"Bridges.png","SouthBridge",new Vector2(-25.5f+i*.5f,9),landing);
 Prop("Broken Dock Stub",Pond+"Bridges.png","Plank",new Vector2(-23.5f,9),landing);
 Prop("Tackle Crate",Pond+"BuildingProps.png","CartBed",new Vector2(-28.5f,9.5f),landing,new Vector2(1.1f,.45f));
 Prop("Fishermans Bench",Pond+"BuildingProps.png","OutpostPlankBench",new Vector2(-28,7.5f),landing,new Vector2(1.25f,.35f));
 Prop("Abandoned Driftwood",Pond+"Resources.png","Log",new Vector2(-25,8),landing);
 var shrine=Group("SunkenShrine");
 Pillar("Shrine Pillar West",new Vector2(-13.5f,12),shrine);Pillar("Shrine Pillar East",new Vector2(-10,12.5f),shrine);
 Prop("Moss Altar",Pond+"BuildingProps.png","CartBed",new Vector2(-12,12.5f),shrine,new Vector2(1.1f,.45f),new Color(.65f,.84f,.75f));
 Prop("Sanctuary Rune", "Assets/GoblinOutpost/Art/DungeonProps.png","RuneStone",new Vector2(-12,12.8f),shrine,new Vector2(.3f,.2f),new Color(.55f,1,.9f));
 Lantern("Shrine Lantern",new Vector2(-10,10.5f),shrine,true);Lantern("Upper Shrine Beacon",new Vector2(-14,18),shrine,false);
 for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10;Prop("Shrine Circle Stone "+i,Pond+"Decor.png","Rock",new Vector2(-12+Mathf.Cos(a)*1.6f,10.5f+Mathf.Sin(a)*1.2f),shrine);}
 Chest("Chest_SunkenShrine",new Vector2(-11,12));
 var ruins=Group("MossyRuins");
 Pillar("Ruin Pillar West",new Vector2(-28.5f,17),ruins);Pillar("Ruin Pillar East",new Vector2(-26,17),ruins);
 Prop("Sunken Arch",Pond+"BuildingProps.png","OutpostStoneArch",new Vector2(-27.5f,17),ruins,new Vector2(.6f,.3f),new Color(.75f,.9f,.85f));
 for(int i=0;i<3;i++)Prop("Broken Moss Block "+i,Pond+"BuildingProps.png","RestWellStone",new Vector2(-29+i*.5f,15),ruins,new Vector2(.35f,.2f),new Color(.68f,.88f,.78f));
 Chest("Chest_MossyRuins",new Vector2(-28,16));
 var cabin=Group("HermitsCabin");
 Lantern("Cabin Spirit Lantern",new Vector2(-24.5f,22.5f),cabin,true);Lantern("Cabin Stair Lantern",new Vector2(-23.5f,19),cabin,false);
 for(int i=0;i<4;i++)Pref("Assets/GoblinOutpost/Prefabs/TimberFenceSegment.prefab","Cabin Garden Fence "+i,new Vector2(-24+i,23),cabin);
 Prop("Stump Offering Table","Assets/Tiny Swords/Terrain/Resources/Wood/Wood Resource/Wood Resource.png",null,new Vector2(-24,20),cabin);
 Fire(new Vector2(-20.5f,22.5f),cabin);Chest("Chest_HermitReward",new Vector2(-21,23));
 Chest("Cache_HiddenReeds",new Vector2(-29,12.5f));
 // Existing mining and scrap interactions; no new resource types.
 Pref("Assets/AshenFrontier/Prefabs/QuarryStoneNode.prefab","Ruins_MineableStone_01",new Vector2(-28.5f,14),ruins);
 Pref("Assets/AshenFrontier/Prefabs/QuarryStoneNode.prefab","Ruins_MineableStone_02",new Vector2(-26,15),ruins);
 Pref("Assets/AshenFrontier/Prefabs/ScrapPileNode.prefab","Landing_Salvage",new Vector2(-28,8.5f),landing);
 Pref("Assets/AshenFrontier/Prefabs/ScrapPileNode.prefab","Shrine_Salvage",new Vector2(-10.5f,9),shrine);
 // New waterline vegetation, spaced and snapped to half cells, without blocking movement.
 var random=new System.Random(8217);int reeds=0,pads=0;
 foreach(var c in water.cellBounds.allPositionsWithin){var p=water.GetCellCenterWorld(c);if(!InWork(p)||water.GetColliderType(c)==Tile.ColliderType.None)continue;
 bool bank=new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down}.Any(d=>water.HasTile(c+d)&&water.GetColliderType(c+d)==Tile.ColliderType.None);
 if(bank&&random.NextDouble()<.5){Prop("Waterline Reeds "+(++reeds),Pond+"Vegetation.png","ReedClump",(Vector2)p,Group("Vegetation"),default,new Color(.68f,.9f,.77f));}
 else if(random.NextDouble()<.22){var r=Prop("Lily Pads "+(++pads),Pond+"Decor.png","LeafPad",(Vector2)p,Group("Vegetation"));r.sortingLayerName="Ground";r.sortingOrder=8;}
 }
 foreach(var p in new[]{new Vector2(-26,10),new Vector2(-28.5f,13),new Vector2(-23,16),new Vector2(-24,19),new Vector2(-20,22.5f),new Vector2(-15,19.5f),new Vector2(-10.5f,15),new Vector2(-13.5f,8)}){
 for(int i=0;i<3;i++)Prop("Glowing Grove Mushrooms",Pond+"Decor.png","Mushrooms",p+new Vector2(i*.5f,(i%2)*.5f),Group("Vegetation"),default,new Color(.45f,1,.85f));
 }
 for(int i=0;i<4;i++)Prop("Hermit Herb Patch "+i,Pond+"Vegetation.png","Fern",new Vector2(-23+i*.5f,22.5f),cabin);
 // Matching original dead-tree prefabs, cool local tint only.
 foreach(var p in new[]{new Vector2(-30,11),new Vector2(-29,15),new Vector2(-17,8),new Vector2(-14,16),new Vector2(-20,23)}){var g=Pref("Assets/AshenFrontier/Prefabs/DeadTree_Small.prefab","Drowned Deadwood",p,Group("Vegetation"));foreach(var r in g.GetComponentsInChildren<SpriteRenderer>())r.color=new Color(.6f,.78f,.72f);}
 var scout=Enemy("bog_scout","Bog Watcher","rogue_scout",38,8);var raider=Enemy("bog_raider","Mire Cultist","rogue_raider",48,10);var boss=Enemy("marsh_warden","Marsh Warden","rogue_captain",125,16);
 var spawner=UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();var encounter=area.gameObject.AddComponent<AshenFrontierEncounterController>();encounter.existingSpawner=spawner;encounter.sharedGrid=GameObject.Find("Grid").GetComponent<Grid>();encounter.groundTilemap=ground;encounter.playerInteraction=GameObject.Find("Player").GetComponent<PlayerInteractionController>();encounter.spawnedContainer=Group("Enemies");encounter.tilemaps=area.GetComponentsInChildren<Tilemap>();encounter.chests=chests.ToArray();encounter.maxConcurrentLocalEnemies=5;encounter.despawnDistance=23;encounter.tickInterval=.5f;encounter.captainRewardPrefix="MarshWardenSpoils_";encounter.captainDefeatedNotice="MARSH WARDEN DEFEATED! THE GROVE FALLS SILENT.";encounter.respawnCaptainOnNewDay=false;encounter.pickupMaterial=lit;
 var old=GameObject.Find("EnvironmentExpansion/AshenFrontier").GetComponent<AshenFrontierEncounterController>();encounter.captainDropItems=old.captainDropItems;encounter.captainDropCounts=old.captainDropCounts;
 Vector2[] points={new Vector2(-27,7),new Vector2(-26,10),new Vector2(-25,11.5f),new Vector2(-24,15.5f),new Vector2(-27,16),new Vector2(-29,13),new Vector2(-15.5f,7),new Vector2(-12,9.5f),new Vector2(-11,11),new Vector2(-12,17),new Vector2(-15,21),new Vector2(-23,21),new Vector2(-21,19.5f)};
 encounter.rogueSlots=points.Select((p,i)=>new AshenFrontierEncounterController.SpawnSlot{label=i==11?"Marsh Warden":"Grove Guard "+(i+1).ToString("00"),point=GO(i==11?"Spawn_MarshWarden":"Spawn_Guard_"+(i+1).ToString("00"),Group("SpawnPoints"),p).transform,prefab=i==11?boss:i%3==0?raider:scout,isCaptain=i==11,activationRadius=i==11?5.5f:5,leashRadius=i==11?6:8}).ToArray();
 encounter.nightSpawnPoints=Array.Empty<Transform>();
 var exploration=UnityEngine.Object.FindAnyObjectByType<ExplorationManager>();exploration.regions=exploration.regions.Concat(new[]{new ExplorationManager.Region{id="drowned_grove",title="The Drowned Grove",bounds=new Rect(-30,3,21,21)}}).ToArray();EditorUtility.SetDirty(exploration);
 foreach(var tm in new[]{water,boundary}){tm.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();EditorUtility.SetDirty(tm);PrefabUtility.RecordPrefabInstancePropertyModifications(tm);}
 Physics2D.SyncTransforms();nav.Invalidate();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(area.gameObject.scene);EditorSceneManager.SaveScene(area.gameObject.scene);
 log.Add("Created "+area.GetComponentsInChildren<Transform>().Length+" objects; "+reeds+" reeds; "+pads+" lily pads; 13 encounter markers, local cap 5, shared cap 10.");File.WriteAllLines("Design/DrownedGrove/BuildLog.txt",log);return string.Join("\n",log);
 }
}
