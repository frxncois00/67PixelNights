using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

return BuildSunkenWaterworks.Main();

public static class BuildSunkenWaterworks
{
    public static string Main()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var groveRoot = GameObject.Find("EnvironmentExpansion/DrownedGrove");
        if (groveRoot == null)
            return "ERROR: EnvironmentExpansion/DrownedGrove not found!";

        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        var nav = Object.FindAnyObjectByType<ForestNavigation>();
        var waterTm = GameObject.Find("Grid/HiddenPondClearing/Water/Water")?.GetComponent<Tilemap>();
        var groundTm = GameObject.Find("Grid/HiddenPondClearing/Terrain/Ground")?.GetComponent<Tilemap>();
        var sideBoundsTm = GameObject.Find("Grid/HiddenPondClearing/Terrain/Cliff Boundary Grid/Cliffs - Side Boundaries")?.GetComponent<Tilemap>();
        var walkableWaterTile = waterTm.GetTile(waterTm.WorldToCell(new Vector3(-18.5f, 20.5f, 0f)));
        var defaultWaterTile = waterTm.GetTile(waterTm.WorldToCell(new Vector3(-20.5f, 12.5f, 0f)));

        // 1. Load generated sprites
        var propSprites = LoadSpriteDict("Assets/DrownedGrove/Art/WaterworksProps.png");
        var reedIdle = LoadSpriteArray("Assets/DrownedGrove/Art/ReedStalker_Idle.png");
        var reedWalk = LoadSpriteArray("Assets/DrownedGrove/Art/ReedStalker_Walk.png");
        var crabIdle = LoadSpriteArray("Assets/DrownedGrove/Art/SluiceCrab_Idle.png");
        var crabWalk = LoadSpriteArray("Assets/DrownedGrove/Art/SluiceCrab_Walk.png");
        var eelIdle = LoadSpriteArray("Assets/DrownedGrove/Art/LanternEel_Idle.png");
        var eelWalk = LoadSpriteArray("Assets/DrownedGrove/Art/LanternEel_Walk.png");
        var bossIdle = LoadSpriteArray("Assets/DrownedGrove/Art/FloodgateKeeper_Idle.png");
        var bossWalk = LoadSpriteArray("Assets/DrownedGrove/Art/FloodgateKeeper_Walk.png");

        var groveProps = LoadSpriteDict("Assets/DrownedGrove/Art/GroveProps.png");
        var frontierProps = LoadSpriteDict("Assets/AshenFrontier/Art/FrontierProps.png");

        // 2. Create/Update the 4 Lake Creature Archetypes & Prefabs
        var reedArch = CreateOrUpdateArchetype(
            "Assets/DrownedGrove/Enemies/reed_stalker.asset",
            "reed_stalker", "Reed Stalker", EnemyBehaviorProfile.Crawler,
            health: 42f, damage: 10f, patrolSpeed: 1.15f, chaseSpeed: 2.05f,
            detectionRange: 4.6f, attackRange: 0.95f, windup: 0.60f, recovery: 0.55f,
            cooldown: 2.1f, knockbackRes: 0.15f, lungeDist: 1.55f,
            idle: reedIdle, walk: reedWalk, fps: 7f);

        var crabArch = CreateOrUpdateArchetype(
            "Assets/DrownedGrove/Enemies/sluice_crab.asset",
            "sluice_crab", "Sluice Crab", EnemyBehaviorProfile.Stalker,
            health: 64f, damage: 12f, patrolSpeed: 0.90f, chaseSpeed: 1.45f,
            detectionRange: 4.5f, attackRange: 1.15f, windup: 0.68f, recovery: 1.65f,
            cooldown: 2.4f, knockbackRes: 0.55f, lungeDist: 1.0f,
            idle: crabIdle, walk: crabWalk, fps: 6f);

        var eelArch = CreateOrUpdateArchetype(
            "Assets/DrownedGrove/Enemies/lantern_eel.asset",
            "lantern_eel", "Lantern Eel", EnemyBehaviorProfile.Stalker,
            health: 46f, damage: 12f, patrolSpeed: 0.65f, chaseSpeed: 1.10f,
            detectionRange: 4.8f, attackRange: 2.45f, windup: 0.85f, recovery: 2.20f,
            cooldown: 3.1f, knockbackRes: 0.75f, lungeDist: 1.0f,
            idle: eelIdle, walk: eelWalk, fps: 7f);

        var bossArch = CreateOrUpdateArchetype(
            "Assets/DrownedGrove/Enemies/floodgate_keeper.asset",
            "floodgate_keeper", "The Floodgate Keeper", EnemyBehaviorProfile.Brute,
            health: 135f, damage: 16f, patrolSpeed: 0.95f, chaseSpeed: 1.65f,
            detectionRange: 5.4f, attackRange: 2.65f, windup: 0.85f, recovery: 2.20f,
            cooldown: 2.9f, knockbackRes: 0.70f, lungeDist: 1.4f,
            idle: bossIdle, walk: bossWalk, fps: 6f);

        Sprite reedClumpSpr = groveProps.ContainsKey("ReedClump") ? groveProps["ReedClump"] : null;
        Sprite rippleSpr = propSprites["WaterRippleRing"];
        Sprite nightGlowSpr = propSprites["NightTellGlow"];

        var reedPrefab = CreateLakeCreaturePrefab(
            "Assets/DrownedGrove/Prefabs/ReedStalker.prefab", reedArch,
            SunkenWaterworksCreature.CreatureKind.ReedStalker,
            new Color(0.75f, 1f, 0.35f, 0.9f), reedClumpSpr, rippleSpr, nightGlowSpr,
            colliderRadius: 0.24f, maxChase: 5.5f);

        var crabPrefab = CreateLakeCreaturePrefab(
            "Assets/DrownedGrove/Prefabs/SluiceCrab.prefab", crabArch,
            SunkenWaterworksCreature.CreatureKind.SluiceCrab,
            new Color(0.35f, 0.95f, 1f, 0.9f), null, rippleSpr, nightGlowSpr,
            colliderRadius: 0.28f, maxChase: 5.5f);

        var eelPrefab = CreateLakeCreaturePrefab(
            "Assets/DrownedGrove/Prefabs/LanternEel.prefab", eelArch,
            SunkenWaterworksCreature.CreatureKind.LanternEel,
            new Color(1f, 0.92f, 0.45f, 0.95f), null, rippleSpr, nightGlowSpr,
            colliderRadius: 0.36f, maxChase: 1.65f);

        var bossPrefab = CreateLakeCreaturePrefab(
            "Assets/DrownedGrove/Prefabs/FloodgateKeeper.prefab", bossArch,
            SunkenWaterworksCreature.CreatureKind.FloodgateKeeper,
            new Color(0.45f, 0.98f, 1f, 0.95f), null, rippleSpr, nightGlowSpr,
            colliderRadius: 0.36f, maxChase: 6.5f);

        // 3. Configure Stepping-Stone Crossings & Clear Blocking Pond Boulders
        // Disable the two blocking water rocks between Central Islet and East Bank, the channel water rock, and the two East Bank arena boulders
        DisablePondObject("Grid/HiddenPondClearing/Props/Tiny Swords Water Stone 0");
        DisablePondObject("Grid/HiddenPondClearing/Props/Tiny Swords Water Stone 1");
        DisablePondObject("Grid/HiddenPondClearing/Props/Tiny Swords Water Stone 3");
        DisablePondObject("Grid/HiddenPondClearing/Props/Mossy Boulders", new Vector2(-14.0f, 9.2f));
        DisablePondObject("Grid/HiddenPondClearing/Props/Mossy Boulders", new Vector2(-11.0f, 12.0f));

        // Make Central Islet (The Gauge Shrine) stepping stones walkable on Water tilemap:
        // East crossing: world (-16, 13), (-15, 13), (-16, 12), (-15, 12)
        // South crossing: world (-17, 11), (-16, 11), (-17, 10)
        // Plus shallow bank cells for Lantern Eel at (-16, 12) & (-14, 11)
        Vector2Int[] steppingStoneWorldCells = new Vector2Int[]
        {
            new Vector2Int(-16, 13), new Vector2Int(-15, 13),
            new Vector2Int(-16, 12), new Vector2Int(-15, 12),
            new Vector2Int(-17, 11), new Vector2Int(-16, 11),
            new Vector2Int(-17, 10)
        };
        foreach (var wc in steppingStoneWorldCells)
        {
            Vector3 wCenter = new Vector3(wc.x + 0.5f, wc.y + 0.5f, 0f);
            Vector3Int cell = waterTm.WorldToCell(wCenter);
            if (waterTm.HasTile(cell))
            {
                waterTm.SetTile(cell, walkableWaterTile);
                waterTm.SetColliderType(cell, Tile.ColliderType.None);
            }
        }

        // Keep dry ground at foot of stairs (-21..-20, 15..16) as walkableWaterTile, and set bounded spillway channel water cells (-19..-17, 15..16) to default blocking water initially (drained when wheel turns)
        for (int wx = -21; wx <= -20; wx++)
        {
            for (int wy = 15; wy <= 16; wy++)
            {
                Vector3Int c = waterTm.WorldToCell(new Vector3(wx + 0.5f, wy + 0.5f, 0f));
                if (waterTm.HasTile(c))
                {
                    waterTm.SetTile(c, walkableWaterTile);
                    waterTm.SetColliderType(c, Tile.ColliderType.None);
                }
            }
        }
        var boundedChannelCells = new List<Vector3Int>();
        for (int wx = -19; wx <= -17; wx++)
        {
            for (int wy = 15; wy <= 16; wy++)
            {
                Vector3 wCenter = new Vector3(wx + 0.5f, wy + 0.5f, 0f);
                Vector3Int c = waterTm.WorldToCell(wCenter);
                boundedChannelCells.Add(c);
                if (walkableWaterTile != null && waterTm.HasTile(c))
                {
                    waterTm.SetTile(c, walkableWaterTile);
                    waterTm.SetColliderType(c, Tile.ColliderType.None);
                }
            }
        }
        waterTm.RefreshAllTiles();
        var waterCol = waterTm.GetComponent<TilemapCollider2D>();
        if (waterCol != null)
            waterCol.ProcessTilemapChanges();

        // 4. Rebuild Landmark Groups under EnvironmentExpansion/DrownedGrove (clean up any legacy group names first)
        RemoveGroupIfPresent(groveRoot.transform, "MossyRuins");
        RemoveGroupIfPresent(groveRoot.transform, "HermitsCabin");
        RemoveGroupIfPresent(groveRoot.transform, "SunkenShrine");
        var landingGroup = ResetGroup(groveRoot.transform, "FishermansLanding");
        var shaftGroup = ResetGroup(groveRoot.transform, "TheSluiceShaft");
        var cabinGroup = ResetGroup(groveRoot.transform, "PumpkeepersCabin");
        var stationGroup = ResetGroup(groveRoot.transform, "FloodedPumpStation");
        var isletGroup = ResetGroup(groveRoot.transform, "TheGaugeShrine");
        var lootGroup = ResetGroup(groveRoot.transform, "Loot");
        var spawnGroup = ResetGroup(groveRoot.transform, "SpawnPoints");
        var enemiesGroup = ResetGroup(groveRoot.transform, "Enemies");

        var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DrownedGrove/Prefabs/GroveLootChest.prefab");
        var lanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DrownedGrove/Prefabs/MarshLantern.prefab");
        var stoneNodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DrownedGrove/Prefabs/MossyMineableStone.prefab");

        var woodItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/wood_log.asset");
        var stoneItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/stone.asset");
        var scrapItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/scrap.asset");
        var berryItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/berries.asset");

        // --- LANDMARK 1: FISHERMAN'S LANDING (South Entrance & West Bank) ---
        SpawnLantern(lanternPrefab, landingGroup, "Entrance Dock Lantern", new Vector3(-25.5f, 5.0f, 0f));
        SpawnProp(landingGroup, "Landing Entrance Post", LoadExternalSprite("Assets/Tiny Swords/Terrain/Decorations/Rocks in the water/Water Rocks_01.png"),
            new Vector3(-28.4f, 4.6f, 0f), false, Vector2.zero, 0);
        // Short Pier extending toward the water at y = 8.9..9.1, x = -25.6..-23.2
        Sprite bridgePlank = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Buildings/TinySwords/SouthBridge.png");
        for (int i = 0; i < 5; i++)
        {
            var plank = SpawnProp(landingGroup, $"Pier Deck Plank {i}", bridgePlank,
                new Vector3(-25.5f + i * 0.52f, 9.0f, 0f), false, Vector2.zero, 12);
            plank.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        }
        // Mooring posts along the pier
        SpawnProp(landingGroup, "Pier Mooring Post NW", propSprites["MooringPost"], new Vector3(-25.4f, 9.38f, 0f), false, Vector2.zero, 2);
        SpawnProp(landingGroup, "Pier Mooring Post NE", propSprites["MooringPost"], new Vector3(-23.8f, 9.38f, 0f), false, Vector2.zero, 2);
        SpawnProp(landingGroup, "Pier Mooring Post SW", propSprites["MooringPost"], new Vector3(-25.4f, 8.55f, 0f), false, Vector2.zero, 2);
        SpawnProp(landingGroup, "Pier Mooring Post SE", propSprites["MooringPost"], new Vector3(-23.8f, 8.55f, 0f), false, Vector2.zero, 2);
        SpawnRipple(landingGroup, "Pier Water Ripple 0", propSprites["WaterRippleRing"], new Vector3(-23.4f, 8.95f, 0f));

        // Broken Boat Ramp & half-sunken skiff at (-24.3, 7.75)
        var ramp = SpawnProp(landingGroup, "Broken Boat Ramp", propSprites["BrokenBoatRamp"], new Vector3(-24.25f, 7.75f, 0f), false, Vector2.zero, 12);
        ramp.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        SpawnRipple(landingGroup, "Boat Ramp Ripple", propSprites["WaterRippleRing"], new Vector3(-23.55f, 7.75f, 0f));

        // Fishing racks, crates & salvage along west edge (leaving x=-27.2..-25.0 clear for combat & retreat)
        SpawnProp(landingGroup, "Fishing Drying Rack North", propSprites["FishingRack"], new Vector3(-28.35f, 8.85f, 0f), true, new Vector2(0.9f, 0.35f), 2);
        SpawnProp(landingGroup, "Fishing Drying Rack South", propSprites["FishingRack"], new Vector3(-28.35f, 7.25f, 0f), true, new Vector2(0.9f, 0.35f), 2);
        if (frontierProps.ContainsKey("Crate"))
            SpawnProp(landingGroup, "Fishermans Tackle Crate", frontierProps["Crate"], new Vector3(-28.45f, 9.75f, 0f), true, new Vector2(0.55f, 0.45f), 2);
        if (frontierProps.ContainsKey("Barrel"))
            SpawnProp(landingGroup, "Salted Bait Barrel", frontierProps["Barrel"], new Vector3(-28.45f, 6.35f, 0f), true, new Vector2(0.5f, 0.4f), 2);
        SpawnSalvagePickup(landingGroup, "Landing_Salvage", new Vector3(-27.7f, 8.1f, 0f), scrapItem, 2);

        // --- LANDMARK 2: THE SLUICE SHAFT (West Ruins Below Cabin) ---
        Sprite pillarSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GoblinOutpost/OutpostPillar.png");
        Sprite archSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GoblinOutpost/OutpostStoneArch.png");
        Sprite blockSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RoadsideRestStop/RestWellStone.png");

        SpawnProp(shaftGroup, "Sluice Pillar West", pillarSpr, new Vector3(-28.5f, 17.0f, 0f), true, new Vector2(0.6f, 0.45f), 3);
        SpawnProp(shaftGroup, "Sluice Pillar East", pillarSpr, new Vector3(-26.0f, 17.0f, 0f), true, new Vector2(0.6f, 0.45f), 3);

        var archGo = SpawnProp(shaftGroup, "The Sluice Shaft Arch", archSpr, new Vector3(-27.25f, 17.0f, 0f), false, Vector2.zero, 4);
        var grateGo = SpawnProp(archGo.transform, "Sluice Shaft Grate", propSprites["SluiceShaftGrate"], new Vector3(0f, -0.05f, 0f), false, Vector2.zero, 5);
        SpawnProp(shaftGroup, "Sluice Depth Marker", propSprites["DepthMarkerPost"], new Vector3(-25.45f, 16.85f, 0f), false, Vector2.zero, 4);
        SpawnProp(shaftGroup, "Sluice Water Stain Moss", propSprites["AlgaePatch"], new Vector3(-27.25f, 16.65f, 0f), false, Vector2.zero, 3);

        // Enclosed Lower Sluice Chamber & Walkable Inspection Walkway (x = -29.0..-25.2, y = 14.8..16.6)
        for (int wx = 0; wx < 4; wx++)
        {
            for (int wy = 0; wy < 2; wy++)
            {
                var slab = SpawnProp(shaftGroup, $"Inspection Walkway Slab {wx}_{wy}", propSprites["WalkwayStoneSlab"],
                    new Vector3(-28.4f + wx * 0.95f, 15.35f + wy * 0.90f, 0f), false, Vector2.zero, 11);
                slab.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
            }
        }
        // Low stone chamber border along the west/southwest edge (leaving east/south walkway wide open)
        SpawnProp(shaftGroup, "Chamber Wall West 0", blockSpr, new Vector3(-29.15f, 16.2f, 0f), true, new Vector2(0.45f, 0.4f), 2);
        SpawnProp(shaftGroup, "Chamber Wall West 1", blockSpr, new Vector3(-29.15f, 15.4f, 0f), true, new Vector2(0.45f, 0.4f), 2);
        SpawnProp(shaftGroup, "Chamber Wall South 0", blockSpr, new Vector3(-28.7f, 14.7f, 0f), true, new Vector2(0.45f, 0.4f), 2);
        SpawnProp(shaftGroup, "Chamber Wall South 1", blockSpr, new Vector3(-28.1f, 14.7f, 0f), true, new Vector2(0.45f, 0.4f), 2);

        var walkwayEntry = new GameObject("WalkwayEntryPoint").transform;
        walkwayEntry.SetParent(shaftGroup, false);
        walkwayEntry.position = new Vector3(-27.25f, 16.15f, 0f);

        var shaftEntrance = archGo.AddComponent<SluiceShaftEntrance>();
        shaftEntrance.grateVisual = grateGo.transform;
        shaftEntrance.walkwayEntryPoint = walkwayEntry;
        shaftEntrance.navigation = nav;

        // Interactive Drainage Wheel on the Inspection Walkway at (-25.65, 16.15)
        var wheelPedestal = SpawnProp(shaftGroup, "Sluice Drainage Wheel", propSprites["DrainageWheelPedestal"],
            new Vector3(-25.65f, 16.15f, 0f), true, new Vector2(0.55f, 0.35f), 4);
        var wheelRotor = SpawnProp(wheelPedestal.transform, "Wheel Rotor", propSprites["DrainageWheelRotor"],
            new Vector3(0f, 0.36f, 0f), false, Vector2.zero, 5);
        var drainageWheel = wheelPedestal.AddComponent<SluiceDrainageWheel>();
        drainageWheel.wheelVisual = wheelRotor.transform;
        drainageWheel.waterTilemap = waterTm;
        drainageWheel.walkableWaterTile = walkableWaterTile;
        drainageWheel.boundedChannelCells = boundedChannelCells.ToArray();
        drainageWheel.navigation = nav;

        // Mineable stone nodes outside the chamber
        SpawnPrefab(stoneNodePrefab, shaftGroup, "Shaft_MineableStone_01", new Vector3(-28.6f, 13.8f, 0f));
        SpawnPrefab(stoneNodePrefab, shaftGroup, "Shaft_MineableStone_02", new Vector3(-26.0f, 14.2f, 0f));

        // --- LANDMARK 3: PUMPKEEPER'S CABIN & BOUNDED SLUICE CHANNEL ---
        SpawnLantern(lanternPrefab, cabinGroup, "Cabin Yard Lantern", new Vector3(-24.5f, 22.4f, 0f));
        SpawnLantern(lanternPrefab, cabinGroup, "Cabin Stair Lantern", new Vector3(-23.5f, 18.9f, 0f));

        // Pump Housing & Flume Pipes leading to Waterfall
        SpawnProp(cabinGroup, "Main Pump Housing", propSprites["PumpHousing"], new Vector3(-23.8f, 22.15f, 0f), true, new Vector2(1.4f, 0.6f), 3);
        for (int p = 0; p < 4; p++)
        {
            SpawnProp(cabinGroup, $"Headwater Flume Pipe {p}", propSprites["TimberFlumePipe"],
                new Vector3(-22.4f + p * 0.95f, 22.15f, 0f), false, Vector2.zero, 2);
        }

        // Pumpkeeper's Workbench & Readable Water-Level Diagram Clue
        var benchSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/GoblinOutpost/OutpostPlankBench.png");
        SpawnProp(cabinGroup, "Pumpkeepers Workbench", benchSpr, new Vector3(-24.1f, 19.95f, 0f), true, new Vector2(0.85f, 0.35f), 2);
        var diagramGo = SpawnProp(cabinGroup, "Waterworks Diagram Board", propSprites["WaterworksDiagramBoard"],
            new Vector3(-23.15f, 20.05f, 0f), true, new Vector2(0.55f, 0.35f), 3);
        diagramGo.AddComponent<WaterworksDiagramClue>();

        // Water-level markings on upper terrace & bounded channel
        var gaugeUpper = SpawnProp(cabinGroup, "Terrace Water Gauge West", propSprites["WaterLevelGaugeMarker"], new Vector3(-21.3f, 18.75f, 0f), false, Vector2.zero, 3);
        var gaugeWaterfall = SpawnProp(cabinGroup, "Terrace Water Gauge East", propSprites["WaterLevelGaugeMarker"], new Vector3(-19.3f, 18.75f, 0f), false, Vector2.zero, 3);

        // Bounded Sluice Spillway Channel at foot of stairs/waterfall (x = -19.5..-16.5, y = 15.5..16.5)
        var channelRoot = new GameObject("BoundedSluiceChannel").transform;
        channelRoot.SetParent(cabinGroup, false);
        channelRoot.position = new Vector3(-18.0f, 16.0f, 0f);

        var floodedVis0 = SpawnProp(channelRoot, "Spillway Flooded West", propSprites["BoundedSpillwayFlooded"], new Vector3(-0.75f, 0f, 0f), false, Vector2.zero, 11);
        floodedVis0.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        var floodedVis1 = SpawnProp(channelRoot, "Spillway Flooded Center", propSprites["BoundedSpillwayFlooded"], new Vector3(0.0f, 0f, 0f), true, new Vector2(3.0f, 1.1f), 11);
        floodedVis1.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        var floodedVis2 = SpawnProp(channelRoot, "Spillway Flooded East", propSprites["BoundedSpillwayFlooded"], new Vector3(0.75f, 0f, 0f), false, Vector2.zero, 11);
        floodedVis2.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";

        var drainedVis0 = SpawnProp(channelRoot, "Spillway Drained West", propSprites["BoundedSpillwayDrained"], new Vector3(-0.75f, 0f, 0f), false, Vector2.zero, 11);
        drainedVis0.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        var drainedVis1 = SpawnProp(channelRoot, "Spillway Drained Center", propSprites["BoundedSpillwayDrained"], new Vector3(0.0f, 0f, 0f), false, Vector2.zero, 11);
        drainedVis1.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        var drainedVis2 = SpawnProp(channelRoot, "Spillway Drained East", propSprites["BoundedSpillwayDrained"], new Vector3(0.75f, 0f, 0f), false, Vector2.zero, 11);
        drainedVis2.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        var drainedStepping = SpawnProp(channelRoot, "Drained Sluice Stepping Slab", propSprites["WalkableSteppingStone"], new Vector3(0f, -0.35f, 0f), false, Vector2.zero, 12);
        drainedStepping.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        drainedVis0.SetActive(false);
        drainedVis1.SetActive(false);
        drainedVis2.SetActive(false);
        drainedStepping.SetActive(false);

        var channelGrateHolder = new GameObject("Spillway Sluice Grate Holder").transform;
        channelGrateHolder.SetParent(channelRoot, false);
        channelGrateHolder.localPosition = new Vector3(-1.45f, 0.05f, 0f);
        var channelGrateGo = SpawnProp(channelGrateHolder, "Spillway Sluice Grate Visual", propSprites["SpillwaySluiceGrate"], Vector3.zero, false, Vector2.zero, 4);
        var grateColGo = new GameObject("GrateBarrierCollider");
        grateColGo.transform.SetParent(channelGrateHolder, false);
        var grateBox = grateColGo.AddComponent<BoxCollider2D>();
        grateBox.size = new Vector2(0.65f, 0.65f);

        var gaugeChannel = SpawnProp(channelRoot, "Channel Water Level Marker", propSprites["WaterLevelGaugeMarker"], new Vector3(-1.85f, 0.45f, 0f), false, Vector2.zero, 4);

        drainageWheel.channelGrateVisual = channelGrateGo.transform;
        drainageWheel.channelGrateCollider = grateBox;
        drainageWheel.floodedChannelObjects = new GameObject[] { floodedVis0, floodedVis1, floodedVis2 };
        drainageWheel.drainedChannelObjects = new GameObject[] { drainedVis0, drainedVis1, drainedVis2, drainedStepping };
        drainageWheel.waterLevelMarkers = new SpriteRenderer[]
        {
            gaugeUpper.GetComponent<SpriteRenderer>(),
            gaugeWaterfall.GetComponent<SpriteRenderer>(),
            gaugeChannel.GetComponent<SpriteRenderer>()
        };

        // --- LANDMARK 4: FLOODED PUMP STATION (East Bank) ---
        // Raised Service Platform (non-blocking ground slabs across x = -13.4..-10.6, y = 9.6..11.8)
        for (int px = 0; px < 3; px++)
        {
            for (int py = 0; py < 3; py++)
            {
                var plat = SpawnProp(stationGroup, $"Pump Platform Slab {px}_{py}", propSprites["PumpPlatformSlab"],
                    new Vector3(-13.0f + px * 1.0f, 9.6f + py * 0.95f, 0f), false, Vector2.zero, 11);
                plat.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
            }
        }
        // Broken Waterwheel at lake edge (-14.15, 11.35)
        SpawnProp(stationGroup, "Broken Pump Waterwheel", propSprites["BrokenWaterwheel"], new Vector3(-14.15f, 11.35f, 0f), true, new Vector2(0.85f, 0.55f), 4);
        SpawnRipple(stationGroup, "Waterwheel Ripple North", propSprites["WaterRippleRing"], new Vector3(-14.35f, 11.85f, 0f));
        SpawnRipple(stationGroup, "Waterwheel Ripple South", propSprites["WaterRippleRing"], new Vector3(-14.35f, 10.75f, 0f));

        // Monumental Sluice Gate & Bulkhead at north edge of Pump Station (-12.0, 12.55)
        SpawnProp(stationGroup, "Pump Station Sluice Gate", propSprites["PumpStationSluiceGate"], new Vector3(-12.0f, 12.55f, 0f), true, new Vector2(2.4f, 0.55f), 4);
        SpawnProp(stationGroup, "Station Pier West", pillarSpr, new Vector3(-13.6f, 12.45f, 0f), true, new Vector2(0.55f, 0.45f), 4);
        SpawnProp(stationGroup, "Station Pier East", pillarSpr, new Vector3(-10.2f, 12.45f, 0f), true, new Vector2(0.55f, 0.45f), 4);
        SpawnLantern(lanternPrefab, stationGroup, "Pump Station Lantern", new Vector3(-10.0f, 10.4f, 0f));
        SpawnLantern(lanternPrefab, stationGroup, "Upper Station Beacon", new Vector3(-14.0f, 18.0f, 0f));
        SpawnSalvagePickup(stationGroup, "PumpStation_Salvage", new Vector3(-10.4f, 9.1f, 0f), scrapItem, 2);

        // --- LANDMARK 5: CENTRAL ISLET — THE GAUGE SHRINE ---
        SpawnProp(isletGroup, "The Gauge Shrine Monument", propSprites["GaugeShrineMonument"], new Vector3(-17.35f, 13.45f, 0f), true, new Vector2(0.55f, 0.35f), 4);
        SpawnLantern(lanternPrefab, isletGroup, "Gauge Shrine Lantern", new Vector3(-17.65f, 12.35f, 0f));
        // Walkable Stepping Stones connecting Islet <-> Flooded Pump Station (East) & Southern Peninsula (South)
        Vector3[] steppingVisualPositions = new Vector3[]
        {
            new Vector3(-15.65f, 12.95f, 0f),
            new Vector3(-14.70f, 12.95f, 0f),
            new Vector3(-15.55f, 12.15f, 0f),
            new Vector3(-14.65f, 12.15f, 0f),
            new Vector3(-16.55f, 11.55f, 0f),
            new Vector3(-16.35f, 10.75f, 0f)
        };
        for (int i = 0; i < steppingVisualPositions.Length; i++)
        {
            var st = SpawnProp(isletGroup, $"Gauge Stepping Stone {i}", propSprites["WalkableSteppingStone"],
                steppingVisualPositions[i], false, Vector2.zero, 11);
            st.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
            if (i % 2 == 0)
                SpawnRipple(isletGroup, $"Stepping Stone Ripple {i}", propSprites["WaterRippleRing"], steppingVisualPositions[i]);
        }

        // Extra waterline algae & ripples for atmosphere
        Vector3[] algaeSpots = new Vector3[]
        {
            new Vector3(-23.2f, 9.6f, 0f),
            new Vector3(-22.6f, 14.6f, 0f),
            new Vector3(-18.2f, 13.8f, 0f),
            new Vector3(-14.4f, 13.4f, 0f),
            new Vector3(-17.8f, 8.8f, 0f)
        };
        for (int i = 0; i < algaeSpots.Length; i++)
        {
            var alg = SpawnProp(isletGroup, $"Waterline Algae {i}", propSprites["AlgaePatch"], algaeSpots[i], false, Vector2.zero, 11);
            alg.GetComponent<SpriteRenderer>().sortingLayerName = "Ground";
        }

        // 5. Loot Chests (4 Chests: Sluice Shaft Supply Cache, Pumpkeeper's Cabin, Flooded Pump Station, Gauge Shrine Stash)
        SpawnChest(chestPrefab, lootGroup, "Chest_SluiceShaft", new Vector3(-28.35f, 16.05f, 0f), new[]
        {
            new LootChest.Reward { item = scrapItem, minimum = 3, maximum = 5 },
            new LootChest.Reward { item = stoneItem, minimum = 4, maximum = 6 },
            new LootChest.Reward { item = woodItem, minimum = 4, maximum = 6 }
        });
        SpawnChest(chestPrefab, lootGroup, "Chest_PumpkeepersCabin", new Vector3(-21.0f, 22.8f, 0f), new[]
        {
            new LootChest.Reward { item = scrapItem, minimum = 2, maximum = 4 },
            new LootChest.Reward { item = berryItem, minimum = 3, maximum = 5 },
            new LootChest.Reward { item = woodItem, minimum = 4, maximum = 6 }
        });
        SpawnChest(chestPrefab, lootGroup, "Chest_FloodedPumpStation", new Vector3(-10.45f, 11.75f, 0f), new[]
        {
            new LootChest.Reward { item = scrapItem, minimum = 4, maximum = 6 },
            new LootChest.Reward { item = stoneItem, minimum = 5, maximum = 8 },
            new LootChest.Reward { item = berryItem, minimum = 3, maximum = 5 }
        });
        SpawnChest(chestPrefab, lootGroup, "Chest_GaugeShrine", new Vector3(-16.35f, 13.45f, 0f), new[]
        {
            new LootChest.Reward { item = scrapItem, minimum = 2, maximum = 4 },
            new LootChest.Reward { item = stoneItem, minimum = 3, maximum = 5 },
            new LootChest.Reward { item = berryItem, minimum = 2, maximum = 4 }
        });

        // 6. Configure the 13 Lake Creature Spawn Slots on AshenFrontierEncounterController
        var encounter = groveRoot.GetComponent<AshenFrontierEncounterController>();
        encounter.existingSpawner = spawner;
        encounter.spawnedContainer = enemiesGroup;
        encounter.maxConcurrentLocalEnemies = 5;
        encounter.despawnDistance = 24f;
        encounter.chests = lootGroup.GetComponentsInChildren<LootChest>(true);
        encounter.captainDropItems = new ItemDefinition[] { scrapItem, stoneItem, woodItem, berryItem };
        encounter.captainDropCounts = new int[] { 5, 6, 6, 4 };
        encounter.captainRewardPrefix = "FloodgateKeeperSpoils_";
        encounter.captainDefeatedNotice = "THE FLOODGATE KEEPER DEFEATED! COLLECT ITS WATERWORKS SPOILS.";

        var slotDefs = new (string id, Vector2 pos, GameObject prefab, float tether, bool isBoss)[]
        {
            // Fisherman's Landing & Reed Beds (Reed Stalkers)
            ("Spawn_ReedStalker_LandingSouth", new Vector2(-26.8f, 7.0f), reedPrefab, 5.5f, false),
            ("Spawn_ReedStalker_LandingPier",  new Vector2(-25.8f, 10.0f), reedPrefab, 5.5f, false),
            ("Spawn_ReedStalker_WestReeds",    new Vector2(-25.8f, 12.2f), reedPrefab, 5.5f, false),
            ("Spawn_ReedStalker_SouthBank",    new Vector2(-15.5f, 7.2f), reedPrefab, 5.5f, false),
            ("Spawn_ReedStalker_NorthReeds",   new Vector2(-15.0f, 20.6f), reedPrefab, 5.5f, false),

            // Sluice Shaft Walkway & Pump Station Approaches (Sluice Crabs)
            ("Spawn_SluiceCrab_ShaftWalkway",  new Vector2(-26.4f, 15.5f), crabPrefab, 5.0f, false),
            ("Spawn_SluiceCrab_ShaftApproach", new Vector2(-24.2f, 15.6f), crabPrefab, 5.0f, false),
            ("Spawn_SluiceCrab_CabinTerrace",  new Vector2(-22.2f, 20.4f), crabPrefab, 5.0f, false),
            ("Spawn_SluiceCrab_PumpSouth",     new Vector2(-12.2f, 9.3f), crabPrefab, 5.5f, false),
            ("Spawn_SluiceCrab_EastTerrace",   new Vector2(-12.0f, 16.8f), crabPrefab, 5.5f, false),

            // Shallow Waterline beside Gauge Shrine Islet & Flooded Pump Station (Lantern Eels)
            ("Spawn_LanternEel_IsletBank",     new Vector2(-16.1f, 12.3f), eelPrefab, 1.65f, false),
            ("Spawn_LanternEel_PumpWaterline", new Vector2(-13.6f, 10.6f), eelPrefab, 1.65f, false),

            // East Bank Flooded Pump Station Boss Arena (The Floodgate Keeper)
            ("Spawn_FloodgateKeeper_Boss",     new Vector2(-12.0f, 10.9f), bossPrefab, 6.5f, true)
        };

        var slots = new List<AshenFrontierEncounterController.SpawnSlot>();
        foreach (var def in slotDefs)
        {
            var marker = new GameObject(def.id);
            marker.transform.SetParent(spawnGroup, false);
            marker.transform.position = new Vector3(def.pos.x, def.pos.y, 0f);

            var slot = new AshenFrontierEncounterController.SpawnSlot
            {
                label = def.id,
                point = marker.transform,
                prefab = def.prefab.GetComponent<EnemyController>(),
                activationRadius = 9.5f,
                leashRadius = def.tether,
                isCaptain = def.isBoss
            };
            slots.Add(slot);
        }
        encounter.rogueSlots = slots.ToArray();

        Physics2D.SyncTransforms();
        if (nav != null)
            nav.Invalidate();

        EditorUtility.SetDirty(groveRoot);
        EditorUtility.SetDirty(waterTm);
        EditorSceneManager.MarkSceneDirty(groveRoot.scene);
        EditorSceneManager.SaveScene(groveRoot.scene);
        AssetDatabase.SaveAssets();

        return $"Upgraded The Drowned Grove — The Sunken Waterworks with 5 landmarks and {slots.Count} lake creature encounters.";
    }

    static EnemyArchetype CreateOrUpdateArchetype(
        string path, string id, string displayName, EnemyBehaviorProfile behavior,
        float health, float damage, float patrolSpeed, float chaseSpeed,
        float detectionRange, float attackRange, float windup, float recovery,
        float cooldown, float knockbackRes, float lungeDist,
        Sprite[] idle, Sprite[] walk, float fps)
    {
        var arch = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
        if (arch == null)
        {
            arch = ScriptableObject.CreateInstance<EnemyArchetype>();
            AssetDatabase.CreateAsset(arch, path);
        }
        arch.id = id;
        arch.displayName = displayName;
        arch.behavior = behavior;
        arch.health = health;
        arch.damage = damage;
        arch.patrolSpeed = patrolSpeed;
        arch.chaseSpeed = chaseSpeed;
        arch.detectionRange = detectionRange;
        arch.attackRange = attackRange;
        arch.windup = windup;
        arch.recovery = recovery;
        arch.cooldown = cooldown;
        arch.knockbackResistance = knockbackRes;
        arch.lungeDistance = lungeDist;
        arch.slamRadius = attackRange;
        arch.sprite = idle != null && idle.Length > 0 ? idle[0] : null;
        arch.idleFrames = idle;
        arch.walkFrames = walk;
        arch.animationFPS = fps;
        arch.tint = Color.white;
        arch.Validate();
        EditorUtility.SetDirty(arch);
        return arch;
    }

    static GameObject CreateLakeCreaturePrefab(
        string prefabPath, EnemyArchetype arch,
        SunkenWaterworksCreature.CreatureKind kind, Color nightColor,
        Sprite reedCamoSprite, Sprite rippleSprite, Sprite nightGlowSprite,
        float colliderRadius, float maxChase)
    {
        var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ForestStalker.prefab");
        Material litMat = template != null ? template.GetComponentInChildren<SpriteRenderer>().sharedMaterial : null;

        var root = new GameObject(arch.displayName);
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = root.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;

        var hp = root.AddComponent<EnemyHealth>();
        hp.Initialize(arch.health);

        var visGo = new GameObject("Visual");
        visGo.transform.SetParent(root.transform, false);
        var sr = visGo.AddComponent<SpriteRenderer>();
        sr.sprite = arch.sprite;
        sr.sharedMaterial = litMat;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 2;

        // Water ripple at feet
        var rippleGo = new GameObject("WaterRipple");
        rippleGo.transform.SetParent(root.transform, false);
        rippleGo.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        var rippleSr = rippleGo.AddComponent<SpriteRenderer>();
        rippleSr.sprite = rippleSprite;
        rippleSr.sharedMaterial = litMat;
        rippleSr.sortingLayerName = "Ground";
        rippleSr.sortingOrder = 12;
        rippleSr.color = new Color(0.65f, 0.95f, 1f, 0.65f);

        // Night tell glow
        var nightGo = new GameObject("NightTell");
        nightGo.transform.SetParent(visGo.transform, false);
        nightGo.transform.localPosition = new Vector3(0f, kind == SunkenWaterworksCreature.CreatureKind.LanternEel ? 0.55f : 0.28f, 0f);
        var nightSr = nightGo.AddComponent<SpriteRenderer>();
        nightSr.sprite = nightGlowSprite;
        nightSr.sharedMaterial = litMat;
        nightSr.sortingLayerName = "Characters";
        nightSr.sortingOrder = 4;
        nightSr.color = nightColor;

        // Subtle 2D point light for unmistakable night tell
        var light2d = nightGo.AddComponent<Light2D>();
        light2d.lightType = Light2D.LightType.Point;
        light2d.color = nightColor;
        light2d.intensity = kind == SunkenWaterworksCreature.CreatureKind.FloodgateKeeper ? 0.75f : 0.45f;
        light2d.pointLightInnerRadius = 0.15f;
        light2d.pointLightOuterRadius = kind == SunkenWaterworksCreature.CreatureKind.FloodgateKeeper ? 1.8f : 1.15f;

        SpriteRenderer reedSr = null;
        if (reedCamoSprite != null)
        {
            var reedGo = new GameObject("ReedCamouflage");
            reedGo.transform.SetParent(root.transform, false);
            reedGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            reedSr = reedGo.AddComponent<SpriteRenderer>();
            reedSr.sprite = reedCamoSprite;
            reedSr.sharedMaterial = litMat;
            reedSr.sortingLayerName = "Characters";
            reedSr.sortingOrder = 3;
            reedSr.color = new Color(0.78f, 0.95f, 0.82f, 0.85f);
        }

        var ctrl = root.AddComponent<EnemyController>();
        ctrl.visual = sr;
        ctrl.archetype = arch;
        ctrl.activeDuringDaytime = true;
        ctrl.maxChaseDistanceFromOrigin = maxChase;

        var lake = root.AddComponent<SunkenWaterworksCreature>();
        lake.kind = kind;
        lake.nightTellRenderer = nightSr;
        lake.rippleRenderer = rippleSr;
        lake.reedCamouflageRenderer = reedSr;
        lake.nightTellColor = nightColor;
        if (kind == SunkenWaterworksCreature.CreatureKind.FloodgateKeeper)
        {
            lake.surgeLength = 3.6f;
            lake.surgeWidth = 0.95f;
            lake.surfaceOrWeakPointDuration = 2.2f;
            lake.weakPointDamageMultiplier = 1.5f;
        }

        var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    static void RemoveGroupIfPresent(Transform parent, string name)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.name == name)
                Object.DestroyImmediate(c.gameObject);
        }
    }

    static Transform ResetGroup(Transform parent, string name)
    {
        Transform existing = null;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.name == name)
            {
                if (existing == null) existing = c;
                else Object.DestroyImmediate(c.gameObject);
            }
        }
        if (existing == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
        for (int i = existing.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(existing.GetChild(i).gameObject);
        return existing;
    }

    static void DisablePondObject(string path, Vector2? nearPos = null)
    {
        if (nearPos == null)
        {
            var go = GameObject.Find(path);
            if (go != null) go.SetActive(false);
            return;
        }
        var parent = GameObject.Find("Grid/HiddenPondClearing/Props")?.transform;
        if (parent == null) return;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == "Mossy Boulders" && Vector2.Distance(c.position, nearPos.Value) < 0.6f)
                c.gameObject.SetActive(false);
        }
    }

    static GameObject SpawnProp(Transform parent, string name, Sprite sprite, Vector3 pos, bool hasCollider, Vector2 colSize, int sortOrder)
    {
        var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ForestStalker.prefab");
        Material litMat = template != null ? template.GetComponentInChildren<SpriteRenderer>().sharedMaterial : null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (litMat != null) sr.sharedMaterial = litMat;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = sortOrder;
        if (hasCollider)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colSize;
        }
        return go;
    }

    static GameObject SpawnRipple(Transform parent, string name, Sprite sprite, Vector3 pos)
    {
        var go = SpawnProp(parent, name, sprite, pos, false, Vector2.zero, 12);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Ground";
        sr.color = new Color(0.75f, 0.98f, 1f, 0.58f);
        var anim = go.AddComponent<WaterlineRippleAnimator>();
        anim.targetRenderer = sr;
        return go;
    }

    static void SpawnLantern(GameObject prefab, Transform parent, string name, Vector3 pos)
    {
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.position = pos;
    }

    static void SpawnPrefab(GameObject prefab, Transform parent, string name, Vector3 pos)
    {
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.position = pos;
    }

    static void SpawnChest(GameObject prefab, Transform parent, string name, Vector3 pos, LootChest.Reward[] rewards)
    {
        if (prefab == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.position = pos;
        var chest = inst.GetComponent<LootChest>();
        if (chest != null)
        {
            chest.rewards = rewards;
            EditorUtility.SetDirty(chest);
        }
    }

    static void SpawnSalvagePickup(Transform parent, string name, Vector3 pos, ItemDefinition item, int amount)
    {
        var toolSpr = LoadExternalSprite("Assets/Pixel Crawler - Free Pack/Environment/Props/Static/Tools/Tool_04.png");
        var go = SpawnProp(parent, name, toolSpr, pos, false, Vector2.zero, 2);
        var pickup = go.AddComponent<ResourcePickup>();
        pickup.type = ResourceType.Scrap;
        pickup.item = item;
        pickup.quantity = amount;
    }

    static Sprite LoadExternalSprite(string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite s) return s;
        return null;
    }

    static Dictionary<string, Sprite> LoadSpriteDict(string path)
    {
        var dict = new Dictionary<string, Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite s) dict[s.name] = s;
        return dict;
    }

    static Sprite[] LoadSpriteArray(string path)
    {
        var list = new List<Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite s) list.Add(s);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list.ToArray();
    }
}
