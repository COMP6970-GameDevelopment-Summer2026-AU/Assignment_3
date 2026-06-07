// ══════════════════════════════════════════════════════════════════════════════
// EnemyBullet.cs — replaces base version
// BASE: moves down, destroys on Player trigger
// NEW (A3): calls player.OnHit() → health system instead of just Debug.Log
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
        if (transform.position.y < -6f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Req 4-6: notify player → GameManager health system
            other.GetComponent<PlayerController>()?.OnHit();
            Destroy(gameObject);
        }
    }
}
