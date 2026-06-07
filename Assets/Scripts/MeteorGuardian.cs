// ══════════════════════════════════════════════════════════════════════════════
// MeteorGuardian.cs — Meteor Rush A3
// Developer: Jahidul Arafat | COMP 6910 | Summer 2026
//
// WHAT IT DOES:
//   • Scans for incoming meteors every frame
//   • Warns the player with an OnGUI arrow + message ("⚠ MOVE LEFT/RIGHT!")
//   • If player does NOT move for warningTimeout seconds → auto-moves player
//     out of the meteor's path
//
// MODES (toggle with G key or Inspector):
//   OFF        — disabled, no warnings, no auto-move
//   WARN_ONLY  — shows warnings, player must dodge themselves
//   AUTO       — warns + auto-moves player if they don't respond
//
// Attach to: Player GameObject
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class MeteorGuardian : MonoBehaviour
{
    // ── Mode ──────────────────────────────────────────────────────────────────
    public enum GuardianMode { Off, WarnOnly, Auto }

    [Header("Guardian Mode (press G to cycle)")]
    public GuardianMode mode = GuardianMode.Off;

    [Header("Detection")]
    public float detectionRadius   = 5f;   // how far to scan for meteors
    public float dangerThreshold   = 2.5f; // within this distance = danger
    public float warningTimeout    = 1.2f; // seconds of no input → auto-move

    [Header("Auto-Move")]
    public float autoMoveSpeed     = 6f;   // how fast guardian moves player
    public float safeDistance      = 1.8f; // how far to dodge sideways

    // ── Runtime ───────────────────────────────────────────────────────────────
    PlayerController playerCtrl;
    Rigidbody2D      rb;
    Transform        closestMeteor;
    float            closestDist;
    float            noInputTimer;
    bool             isAutoMoving;
    Vector2          autoTarget;
    string           warningMessage = "";
    float            warningFlash;

    // GUI
    GUIStyle warnStyle, modeStyle, msgStyle;
    bool     stylesReady;

    // bounds from PlayerController
    float minX = -2.4f, maxX = 2.4f;
    float minY = -4.5f, maxY =  4.5f;

    void Start()
    {
        playerCtrl = GetComponent<PlayerController>();
        rb         = GetComponent<Rigidbody2D>();
        Debug.Log($"[Guardian] Started in mode: {mode}. Press G to cycle modes.");
    }

    void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.IsGameOver ||
            GameManager.Instance.IsStartScreen) return;

        // ── Toggle mode with G ────────────────────────────────────────────────
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            mode = (GuardianMode)(((int)mode + 1) % 3);
            Debug.Log($"[Guardian] Mode switched to: {mode}");
            isAutoMoving   = false;
            warningMessage = "";
            GameManager.Instance?.ShowHint($"Guardian: {mode}");
        }

        if (mode == GuardianMode.Off)
        {
            warningMessage = "";
            isAutoMoving   = false;
            return;
        }

        // ── Scan for closest meteor ───────────────────────────────────────────
        ScanMeteors();

        // ── Check player input ────────────────────────────────────────────────
        bool playerMoving = Mathf.Abs(Keyboard.current.aKey.isPressed ? -1 :
                             Keyboard.current.dKey.isPressed ?  1 :
                             Keyboard.current.leftArrowKey.isPressed ? -1 :
                             Keyboard.current.rightArrowKey.isPressed ? 1 : 0) > 0.1f;

        // ── Warn + auto-move logic ────────────────────────────────────────────
        if (closestMeteor != null && closestDist < dangerThreshold)
        {
            // Determine dodge direction
            float meteorX = closestMeteor.position.x;
            float playerX = transform.position.x;
            bool  goRight = playerX < meteorX ? false : true; // dodge away
            if (playerX < meteorX) goRight = false; else goRight = true;
            // Actually: move opposite to meteor X
            goRight = meteorX >= playerX;  // meteor is to right → go left, and vice versa
            goRight = !goRight;            // flip: dodge opposite direction

            string dir = goRight ? "RIGHT →" : "← LEFT";
            warningMessage = $"⚠  METEOR INCOMING!  MOVE {dir}";
            warningFlash   = Time.time;

            if (playerMoving)
                noInputTimer = 0f;         // player responded
            else
                noInputTimer += Time.deltaTime;

            // Auto-move if player idle too long
            if (mode == GuardianMode.Auto && noInputTimer >= warningTimeout)
            {
                if (!isAutoMoving)
                {
                    // Calculate safe dodge target
                    float dodgeX = goRight
                        ? Mathf.Min(transform.position.x + safeDistance, maxX - 0.2f)
                        : Mathf.Max(transform.position.x - safeDistance, minX + 0.2f);
                    float dodgeY = transform.position.y;
                    autoTarget   = new Vector2(dodgeX, dodgeY);
                    isAutoMoving = true;
                    Debug.Log($"[Guardian] AUTO-DODGE triggered → target=({dodgeX:F1},{dodgeY:F1})");
                }
            }
        }
        else
        {
            // No immediate threat — clear warning gradually
            warningMessage = closestMeteor != null && closestDist < detectionRadius
                ? "👁  Meteor detected — stay alert"
                : "";
            noInputTimer = 0f;
            if (isAutoMoving && Vector2.Distance(transform.position, autoTarget) < 0.15f)
                isAutoMoving = false;
        }

        // ── Apply auto-move ───────────────────────────────────────────────────
        if (isAutoMoving && mode == GuardianMode.Auto)
        {
            Vector2 dir2  = (autoTarget - (Vector2)transform.position).normalized;
            Vector3 pos   = transform.position;
            pos.x         = Mathf.MoveTowards(pos.x, autoTarget.x, autoMoveSpeed * Time.deltaTime);
            pos.y         = Mathf.MoveTowards(pos.y, autoTarget.y, autoMoveSpeed * Time.deltaTime);
            pos.x         = Mathf.Clamp(pos.x, minX, maxX);
            pos.y         = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;

            if (Vector2.Distance(transform.position, autoTarget) < 0.1f)
            {
                isAutoMoving = false;
                noInputTimer = 0f;
                Debug.Log("[Guardian] Auto-dodge complete.");
            }
        }
    }

    void ScanMeteors()
    {
        closestMeteor = null;
        closestDist   = float.MaxValue;

        // Find all meteors in scene
        var meteors = FindObjectsByType<Meteor>(FindObjectsInactive.Exclude);
        foreach (var m in meteors)
        {
            float d = Vector2.Distance(transform.position, m.transform.position);
            if (d < closestDist)
            {
                closestDist   = d;
                closestMeteor = m.transform;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // OnGUI — warning overlay and mode indicator
    // ══════════════════════════════════════════════════════════════════════════
    void OnGUI()
    {
        if (!stylesReady)
        {
            warnStyle = new GUIStyle
            {
                fontSize   = 20,
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleCenter,
                normal     = { textColor = new Color(1f, 0.85f, 0f) }
            };
            modeStyle = new GUIStyle
            {
                fontSize   = 13,
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleRight,
                normal     = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            msgStyle = new GUIStyle
            {
                fontSize   = 13,
                fontStyle  = FontStyle.Normal,
                alignment  = TextAnchor.MiddleCenter,
                normal     = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            stylesReady = true;
        }

        float sw = Screen.width, sh = Screen.height;

        // ── Mode indicator — bottom right ─────────────────────────────────────
        string modeLabel = mode == GuardianMode.Off      ? "Guardian: OFF" :
                           mode == GuardianMode.WarnOnly ? "Guardian: WARN ONLY" :
                                                           "Guardian: AUTO DODGE";
        Color modeColor  = mode == GuardianMode.Off      ? new Color(0.4f,0.4f,0.4f) :
                           mode == GuardianMode.WarnOnly ? new Color(1f,0.75f,0.2f) :
                                                           new Color(0.3f,1f,0.5f);
        modeStyle.normal.textColor = modeColor;
        GUI.Label(new Rect(0, sh-28, sw-10, 24), modeLabel + "  (G = cycle)", modeStyle);

        if (mode == GuardianMode.Off) return;
        if (GameManager.Instance == null ||
            GameManager.Instance.IsGameOver ||
            GameManager.Instance.IsStartScreen) return;

        // ── Warning banner — center screen ────────────────────────────────────
        if (!string.IsNullOrEmpty(warningMessage))
        {
            // Pulsing alpha
            float pulse = 0.65f + Mathf.Sin(Time.time * 8f) * 0.35f;

            // Background
            Color bgCol = closestDist < dangerThreshold
                ? new Color(0.6f, 0.1f, 0.05f, 0.82f * pulse)
                : new Color(0.2f, 0.2f, 0.05f, 0.65f * pulse);

            GUI.color = bgCol;
            GUI.DrawTexture(new Rect(0, sh*0.42f, sw, 50), Texture2D.whiteTexture);

            // Top border
            GUI.color = closestDist < dangerThreshold
                ? new Color(1f, 0.2f, 0.1f, pulse)
                : new Color(1f, 0.8f, 0.1f, pulse);
            GUI.DrawTexture(new Rect(0, sh*0.42f, sw, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh*0.42f+48, sw, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            warnStyle.normal.textColor = closestDist < dangerThreshold
                ? new Color(1f, 0.3f, 0.2f)
                : new Color(1f, 0.85f, 0f);

            GUI.Label(new Rect(0, sh*0.42f, sw, 50), warningMessage, warnStyle);
        }

        // ── Auto-dodge indicator ──────────────────────────────────────────────
        if (isAutoMoving && mode == GuardianMode.Auto)
        {
            GUI.color = new Color(0.3f, 1f, 0.5f, 0.9f);
            GUI.DrawTexture(new Rect(0, sh*0.42f+52, sw, 26), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var autoStyle = new GUIStyle(msgStyle);
            autoStyle.normal.textColor = new Color(0f, 0.08f, 0f);
            autoStyle.fontStyle        = FontStyle.Bold;
            GUI.Label(new Rect(0, sh*0.42f+52, sw, 26),
                "▶  AUTO-DODGING  ◀", autoStyle);
        }

        // ── Countdown to auto-dodge ───────────────────────────────────────────
        if (mode == GuardianMode.Auto && noInputTimer > 0f && !isAutoMoving
            && closestDist < dangerThreshold)
        {
            float remaining = Mathf.Max(0f, warningTimeout - noInputTimer);
            GUI.color = new Color(0,0,0,0.7f);
            GUI.DrawTexture(new Rect(sw/2f-80, sh*0.50f, 160, 28), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var cdStyle = new GUIStyle(msgStyle);
            cdStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
            cdStyle.fontStyle        = FontStyle.Bold;
            cdStyle.fontSize         = 14;
            GUI.Label(new Rect(sw/2f-80, sh*0.50f, 160, 28),
                $"Auto-dodge in {remaining:F1}s", cdStyle);
        }

        // ── Draw arrow from player toward safe direction ──────────────────────
        if (closestMeteor != null && closestDist < dangerThreshold)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
            float   guiX   = screen.x;
            float   guiY   = Screen.height - screen.y;

            // Dodge direction arrow
            bool   goRight = closestMeteor.position.x >= transform.position.x
                             ? false : true;
            string arrow   = goRight ? "→ →" : "← ←";
            float  arrowX  = goRight
                ? Mathf.Min(guiX + 40, sw - 60)
                : Mathf.Max(guiX - 80, 10);

            var arrowStyle = new GUIStyle();
            arrowStyle.fontSize         = 24;
            arrowStyle.fontStyle        = FontStyle.Bold;
            arrowStyle.normal.textColor = new Color(1f, 0.85f, 0f,
                0.7f + Mathf.Sin(Time.time * 6f) * 0.3f);
            GUI.Label(new Rect(arrowX, guiY - 30, 60, 30), arrow, arrowStyle);
        }
    }
}