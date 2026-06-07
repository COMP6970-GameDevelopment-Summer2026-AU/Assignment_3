// ══════════════════════════════════════════════════════════════════════════════
// MeteorSpawner.cs — NEW for Assignment 3
//
// Req 1: Spawns meteors from the LOWER HALF of the screen
//        (y range: -4.5 to 0 — below the player's default mid position)
// Req 2: Meteor moves toward player (handled in Meteor.cs)
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("Prefab (Req 1-3): assign Meteor prefab")]
    public GameObject meteorPrefab;

    [Header("Spawn Rate")]
    public float spawnRate    = 3f;    // seconds between spawns (gets faster over time)
    public float minSpawnRate = 0.8f;  // fastest possible rate
    public float difficultyRampTime = 60f; // seconds until min rate

    [Header("Spawn Zone — lower half of screen (Req 1)")]
    public float spawnMinX = -3.5f;
    public float spawnMaxX =  3.5f;
    public float spawnMinY = -7.5f;   // well below visible play area
    public float spawnMaxY = -5.5f;   // far from player start

    float nextSpawnTime;
    float elapsed;

    void Awake()
    {
        // Force correct values — overrides any stale Inspector cache
        spawnMinY = -7.5f;
        spawnMaxY = -5.5f;
        nextSpawnTime = 4f; // 4 second delay before first meteor
        Debug.Log($"[MeteorSpawner] Spawn zone reset: Y={spawnMinY} to {spawnMaxY}, firstSpawn in 4s");
    }

    void Update()
    {
        if (GameManager.Instance != null &&
           (GameManager.Instance.IsGameOver || GameManager.Instance.IsStartScreen)) return;

        elapsed += Time.deltaTime;

        // Ramp up difficulty over time
        float t           = Mathf.Clamp01(elapsed / difficultyRampTime);
        float currentRate = Mathf.Lerp(spawnRate, minSpawnRate, t);

        if (Time.time >= nextSpawnTime)
        {
            SpawnMeteor();
            nextSpawnTime = Time.time + currentRate;
        }
    }

    void SpawnMeteor()
    {
        if (meteorPrefab == null) return;

        // Req 1: spawn from lower half
        float x = Random.Range(spawnMinX, spawnMaxX);
        float y = Random.Range(spawnMinY, spawnMaxY);

        Instantiate(meteorPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        Debug.Log($"[MeteorSpawner] Spawned meteor at ({x:F1}, {y:F1})");
    }
}
