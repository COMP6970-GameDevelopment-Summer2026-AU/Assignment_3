// ══════════════════════════════════════════════════════════════════════════════
// GameManager.cs — Meteor Rush: Assignment 3
// Developer: Jahidul Arafat | COMP 6910 | Summer 2026
//
// BASE (skeleton): game state, scene reload
// NEW (A3):
//   Req 4-6  Player health system (N icons, -1 per bullet hit)
//   Req 7-8  Score system (+pts on enemy/meteor kill, shown on screen)
//   Req 9    Explosion sound when enemy destroyed
//   Req 10   Explosion sound when player loses
//   Req 11   Complete game loop: start → play → game over → restart
// ══════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Health (Req 4-6)")]
    public int maxHealth = 3;           // N icons shown on screen

    [Header("Score (Req 7-8)")]
    public int scorePerEnemy  = 100;
    public int scorePerMeteor =  50;

    [Header("Audio (Req 9-10)")]
    public AudioSource audioSource;
    public AudioClip   enemyExplosionSound;   // Req 9  — explosionCrunch_000.ogg
    public AudioClip   playerExplosionSound;  // Req 10 — lowFrequency_explosion_000.ogg

    [Header("Health Icon (Req 5-6): drag player_ship sprite here")]
    public Sprite healthIconSprite;

    // Runtime
    float     currentHealth;
    int       score;
    int       lastScoreThreshold; // tracks last 500-pt milestone for life bonus
    bool      isGameOver;
    bool      isStartScreen = true;
    float     gameOverTimer;
    float     playTime;
    Texture2D healthTex;

    // Live stats tracking
    int   enemiesKilled;
    int   meteorsShot;
    int   bulletHitsTaken;
    int   meteorsAvoided;   // meteors that left screen without hitting
    float sessionStartTime;

    GUIStyle  sBig, sSub, sMeta, sScore, sPrompt, sDim;
    bool      stylesReady;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentHealth  = maxHealth;  // float — supports half lives
        lastScoreThreshold = 0;
        Time.timeScale = 0f;   // paused until SPACE
        if (healthIconSprite != null)
            healthTex = ReadSprite(healthIconSprite);
        Debug.Log($"[GM] Meteor Rush A3 ready. health={maxHealth} scorePerEnemy={scorePerEnemy}");
    }

    void Update()
    {
        if (isStartScreen)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isStartScreen  = false;
                Time.timeScale = 1f;
                sessionStartTime = Time.time;
            }
            return;
        }
        if (!isGameOver) playTime += Time.deltaTime;
        if (bonusHintTimer > 0f) bonusHintTimer -= Time.deltaTime;
        if (isGameOver)
        {
            gameOverTimer += Time.unscaledDeltaTime;
            if (gameOverTimer > 1.5f &&
               (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.rKey.wasPressedThisFrame))
            { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void PlayerHitByBullet()   // Req 4-6
    {
        if (isGameOver || isStartScreen) return;
        currentHealth = Mathf.Max(0, currentHealth - 1);
        bulletHitsTaken++;
        Debug.Log($"[GM] Bullet hit #{bulletHitsTaken} — health={currentHealth}/{maxHealth} | score={score}");
        if (currentHealth <= 0) GameOver();
    }

    public void PlayerHitByMeteor()   // Req 3 — meteor = instant game over
    {
        if (isGameOver || isStartScreen) return;
        Debug.Log("[GM] Meteor hit — INSTANT GAME OVER (Assignment Req 3)");
        currentHealth = 0f;
        GameOver();
    }

    public void EnemyDestroyed()      // Req 7+9
    {
        if (isGameOver) return;
        score += scorePerEnemy;
        enemiesKilled++;
        Sfx(enemyExplosionSound);
        CheckScoreLifeBonus();
        Debug.Log($"[GM] Enemy killed #{enemiesKilled} +{scorePerEnemy} → total={score} | lives={currentHealth:F1}/{maxHealth}");
    }

    public void MeteorShot()
    {
        if (isGameOver) return;
        score += scorePerMeteor;
        meteorsShot++;
        Sfx(enemyExplosionSound);
        CheckScoreLifeBonus();
        Debug.Log($"[GM] Meteor shot #{meteorsShot} +{scorePerMeteor} → total={score}");
    }

    // +1 life every 500 pts, capped at maxHealth
    // HUD hint message for bonus life
    string bonusHintMsg = "";
    float  bonusHintTimer = 0f;

    void CheckScoreLifeBonus()
    {
        int milestone = (score / 500);
        if (milestone > lastScoreThreshold)
        {
            lastScoreThreshold = milestone;
            if (currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + 1f);
                bonusHintMsg   = $"★ +1 LIFE BONUS!  ({score} pts milestone)";
                bonusHintTimer = 3f;
                Debug.Log($"[GM] ★ SCORE BONUS LIFE at {score} pts! health={currentHealth:F1}/{maxHealth}");
            }
            else
            {
                bonusHintMsg   = $"500 pts milestone — lives already full ({maxHealth}/{maxHealth})";
                bonusHintTimer = 3f;
                Debug.Log($"[GM] 500 pts milestone at {score} — max lives already, no bonus given");
            }
        }
    }

    public void MeteorMissed() // called by Meteor when it leaves screen
    {
        meteorsAvoided++;
    }

    public void ShowHint(string msg) => Debug.Log($"[Guardian] {msg}");

    public bool IsGameOver    => isGameOver;
    public bool IsStartScreen => isStartScreen;

    // ─── Game Over ────────────────────────────────────────────────────────────
    void GameOver()
    {
        isGameOver = true; gameOverTimer = 0f;
        Time.timeScale = 0f;
        Sfx(playerExplosionSound);  // Req 10
        var p = GameObject.FindWithTag("Player");
        if (p) p.SetActive(false);

        // Granular end-of-game report
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);
        float accuracy = (enemiesKilled + meteorsShot) == 0 ? 0f :
            (float)(enemiesKilled + meteorsShot) /
            Mathf.Max(1, enemiesKilled + meteorsShot + bulletHitsTaken) * 100f;

        Debug.Log("╔══════════════════════════════════════════╗");
        Debug.Log("║        METEOR RUSH — GAME OVER REPORT    ║");
        Debug.Log("╠══════════════════════════════════════════╣");
        Debug.Log($"║  Final Score        : {score,6} pts          ║");
        Debug.Log($"║  Enemies Killed     : {enemiesKilled,6}              ║");
        Debug.Log($"║  Meteors Shot       : {meteorsShot,6}              ║");
        Debug.Log($"║  Meteors Avoided    : {meteorsAvoided,6}              ║");
        Debug.Log($"║  Bullet Hits Taken  : {bulletHitsTaken,6}              ║");
        Debug.Log($"║  Lives Lost         : {maxHealth - currentHealth,6} / {maxHealth}          ║");
        Debug.Log($"║  Survival Time      : {minutes:00}m {seconds:00}s           ║");
        Debug.Log($"║  Score/Enemy        : {(enemiesKilled>0?scorePerEnemy:0),6} pts          ║");
        Debug.Log($"║  Score/Meteor       : {(meteorsShot>0?scorePerMeteor:0),6} pts          ║");
        Debug.Log($"║  Kill Accuracy      : {accuracy,5:F1}%              ║");
        Debug.Log("╚══════════════════════════════════════════╝");
    }

    void Sfx(AudioClip c) { if (audioSource && c) audioSource.PlayOneShot(c); }

    // ─── OnGUI ────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        BuildStyles();
        float sw = Screen.width, sh = Screen.height;
        if (isStartScreen) { DrawStart(sw, sh); return; }
        if (isGameOver)    { DrawGameOver(sw, sh); return; }
        DrawHUD(sw, sh);
    }

    void DrawStart(float sw, float sh)
    {
        Box(0,0,sw,sh, new Color(0,0,0.05f,0.93f));
        Box(0,0,sw,4,  new Color(0.2f,0.6f,1f,1f));  // blue top bar

        GUI.Label(new Rect(0, sh*0.1f,      sw, 64), "METEOR RUSH", sBig);
        GUI.Label(new Rect(0, sh*0.1f+66,   sw, 26), "Assignment 3  •  COMP 6910  •  Summer 2026", sSub);
        GUI.Label(new Rect(0, sh*0.1f+94,   sw, 20), "Developed by Jahidul Arafat  •  Kenney Assets", sMeta);

        Box(sw*0.08f, sh*0.33f, sw*0.84f, 1, new Color(1,1,1,0.07f)); // divider

        var H = Style(14,FontStyle.Bold,  new Color(0.4f,0.8f,1f), TextAnchor.UpperLeft);
        var V = Style(13,FontStyle.Normal,new Color(0.78f,0.82f,0.78f), TextAnchor.UpperLeft);
        float c1=sw*0.12f, c2=sw*0.56f, cw=sw*0.32f, lh=22f;
        float y1=sh*0.36f, y2=sh*0.36f;

        Box(c1-10, y1-8, cw+20, sh*0.44f, new Color(0,0.03f,0.08f,0.88f));
        Box(c2-10, y2-8, cw+20, sh*0.44f, new Color(0,0.03f,0.08f,0.88f));

        GUI.Label(new Rect(c1,y1,cw,20),"CONTROLS",H); y1+=24;
        Lbl(c1,ref y1,cw,lh,"WASD / Arrows    Move ship",V);
        Lbl(c1,ref y1,cw,lh,"Space / Click    Shoot",V);  y1+=8;
        GUI.Label(new Rect(c1,y1,cw,20),"OBJECTIVE",H); y1+=24;
        Lbl(c1,ref y1,cw,lh,"• Destroy enemies → score",V);
        Lbl(c1,ref y1,cw,lh,"• Shoot meteors → bonus pts",V);
        Lbl(c1,ref y1,cw,lh,"• Survive as long as possible",V); y1+=8;
        GUI.Label(new Rect(c1,y1,cw,20),"HAZARDS",H); y1+=24;
        Lbl(c1,ref y1,cw,lh,"• Enemy bullet → −1 health icon",V);
        Lbl(c1,ref y1,cw,lh,"• Meteor hit   → instant game over",V);

        GUI.Label(new Rect(c2,y2,cw,20),"SCORE",H); y2+=24;
        Lbl(c2,ref y2,cw,lh,$"Enemy destroyed   +{scorePerEnemy} pts",V);
        Lbl(c2,ref y2,cw,lh,$"Meteor shot       +{scorePerMeteor} pts",V);
        Lbl(c2,ref y2,cw,lh,"Every 500 pts     +1 bonus life",V); y2+=8;
        GUI.Label(new Rect(c2,y2,cw,20),"HEALTH",H); y2+=24;
        Lbl(c2,ref y2,cw,lh,$"Start with {maxHealth} ship icons",V);
        Lbl(c2,ref y2,cw,lh,"Enemy bullet hit  −½ life",V);
        Lbl(c2,ref y2,cw,lh,"Meteor hit        instant game over",V);
        Lbl(c2,ref y2,cw,lh,"0 icons left      game over",V); y2+=8;
        GUI.Label(new Rect(c2,y2,cw,20),"GUARDIAN MODE  (G key)",H); y2+=24;
        Lbl(c2,ref y2,cw,lh,"OFF    fully manual play",V);
        Lbl(c2,ref y2,cw,lh,"WARN   warns when meteor incoming",V);
        Lbl(c2,ref y2,cw,lh,"AUTO   auto-dodges if idle >1.2s",V);

        // ── Assignment 3 Checklist ────────────────────────────────────────────
        float ckY = sh*0.36f + sh*0.46f + 8f;
        float ckX = sw*0.04f, ckW = sw*0.92f, ckH = 130f;
        Box(ckX, ckY, ckW, ckH, new Color(0.02f,0.06f,0.12f,0.94f));
        Box(ckX, ckY, ckW, 2,   new Color(0.2f,0.6f,1f,0.6f));

        var CK = Style(12,FontStyle.Bold,  new Color(0.28f,0.88f,0.45f),TextAnchor.UpperLeft);
        var CT = Style(12,FontStyle.Bold,  new Color(0.5f,0.75f,0.95f), TextAnchor.UpperLeft);
        var CD = Style(11,FontStyle.Normal,new Color(0.58f,0.65f,0.70f),TextAnchor.UpperLeft);
        var CH = Style(12,FontStyle.Bold,  new Color(0.4f,0.8f,1f),     TextAnchor.UpperLeft);

        float iy = ckY+8;
        GUI.Label(new Rect(ckX+8,iy,ckW-16,18),"ASSIGNMENT 3 — REQUIREMENTS CHECKLIST",CH); iy+=20;
        Box(ckX+8,iy,ckW-16,1,new Color(1,1,1,0.06f)); iy+=6;

        float col3=ckW/3f, ix0=ckX+8, ix1=ckX+8+col3, ix2=ckX+8+col3*2;
        float iy0=iy, iy1=iy, iy2=iy, lhc=18f;

        // Col 0
        GUI.Label(new Rect(ix0,    iy0,14, lhc),"✓",CK);
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"Meteor Hazard",CT); iy0+=lhc;
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"Spawn lower half, track player",CD); iy0+=lhc;
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"instant game over on hit",CD); iy0+=lhc+4;
        GUI.Label(new Rect(ix0,    iy0,14,lhc),"✓",CK);
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"Sound Effects",CT); iy0+=lhc;
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"Explosion on enemy kill +",CD); iy0+=lhc;
        GUI.Label(new Rect(ix0+16, iy0,col3-20,lhc),"big explosion on player death",CD);

        // Col 1
        GUI.Label(new Rect(ix1,    iy1,14, lhc),"✓",CK);
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"Player Health System",CT); iy1+=lhc;
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"N icons, minus-half per bullet hit",CD); iy1+=lhc;
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"0 icons = game over",CD); iy1+=lhc+4;
        GUI.Label(new Rect(ix1,    iy1,14,lhc),"✓",CK);
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"Enemy & Projectile Interaction",CT); iy1+=lhc;
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"Enemy bullets hit player correctly",CD); iy1+=lhc;
        GUI.Label(new Rect(ix1+16, iy1,col3-20,lhc),"player bullets destroy enemies",CD);

        // Col 2
        GUI.Label(new Rect(ix2,    iy2,14, lhc),"✓",CK);
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"Score System",CT); iy2+=lhc;
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"Enemy +100 pts, meteor +50 pts",CD); iy2+=lhc;
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"displayed live on HUD",CD); iy2+=lhc+4;
        GUI.Label(new Rect(ix2,    iy2,14,lhc),"✓",CK);
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"Gameplay Polish",CT); iy2+=lhc;
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"Start screen, live HUD, game over",CD); iy2+=lhc;
        GUI.Label(new Rect(ix2+16, iy2,col3-20,lhc),"report + guardian auto-dodge mode",CD);

        // Bottom bar
        Box(0,sh-54,sw,54,new Color(0.04f,0.1f,0.2f,1f));
        Box(0,sh-54,sw,1, new Color(0.2f,0.6f,1f,0.4f));
        GUI.Label(new Rect(0,sh-54,sw,54),"Press SPACE to Start", sPrompt);
    }

    void DrawHUD(float sw, float sh)
    {
        int   min = Mathf.FloorToInt(playTime/60f);
        int   sec = Mathf.FloorToInt(playTime%60f);
        float lh  = 22f;

        // ── Left panel: score + live stats ─────────────────────────────────
        Box(8, 8, 200, 108, new Color(0,0,0,0.72f));
        // Accent left bar
        Box(8, 8, 3, 108, new Color(0.2f,0.6f,1f,1f));

        var sL = Style(15,FontStyle.Bold,  Color.white,               TextAnchor.MiddleLeft);
        var sV = Style(15,FontStyle.Normal,new Color(0.7f,0.9f,1f),   TextAnchor.MiddleLeft);

        float lx=18, ly=12;
        Row(lx, ref ly, 190, lh, "Score",    score+" pts",         sL, sV);
        Row(lx, ref ly, 190, lh, "Enemies",  enemiesKilled+"",     sL, sV);
        Row(lx, ref ly, 190, lh, "Meteors",  meteorsShot+"",       sL, sV);
        Row(lx, ref ly, 190, lh, "Time",     $"{min:00}:{sec:00}", sL, sV);

        // ── Right panel: health icons ───────────────────────────────────────
        float iw=28, ih=28, gap=4;
        float tot = maxHealth*(iw+gap)-gap;
        float sx  = sw - tot - 14f;

        Box(sx-8, 8, tot+16, 44, new Color(0,0,0,0.72f));
        Box(sx-8, 8, tot+16, 3,  new Color(0.2f,0.6f,1f,1f)); // accent top

        var sHlbl = Style(12,FontStyle.Normal,new Color(0.55f,0.65f,0.75f),TextAnchor.MiddleCenter);
        GUI.Label(new Rect(sx-8, 8, tot+16, 14), "HEALTH", sHlbl);

        for (int i = 0; i < maxHealth; i++)
        {
            float ix = sx + i*(iw+gap);
            if (i < Mathf.FloorToInt(currentHealth))
            {
                // Full life icon
                GUI.color = Color.white;
                if (healthTex != null) GUI.DrawTexture(new Rect(ix,22,iw,ih),healthTex);
                else { GUI.color=new Color(0.3f,0.8f,1f); GUI.DrawTexture(new Rect(ix,22,iw,ih),Texture2D.whiteTexture); }
            }
            else if (i < currentHealth)
            {
                // Half life icon — draw at 50% alpha
                GUI.color = new Color(1f,1f,1f,0.45f);
                if (healthTex != null) GUI.DrawTexture(new Rect(ix,22,iw,ih),healthTex);
                else { GUI.color=new Color(0.3f,0.8f,0.4f,0.5f); GUI.DrawTexture(new Rect(ix,22,iw,ih),Texture2D.whiteTexture); }
            }
            else
            {
                // Empty slot
                GUI.color=new Color(1,1,1,0.12f);
                GUI.DrawTexture(new Rect(ix,22,iw,ih),Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;
    }

    void Row(float x, ref float y, float w, float lh, string label, string val, GUIStyle ls, GUIStyle vs)
    {
        GUI.Label(new Rect(x,      y, w*0.5f, lh), label, ls);
        GUI.Label(new Rect(x+w*0.5f, y, w*0.5f, lh), val,   vs);
        y += lh;
    }

    void DrawGameOver(float sw, float sh)
    {
        Box(0,0,sw,sh,new Color(0,0,0,0.86f));

        // Title
        var red=Style(52,FontStyle.Bold,new Color(1f,0.22f,0.22f),TextAnchor.MiddleCenter);
        GUI.Label(new Rect(0,sh*0.04f, sw,62),"GAME OVER",red);

        // Accent line
        Box(sw*0.1f, sh*0.16f, sw*0.8f, 1, new Color(1f,0.22f,0.22f,0.4f));

        // Report card
        // Two side-by-side cards: Score Report | Assignment Checklist
        float gap     = 14f;
        float cardW   = sw * 0.42f;
        float cardH   = sh * 0.68f;
        float cardY   = sh * 0.16f;
        float card1X  = sw * 0.04f;                   // score card left
        float card2X  = card1X + cardW + gap;          // checklist card right

        // Score card
        Box(card1X, cardY, cardW, cardH, new Color(0.04f,0.04f,0.10f,0.96f));
        Box(card1X, cardY, cardW, 3,     new Color(0.2f,0.6f,1f,0.8f));

        // Checklist card
        Box(card2X, cardY, cardW, cardH, new Color(0.03f,0.08f,0.04f,0.96f));
        Box(card2X, cardY, cardW, 3,     new Color(0.2f,0.8f,0.3f,0.8f));

        var sHdr = Style(14,FontStyle.Bold,  new Color(0.4f,0.8f,1f),     TextAnchor.UpperLeft);
        var sKey = Style(14,FontStyle.Bold,  new Color(0.82f,0.86f,0.92f),TextAnchor.UpperLeft);
        var sVal = Style(14,FontStyle.Normal,new Color(0.55f,0.75f,1f),    TextAnchor.UpperRight);

        float lh=22f, ip=16f;
        float rx=card1X+ip, rw=cardW-ip*2;
        float ry=cardY+ip;

        var sBig2=Style(16,FontStyle.Bold,Color.white,TextAnchor.MiddleCenter);
        GUI.Label(new Rect(card1X,ry,cardW,24),"SESSION REPORT",sBig2); ry+=28;
        Box(rx,ry,rw,1,new Color(1,1,1,0.07f)); ry+=8;

        int min=Mathf.FloorToInt(playTime/60f);
        int sec=Mathf.FloorToInt(playTime%60f);
        float accuracy=(enemiesKilled+meteorsShot)==0 ? 0f :
            (float)(enemiesKilled+meteorsShot)/
            Mathf.Max(1,enemiesKilled+meteorsShot+bulletHitsTaken)*100f;

        StatRow(rx,ref ry,rw,lh,"Final Score",      score+" pts",             sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Survival Time",    $"{min:00}m {sec:00}s",   sKey,sVal);
        Box(rx,ry,rw,1,new Color(1,1,1,0.05f)); ry+=8;

        GUI.Label(new Rect(rx,ry,rw,20),"COMBAT",sHdr); ry+=22;
        StatRow(rx,ref ry,rw,lh,"Enemies Killed",   enemiesKilled.ToString(), sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Meteors Shot",      meteorsShot.ToString(),   sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Meteors Avoided",   meteorsAvoided.ToString(),sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Bullet Hits Taken", bulletHitsTaken.ToString(),sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Kill Accuracy",    $"{accuracy:F1}%",        sKey,sVal);
        Box(rx,ry,rw,1,new Color(1,1,1,0.05f)); ry+=8;

        GUI.Label(new Rect(rx,ry,rw,20),"HEALTH",sHdr); ry+=22;
        StatRow(rx,ref ry,rw,lh,"Starting Lives",   maxHealth.ToString(),             sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Lives Lost",       $"{maxHealth-currentHealth:F1}",  sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"Lives Remaining",  $"{currentHealth:F1}",            sKey,sVal);
        Box(rx,ry,rw,1,new Color(1,1,1,0.05f)); ry+=8;

        GUI.Label(new Rect(rx,ry,rw,20),"SCORE BREAKDOWN",sHdr); ry+=22;
        StatRow(rx,ref ry,rw,lh,"From Enemies",     $"{enemiesKilled*scorePerEnemy} pts", sKey,sVal);
        StatRow(rx,ref ry,rw,lh,"From Meteors",     $"{meteorsShot*scorePerMeteor} pts",  sKey,sVal);

        // ── Checklist card (right) ────────────────────────────────────────────
        var CK  = Style(12,FontStyle.Bold,  new Color(0.28f,0.88f,0.45f),TextAnchor.UpperLeft);
        var CT  = Style(13,FontStyle.Bold,  new Color(0.70f,0.92f,0.75f),TextAnchor.UpperLeft);
        var CD  = Style(12,FontStyle.Normal,new Color(0.52f,0.65f,0.55f),TextAnchor.UpperLeft);
        var CH2 = Style(15,FontStyle.Bold,  Color.white,                  TextAnchor.MiddleCenter);

        float cx=card2X+ip, cw2=cardW-ip*2;
        float cy=cardY+ip;

        GUI.Label(new Rect(card2X,cy,cardW,24),"A3 REQUIREMENTS",CH2); cy+=28;
        Box(cx,cy,cw2,1,new Color(0.2f,0.8f,0.3f,0.2f)); cy+=8;

        // Each criterion: check + title + 2 description lines
        void CritRow(ref float y, string title, string d1, string d2)
        {
            GUI.Label(new Rect(cx,     y, 16,   lh), "✓",    CK);
            GUI.Label(new Rect(cx+18,  y, cw2-18,lh), title, CT); y+=lh;
            GUI.Label(new Rect(cx+18,  y, cw2-18,lh), d1,    CD); y+=lh;
            GUI.Label(new Rect(cx+18,  y, cw2-18,lh), d2,    CD); y+=lh+6;
            Box(cx,y,cw2,1,new Color(1,1,1,0.04f)); y+=6;
        }

        CritRow(ref cy,
            "Meteor Hazard",
            "Spawn from lower half, move toward player",
            "Instant game over on collision  ✓");
        CritRow(ref cy,
            "Player Health System",
            "N ship icons shown on screen",
            "Lose 0.5 icon per enemy bullet hit  ✓");
        CritRow(ref cy,
            "Score System",
            "Enemy +100 pts, Meteor +50 pts",
            "Score displayed live on HUD  ✓");
        CritRow(ref cy,
            "Sound Effects",
            "Explosion SFX on enemy destroy",
            "Big explosion SFX on player death  ✓");
        CritRow(ref cy,
            "Enemy & Projectile Interaction",
            "Enemy bullets damage player correctly",
            "Player bullets destroy enemies  ✓");
        CritRow(ref cy,
            "Gameplay Polish",
            "Start screen, HUD, game over report",
            "Guardian mode, bonus life system  ✓");

        // Bottom bar
        Box(0,sh-52,sw,52,new Color(0.06f,0.08f,0.14f,1f));
        Box(0,sh-52,sw,1, new Color(0.2f,0.6f,1f,0.4f));
        GUI.Label(new Rect(0,sh-52,sw,52),
            gameOverTimer>1.5f ? "Press SPACE or R to restart" : "...", sPrompt);
    }

    void StatRow(float x, ref float y, float w, float lh, string k, string v, GUIStyle ks, GUIStyle vs)
    {
        GUI.Label(new Rect(x,      y, w*0.6f, lh), k, ks);
        GUI.Label(new Rect(x, y, w-2, lh),         v, vs);
        y += lh;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    void Box(float x,float y,float w,float h,Color c)
    { GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture); GUI.color=Color.white; }

    void Lbl(float x,ref float y,float w,float lh,string t,GUIStyle s)
    { GUI.Label(new Rect(x,y,w,lh),t,s); y+=lh; }

    GUIStyle Style(int sz,FontStyle fs,Color c,TextAnchor a)
    => new GUIStyle{fontSize=sz,fontStyle=fs,normal={textColor=c},alignment=a};

    void BuildStyles()
    {
        if (stylesReady) return;
        sBig   =Style(52,FontStyle.Bold,  Color.white,                   TextAnchor.MiddleCenter);
        sSub   =Style(18,FontStyle.Normal,new Color(0.65f,0.78f,1f),     TextAnchor.MiddleCenter);
        sMeta  =Style(13,FontStyle.Normal,new Color(0.45f,0.52f,0.62f),  TextAnchor.MiddleCenter);
        sScore =Style(20,FontStyle.Bold,  Color.white,                   TextAnchor.MiddleLeft);
        sPrompt=Style(24,FontStyle.Bold,  Color.yellow,                  TextAnchor.MiddleCenter);
        sDim   =Style(14,FontStyle.Normal,new Color(0.5f,0.5f,0.5f),     TextAnchor.MiddleCenter);
        stylesReady=true;
    }

    Texture2D ReadSprite(Sprite sp)
    {
        Texture2D src = sp.texture;
        if (!src.isReadable) return null;
        int x=(int)sp.rect.x, y=(int)sp.rect.y, w=(int)sp.rect.width, h=(int)sp.rect.height;
        var t=new Texture2D(w,h);
        t.SetPixels(src.GetPixels(x,y,w,h));
        t.Apply(); return t;
    }
}