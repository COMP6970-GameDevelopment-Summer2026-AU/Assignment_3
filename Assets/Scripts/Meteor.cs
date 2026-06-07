// ══════════════════════════════════════════════════════════════════════════════
// Meteor.cs — NEW for Assignment 3
//
// Req 1: Spawns from lower half of screen
// Req 2: Moves toward the player
// Req 3: Hits player → instant game over
//
// Sprite: spaceMeteors_001.png or spaceMeteors_002.png (tag: "Meteor")
// Attach: Rigidbody2D (Kinematic), CircleCollider2D (Is Trigger)
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;

public class Meteor : MonoBehaviour
{
    [Header("Movement")]
    public float speed        = 1.5f;    // base speed toward player
    public float rotateSpeed  = 90f;     // spin while flying

    [Header("Speed randomisation")]
    public float minSpeed = 1f;
    public float maxSpeed = 2.5f;

    Transform player;
    Vector3   direction;    // computed once at spawn toward player

    void Start()
    {
        // Reduce VISUAL SIZE by 60% — scale down the sprite
        transform.localScale = transform.localScale * 0.4f;

        // Also reduce collider to match
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = 0.2f; // tight hitbox regardless of scale
            Debug.Log($"[Meteor] Scale={transform.localScale.x:F2} collider={col.radius:F2}");
        }

        // Force slow speed — overrides stale Inspector values
        minSpeed = 1f;
        maxSpeed = 2.5f;
        speed    = Random.Range(minSpeed, maxSpeed);

        // Find player
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player    = p.transform;
            // Direction toward player position at spawn time (Req 2)
            direction = (player.position - transform.position).normalized;
        }
        else
        {
            // Fallback: move straight up
            direction = Vector3.up;
        }

        // Destroy after 10s if never hit anything
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Move toward player (Req 2)
        if (player != null)
        {
            // Continuously track player for homing feel
            direction = Vector3.Lerp(direction,
                (player.position - transform.position).normalized, Time.deltaTime * 0.08f);
        }

        transform.position += direction * speed * Time.deltaTime;

        // Spin
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // Self-destruct if off screen — counts as avoided
        if (transform.position.y > 8f || transform.position.y < -9f ||
            Mathf.Abs(transform.position.x) > 6f)
        {
            GameManager.Instance?.MeteorMissed();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Req 3: meteor hits player → instant game over
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Meteor] Hit player — game over!");
            GameManager.Instance?.PlayerHitByMeteor();
            Destroy(gameObject);
        }
    }
}