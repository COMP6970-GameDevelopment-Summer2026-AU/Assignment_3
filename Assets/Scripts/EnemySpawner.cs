// ══════════════════════════════════════════════════════════════════════════════
// EnemySpawner.cs — unchanged from base
// Spawns enemies from left or right edge at random Y in upper half
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnRate = 2f;
    public float minY = 1.5f, maxY = 4.2f;

    float nextSpawnTime;

    void Update()
    {
        if (GameManager.Instance != null &&
           (GameManager.Instance.IsGameOver || GameManager.Instance.IsStartScreen)) return;

        if (Time.time >= nextSpawnTime) { SpawnEnemy(); nextSpawnTime = Time.time + spawnRate; }
    }

    void SpawnEnemy()
    {
        bool left = Random.value > 0.5f;
        float x = left ? -3.5f : 3.5f;
        float y = Random.Range(minY, maxY);
        var go = Instantiate(enemyPrefab, new Vector3(x, y, 0f), Quaternion.Euler(0,0,180));
        var e  = go.GetComponent<Enemy>();
        if (!left && e != null) e.moveSpeed *= -1f;
    }
}
