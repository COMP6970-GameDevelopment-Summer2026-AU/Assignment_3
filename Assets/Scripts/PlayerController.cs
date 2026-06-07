// ══════════════════════════════════════════════════════════════════════════════
// PlayerController.cs — replaces base version
// BASE: WASD movement clamped to bounds, shoot on Space/Click, AudioSource
// NEW (A3): OnHit() called by EnemyBullet, hit flash, calls GameManager.TakeHit
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float minX = -4.5f, maxX = 4.5f;   // full screen horizontal
    public float minY = -4.5f, maxY = 4.5f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform  firePoint;
    public float      fireRate = 0.25f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   shootSound;
    public AudioClip   hitSound;        // impactMetal_000.ogg

    // Runtime
    float          nextFireTime;
    Vector2        moveInput;
    SpriteRenderer sr;
    float          flashTimer;
    bool           flashing;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Auto-calculate bounds from camera so player always uses full screen
        if (Camera.main != null)
        {
            float h = Camera.main.orthographicSize;
            float w = h * Camera.main.aspect;
            minX = -w + 0.3f;   // small margin so sprite doesn't clip edge
            maxX =  w - 0.3f;
            minY = -h + 0.3f;
            maxY =  h - 0.3f;
            Debug.Log($"[Player] Bounds set from camera: X={minX:F1}..{maxX:F1} Y={minY:F1}..{maxY:F1}");
        }
    }

    void Update()
    {
        if (GameManager.Instance != null &&
           (GameManager.Instance.IsGameOver || GameManager.Instance.IsStartScreen)) return;

        // Move
        Vector3 move = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += move * moveSpeed * Time.deltaTime;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;

        // Hit flash
        if (flashing) { flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) { flashing=false; if(sr) sr.color=Color.white; } }
    }

    public void OnMove(InputValue value)   => moveInput = value.Get<Vector2>();

    public void OnAttack(InputValue value)
    {
        if (GameManager.Instance != null &&
           (GameManager.Instance.IsGameOver || GameManager.Instance.IsStartScreen)) return;
        if (Time.time >= nextFireTime) { Shoot(); nextFireTime = Time.time + fireRate; }
    }

    void Shoot()
    {
        if (audioSource && shootSound) audioSource.PlayOneShot(shootSound);
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    // Called by EnemyBullet on Player trigger (Req 4-6)
    public void OnHit()
    {
        if (audioSource && hitSound) audioSource.PlayOneShot(hitSound);
        if (sr) sr.color = new Color(1f, 0.2f, 0.2f);  // flash red
        flashing = true; flashTimer = 0.15f;
        GameManager.Instance?.PlayerHitByBullet();
    }
}