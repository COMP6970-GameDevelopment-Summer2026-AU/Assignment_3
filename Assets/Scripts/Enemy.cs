// ══════════════════════════════════════════════════════════════════════════════
// Enemy.cs — unchanged from base (kept for reference)
// Moves left/right with sine wave, shoots at fireRate, destroys at screen edge
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed;
    public float waveAmount;
    public float waveSpeed;

    public GameObject enemyBulletPrefab;
    public Transform  enemyFirePoint;
    public float      fireRate = 1.5f;

    public AudioSource audioSource;
    public AudioClip   shootSound;

    float startY, nextFireTime;

    void Start()
    {
        moveSpeed  = Random.Range(1.5f, 3f);
        waveAmount = Random.Range(0.2f, 0.8f);
        waveSpeed  = Random.Range(1f,   3f);
        startY     = transform.position.y;
        nextFireTime = Time.time + Random.Range(0.5f, 1.5f);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
        float y = startY + Mathf.Sin(Time.time * waveSpeed) * waveAmount;
        transform.position = new Vector3(transform.position.x, y, 0f);

        if (Time.time >= nextFireTime) { Shoot(); nextFireTime = Time.time + fireRate; }
        if (transform.position.x < -4f || transform.position.x > 4f) Destroy(gameObject);
    }

    void Shoot()
    {
        if (audioSource && shootSound) audioSource.PlayOneShot(shootSound);
        Instantiate(enemyBulletPrefab, enemyFirePoint.position, Quaternion.identity);
    }
}
