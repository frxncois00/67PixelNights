using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages proximity-based rogue stronghold encounters, chest references, optional night-wave ground markers,
/// and the Rogue Captain defeat reward for The Ashen Frontier without altering night-wave schedules or exceeding
/// the global enemy cap.
/// </summary>
public class AshenFrontierEncounterController : MonoBehaviour
{
    [Serializable]
    public class SpawnSlot
    {
        public string label;
        public Transform point;
        public EnemyController prefab;
        public bool isCaptain;
        [Min(2f)] public float activationRadius = 11f;
        [Min(4f)] public float leashRadius = 12f;
        [Tooltip("Optional ordered patrol. Empty keeps the existing random patrol; one point holds a post.")]
        public Transform[] patrolWaypoints;

        [NonSerialized] public EnemyController instance;
        [NonSerialized] public bool defeated;
        [NonSerialized] public float farSince;
    }

    [Header("World Integration")]
    public Grid sharedGrid;
    public Tilemap[] tilemaps;
    public Tilemap groundTilemap;
    public EnemySpawner existingSpawner;
    public PlayerInteractionController playerInteraction;
    public LootChest[] chests;
    public Transform spawnedContainer;

    [Header("Encounter Population & Limits")]
    [Min(1)] public int maxConcurrentLocalEnemies = 6;
    [Min(12f)] public float despawnDistance = 26f;
    [Min(0.1f)] public float tickInterval = 0.4f;
    public SpawnSlot[] rogueSlots;
    [Tooltip("Opt-in boundary for this encounter only, in world coordinates.")]
    public bool restrictToEncounterBounds;
    public Rect encounterBounds;

    [Tooltip("Walkable open ground markers optionally shared with existing night-wave spawns.")]
    public Transform[] nightSpawnPoints;

    [Header("Captain Defeat Reward")]
    public ItemDefinition[] captainDropItems;
    public int[] captainDropCounts;
    public Material pickupMaterial;
    public string captainRewardPrefix = "AshenCaptainSpoils_";
    public string captainDefeatedNotice = "ASHEN CAPTAIN DEFEATED! COLLECT HIS SPOILS.";
    public bool respawnCaptainOnNewDay = true;

    EnemySpawner registeredSpawner;
    Transform[] registeredNightPoints;
    DayNightCycle boundCycle;
    float nextTick;
    bool captainRewardDropped;

    public int LocalActiveCount
    {
        get
        {
            int count = 0;
            if (rogueSlots == null) return 0;
            foreach (var slot in rogueSlots)
            {
                if (slot != null && slot.instance && slot.instance.gameObject.activeInHierarchy && slot.instance.Alive)
                    count++;
            }
            return count;
        }
    }

    public int TotalSlots => rogueSlots != null ? rogueSlots.Length : 0;

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (!existingSpawner)
        {
            Debug.LogWarning("AshenFrontierEncounterController requires existingSpawner.", this);
            return;
        }

        registeredSpawner = existingSpawner;
        if (nightSpawnPoints != null && nightSpawnPoints.Length > 0)
        {
            registeredNightPoints = (Transform[])nightSpawnPoints.Clone();
            foreach (var p in registeredNightPoints)
            {
                if (p)
                    registeredSpawner.RegisterSpawnPoint(p);
            }
        }

        boundCycle = existingSpawner.cycle;
        if (boundCycle)
            boundCycle.OnDayStarted += HandleDayStarted;

        AdoptPreplacedEnemies();
    }

    void AdoptPreplacedEnemies()
    {
        if (!spawnedContainer || rogueSlots == null)
            return;

        var preplaced = spawnedContainer.GetComponentsInChildren<EnemyController>(false);
        foreach (var enemy in preplaced)
        {
            if (!enemy || !enemy.Alive)
                continue;
            bool alreadyAssigned = false;
            foreach (var slot in rogueSlots) if (slot != null && slot.instance == enemy) { alreadyAssigned = true; break; }
            if (alreadyAssigned) continue;

            SpawnSlot bestSlot = null;
            float bestDist = float.MaxValue;
            foreach (var slot in rogueSlots)
            {
                if (slot == null || slot.instance != null || !slot.point)
                    continue;
                float d = Vector2.Distance(enemy.transform.position, slot.point.position);
                if (d < bestDist && d < 4.5f)
                {
                    bestDist = d;
                    bestSlot = slot;
                }
            }

            enemy.world = existingSpawner;
            enemy.activeDuringDaytime = true;
            if (bestSlot != null)
            {
                enemy.maxChaseDistanceFromOrigin = bestSlot.leashRadius;
                enemy.ConfigureEncounter(bestSlot.patrolWaypoints, bestSlot.point.position, restrictToEncounterBounds, encounterBounds);
                bestSlot.instance = enemy;
                bestSlot.farSince = 0f;
                var health = enemy.GetComponent<EnemyHealth>();
                if (health)
                {
                    var capturedSlot = bestSlot;
                    var capturedEnemy = enemy;
                    health.Died += () => HandleEnemyDied(capturedSlot, capturedEnemy);
                }
            }
        }
    }

    void OnDisable()
    {
        if (boundCycle)
            boundCycle.OnDayStarted -= HandleDayStarted;
        boundCycle = null;

        if (registeredSpawner && registeredNightPoints != null)
        {
            foreach (var p in registeredNightPoints)
            {
                if (p)
                    registeredSpawner.UnregisterSpawnPoint(p);
            }
        }

        registeredSpawner = null;
        registeredNightPoints = null;
    }

    void HandleDayStarted()
    {
        if (!Application.isPlaying || rogueSlots == null || !existingSpawner || !existingSpawner.player)
            return;

        // Only refresh defeated frontier scouts on a new day if the player is well outside the stronghold.
        Vector2 playerPos = existingSpawner.player.transform.position;
        foreach (var slot in rogueSlots)
        {
            if (slot == null || !slot.point)
                continue;
            if (slot.isCaptain && !respawnCaptainOnNewDay) continue;
            if (slot.defeated && (!slot.instance || !slot.instance.Alive) && Vector2.Distance(playerPos, slot.point.position) > 22f)
                slot.defeated = false;
        }
    }

    void Update()
    {
        if (!Application.isPlaying || Time.time < nextTick)
            return;
        nextTick = Time.time + tickInterval;
        EvaluateEncounters();
    }

    public void EvaluateEncounters()
    {
        if (!existingSpawner || !existingSpawner.player || !existingSpawner.player.IsAlive || !existingSpawner.safe || rogueSlots == null)
            return;

        Vector2 playerPos = existingSpawner.player.transform.position;
        int globalMax = Mathf.Max(1, existingSpawner.maximumActive);

        // First clean up null/dead instances or despawn distant unengaged guards to protect performance & night cap.
        foreach (var slot in rogueSlots)
        {
            if (slot == null)
                continue;

            if (slot.instance && !slot.instance.Alive)
                slot.defeated = true;

            if (!slot.instance)
                continue;

            float dist = slot.point ? Vector2.Distance(playerPos, slot.point.position) : Vector2.Distance(playerPos, slot.instance.transform.position);
            bool engaged = slot.instance.State == EnemyState.Chase || slot.instance.State == EnemyState.Attack;
            if (dist > despawnDistance && !engaged)
            {
                if (slot.farSince <= 0f)
                    slot.farSince = Time.time;
                else if (Time.time - slot.farSince > 3f)
                {
                    Destroy(slot.instance.gameObject);
                    slot.instance = null;
                    slot.farSince = 0f;
                }
            }
            else
            {
                slot.farSince = 0f;
            }
        }

        int currentLocal = LocalActiveCount;
        int currentGlobal = existingSpawner.TotalActiveCount;
        while (currentGlobal > globalMax)
        {
            SpawnSlot furthestUnengaged = null;
            float bestDist = -1f;
            foreach (var slot in rogueSlots)
            {
                if (slot == null || !slot.instance || !slot.instance.Alive)
                    continue;
                bool engaged = slot.instance.State == EnemyState.Chase || slot.instance.State == EnemyState.Attack;
                if (engaged)
                    continue;
                float d = Vector2.Distance(playerPos, slot.instance.transform.position);
                if (d > bestDist)
                {
                    bestDist = d;
                    furthestUnengaged = slot;
                }
            }

            if (furthestUnengaged == null)
                break;

            Destroy(furthestUnengaged.instance.gameObject);
            furthestUnengaged.instance = null;
            furthestUnengaged.farSince = 0f;
            currentLocal--;
            currentGlobal--;
        }

        if (currentLocal >= maxConcurrentLocalEnemies || currentGlobal >= globalMax)
            return;

        // Spawn closest eligible proximity guards while respecting Walkable, SafeZone, and player buffer.
        for (int i = 0; i < rogueSlots.Length; i++)
        {
            if (currentLocal >= maxConcurrentLocalEnemies || currentGlobal >= globalMax)
                break;

            var slot = rogueSlots[i];
            if (slot == null || slot.defeated || slot.instance != null || !slot.point || !slot.prefab)
                continue;

            float dist = Vector2.Distance(playerPos, slot.point.position);
            if (dist > slot.activationRadius)
                continue;

            if (!FindValidSpawnPosition(slot.point.position, playerPos, out Vector2 spawnPos))
                continue;

            Transform parent = spawnedContainer ? spawnedContainer : transform;
            var enemy = Instantiate(slot.prefab, spawnPos, Quaternion.identity, parent);
            enemy.world = existingSpawner;
            enemy.activeDuringDaytime = true;
            enemy.maxChaseDistanceFromOrigin = slot.leashRadius;
            enemy.ConfigureEncounter(slot.patrolWaypoints, slot.point.position, restrictToEncounterBounds, encounterBounds);
            enemy.name = !string.IsNullOrEmpty(slot.label) ? slot.label : (slot.prefab.archetype ? slot.prefab.archetype.displayName : slot.prefab.name);
            slot.instance = enemy;
            slot.farSince = 0f;

            var health = enemy.GetComponent<EnemyHealth>();
            if (health)
            {
                var capturedSlot = slot;
                var capturedEnemy = enemy;
                health.Died += () => HandleEnemyDied(capturedSlot, capturedEnemy);
            }

            currentLocal++;
            currentGlobal++;
            Physics2D.SyncTransforms();
        }
    }

    bool FindValidSpawnPosition(Vector2 desired, Vector2 playerPos, out Vector2 result)
    {
        if (IsPositionValid(desired, playerPos))
        {
            result = desired;
            return true;
        }

        // Try clean walkable offsets around the placed marker if the player or another unit overlaps exact center.
        for (int r = 1; r <= 2; r++)
        {
            float rad = r * 1.1f;
            for (int a = 0; a < 8; a++)
            {
                float ang = a * (Mathf.PI * 0.25f);
                Vector2 candidate = desired + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                if (IsPositionValid(candidate, playerPos) && existingSpawner.Visible(desired, candidate, null, null))
                {
                    result = candidate;
                    return true;
                }
            }
        }

        result = desired;
        return false;
    }

    bool IsPositionValid(Vector2 pos, Vector2 playerPos)
    {
        if (EncounterSafeArea.Contains(pos, .3f) || (restrictToEncounterBounds && !encounterBounds.Contains(pos)))
            return false;
        if (!existingSpawner || !existingSpawner.safe || existingSpawner.safe.IsPositionSafe(pos))
            return false;
        if (Vector2.Distance(pos, playerPos) < 2.2f)
            return false;
        return existingSpawner.Walkable(pos);
    }

    void HandleEnemyDied(SpawnSlot slot, EnemyController enemy)
    {
        if (slot != null)
            slot.defeated = true;

        if (slot != null && slot.isCaptain && !captainRewardDropped)
        {
            captainRewardDropped = true;
            Vector2 dropOrigin = enemy ? (Vector2)enemy.transform.position : (Vector2)slot.point.position;
            SpawnCaptainReward(dropOrigin);
        }
    }

    void SpawnCaptainReward(Vector2 origin)
    {
        if (captainDropItems == null || captainDropCounts == null)
            return;

        Tilemap ground = groundTilemap ? groundTilemap : existingSpawner.ground;
        for (int i = 0; i < captainDropItems.Length && i < captainDropCounts.Length; i++)
        {
            var item = captainDropItems[i];
            int count = Mathf.Max(1, captainDropCounts[i]);
            if (!item || !item.Icon)
                continue;

            Vector3 dropPos = origin;
            bool placed = false;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float angle = (i * 2.1f) + attempt * 0.9f;
                Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (0.45f + attempt * 0.08f);
                if (ground && !ground.HasTile(ground.WorldToCell(candidate)))
                    continue;
                if (Physics2D.OverlapCircle(candidate, 0.22f))
                    continue;
                dropPos = candidate;
                placed = true;
                break;
            }

            if (!placed)
                dropPos = origin;

            var pickupGo = new GameObject(captainRewardPrefix + item.DisplayName);
            pickupGo.SetActive(false);
            pickupGo.transform.position = dropPos;
            var sr = pickupGo.AddComponent<SpriteRenderer>();
            sr.sprite = item.Icon;
            sr.color = item.IconTint;
            if (pickupMaterial)
                sr.sharedMaterial = pickupMaterial;
            sr.sortingLayerName = "Objects";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            var pickup = pickupGo.AddComponent<ResourcePickup>();
            pickup.type = item.Resource;
            pickup.item = item;
            pickup.quantity = count;
            pickupGo.SetActive(true);
        }

        if (playerInteraction)
            playerInteraction.Notice(captainDefeatedNotice);
    }
}
