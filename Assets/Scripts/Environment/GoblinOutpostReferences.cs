using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Area references and registration with the existing night-wave spawner.</summary>
public class GoblinOutpostReferences : MonoBehaviour
{
    [Tooltip("Tilemaps remain beneath the existing Grid so cell coordinates stay shared.")]
    public Grid sharedGrid;
    public Tilemap[] tilemaps;
    [Tooltip("The three prefab instances live in the existing LootChestInstances registry.")]
    public LootChest[] chests;
    [Tooltip("Existing night-wave system; no second spawner or enemy budget is created.")]
    public EnemySpawner existingSpawner;
    public EnemyController enemyPrefab;
    [Tooltip("All layout markers, including the elevated tower marker.")]
    public Transform[] spawnPoints;
    [Tooltip("Ground markers eligible for existing night waves. Excludes the elevated tower.")]
    public Transform[] groundSpawnPoints;

    EnemySpawner registeredSpawner;
    Transform[] registeredPoints;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!existingSpawner || groundSpawnPoints == null || groundSpawnPoints.Length == 0)
        {
            Debug.LogWarning("GoblinOutpost requires a spawner and ground spawn markers.", this);
            return;
        }
        registeredSpawner = existingSpawner;
        registeredPoints = (Transform[])groundSpawnPoints.Clone();
        foreach (var point in registeredPoints) registeredSpawner.RegisterSpawnPoint(point);
    }

    void OnDisable()
    {
        if (registeredSpawner && registeredPoints != null)
            foreach (var point in registeredPoints) registeredSpawner.UnregisterSpawnPoint(point);
        registeredSpawner = null;
        registeredPoints = null;
    }
}
