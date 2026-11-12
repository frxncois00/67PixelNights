using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Existing scene spawner, now configured with prefabs instead of hard-coded species.
public class WildlifeSpawner : MonoBehaviour
{
    public Tilemap ground;
    public Transform campfire, player;
    public DayNightCycle cycle;
    public PassiveAnimal[] animalPrefabs;
    [Range(0, 80)] public int maximumAnimals = 36;
    [Min(1)] public float respawnSeconds = 30;
    [Min(1)] public float playerSpawnDistance = 6, animalSpacing = 3;
    [Tooltip("Spawn only on these tiles; movement uses ground and solid colliders.")]
    public TileBase[] grassTiles;
    readonly List<PassiveAnimal> animals = new List<PassiveAnimal>();
    readonly List<Vector2>[] sectors = new List<Vector2>[36];
    readonly Collider2D[] blockers = new Collider2D[16];
    float nextSpawn;
    int nextSector, nextSpecies;
    bool ready;
    public int LivingCount { get { animals.RemoveAll(a => !a || !a.GetComponent<EnemyHealth>().IsAlive); return animals.Count; } }

    void Start()
    {
        if (!cycle) cycle = FindAnyObjectByType<DayNightCycle>();
        if (!ground || !player || !campfire || !cycle || animalPrefabs == null || animalPrefabs.Length == 0)
        { Debug.LogError("WildlifeSpawner needs ground, player, campfire, cycle and prefabs.", this); enabled = false; return; }
        // Cache actual grass cells once; visit every sector before filling it again.
        var allowed = new HashSet<TileBase>(grassTiles ?? System.Array.Empty<TileBase>());
        var bounds = ground.cellBounds;
        for (int i = 0; i < sectors.Length; i++) sectors[i] = new List<Vector2>();
        foreach (var cell in bounds.allPositionsWithin)
        {
            var tile = ground.GetTile(cell);
            if (!tile || !allowed.Contains(tile)) continue;
            int x = Mathf.Clamp((cell.x - bounds.xMin) * 6 / Mathf.Max(1, bounds.size.x), 0, 5);
            int y = Mathf.Clamp((cell.y - bounds.yMin) * 6 / Mathf.Max(1, bounds.size.y), 0, 5);
            sectors[y * 6 + x].Add(ground.GetCellCenterWorld(cell));
        }
        ready = true;
        cycle.OnPhaseChanged += OnPhaseChanged;
        OnPhaseChanged();
    }
    void OnDestroy() { if (cycle) cycle.OnPhaseChanged -= OnPhaseChanged; }
    void OnPhaseChanged()
    {
        if (!ready) return;
        if (cycle.IsNighttime)
        {
            // Night removal is not a combat death and cannot generate loot.
            foreach (var animal in animals)
                if (animal && animal.GetComponent<EnemyHealth>().IsAlive)
                { animal.gameObject.SetActive(false); Destroy(animal.gameObject); }
            animals.Clear();
        }
        else Spawn();
        nextSpawn = Time.time + Mathf.Max(1, respawnSeconds);
    }
    void Update()
    {
        if (!ready || !cycle.IsDaytime || Time.time < nextSpawn) return;
        if (LivingCount < maximumAnimals) TrySpawnOne();
        nextSpawn = Time.time + Mathf.Max(1, respawnSeconds);
    }
    public void Spawn()
    {
        if (!ready || !cycle.IsDaytime) return;
        int missing = Mathf.Clamp(maximumAnimals, 0, 80) - LivingCount;
        Physics2D.SyncTransforms();
        for (int i = 0; i < missing; i++) if (!TrySpawnOne()) break;
    }
    bool TrySpawnOne()
    {
        if (LivingCount >= Mathf.Clamp(maximumAnimals, 0, 80)) return false;
        var prefab = animalPrefabs[nextSpecies++ % animalPrefabs.Length];
        if (!prefab || !prefab.data) return false;
        float radius = Mathf.Max(.1f, prefab.data.bodyRadius);
        for (int sectorAttempt = 0; sectorAttempt < sectors.Length; sectorAttempt++)
        {
            var candidates = sectors[nextSector++ % sectors.Length];
            if (candidates.Count == 0) continue;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                Vector2 point = candidates[Random.Range(0, candidates.Count)];
                if (!Clear(point, radius)) continue;
                var animal = Instantiate(prefab, point, Quaternion.identity, transform);
                animal.name = prefab.data.displayName;
                animal.ground = ground; animal.campfire = campfire;
                animals.Add(animal);
                return true;
            }
        }
        return false;
    }
    bool Clear(Vector2 point, float radius)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (!ground.HasTile(ground.WorldToCell(point + new Vector2(x, y) * radius))) return false;
        if (Vector2.Distance(point, player.position) < playerSpawnDistance || Vector2.Distance(point, campfire.position) < 3) return false;
        foreach (var animal in animals)
            if (animal && Vector2.Distance(point, animal.transform.position) < animalSpacing) return false;
        var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
        return Physics2D.OverlapCircle(point, radius + .1f, filter, blockers) == 0;
    }
}
