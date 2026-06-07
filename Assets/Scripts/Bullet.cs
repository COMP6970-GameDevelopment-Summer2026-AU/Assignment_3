// ══════════════════════════════════════════════════════════════════════════════
// Bullet.cs — replaces base version
// BASE: moves up, destroys Enemy on trigger
// NEW (A3): notifies GameManager.EnemyDestroyed() and MeteorShot()
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
        if (transform.position.y > 10f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            GameManager.Instance?.EnemyDestroyed();   // Req 7+9
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Meteor"))
        {
            GameManager.Instance?.MeteorShot();       // bonus score
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
