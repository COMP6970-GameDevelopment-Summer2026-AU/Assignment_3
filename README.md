# Meteor Rush — Assignment 3 🚀

> **⚠ IMPORTANT: Read before cloning — Git LFS required**
>
> This project uses **Git LFS (Large File Storage)** to store binary assets
> (PNG sprites, OGG audio, Unity prefabs, scene files).
> Without LFS, you will see this error in Unity:
> ```
> Could not create asset from Assets/Sprites/...png: File could not be read
> ```
>
> **To reproduce locally:**
> ```bash
> # Step 1 — Install Git LFS (one time per machine)
> git lfs install
>
> # Step 2 — Clone the repository
> git clone https://github.com/COMP6970-GameDevelopment-Summer2026-AU/Assignment_3
> cd Assignment_3
>
> # Step 3 — Pull LFS assets (if not auto-downloaded)
> git lfs pull
>
> # Step 4 — Open in Unity
> # File → Open Project → select the cloned folder
> # Wait for Unity to import all assets (~1-2 min first time)
>
> # Step 5 — Open the scene
> # Project panel → Assets/Scenes → double-click SampleScene
>
> # Step 6 — Press Play ▶
> ```
>
> **Verify assets loaded correctly:**
> - Console should show NO "File could not be read" errors
> - You should see: `[GM] Meteor Rush A3 ready. health=3`
> - Press SPACE on the start screen — game begins

---


> **Assignment 3** — 2D Arcade Shooter Extension
> **Course:** COMP 6910 Game Development | Summer 2026
> **Repository:** https://github.com/COMP6970-GameDevelopment-Summer2026-AU/Assignment_3
> **Developer:** Jahidul Arafat — PhD Student, CSSE, Auburn University
> **Fellowship:** Presidential & Woltosz Graduate Research Fellow
> **Industry:** Former L3 Senior Solution Architect (MLOps), Oracle (Singapore)

---

## Watch the Game

> *(Add YouTube demo link here after recording)*

---

## Summary

**Meteor Rush** extends the base MeteorRush skeleton into a complete arcade shooter with all 11 assignment requirements implemented. New systems include a meteor hazard that tracks the player and causes instant game over, a float-based health system showing N player ship icons that lose half-icons per enemy bullet hit, a live score system with enemy and meteor kill points displayed on the HUD, explosion sound effects on enemy death and player defeat, a bonus life awarded every 500 points, and a Guardian Mode (G key) that warns players about incoming meteors and optionally auto-dodges. The game features a professional start screen with a requirements checklist, a live session HUD, and a two-column game over report showing both session stats and the A3 rubric checklist.

---

## Assignment 3 Requirements

| # | Criterion | Implementation | Status |
|---|-----------|---------------|--------|
| 1 | Meteors spawn from lower half of screen | `MeteorSpawner.cs` — y=-7.5 to -5.5 | ✅ |
| 2 | Meteors move toward the player | `Meteor.cs` — Vector3.Lerp tracking | ✅ |
| 3 | Meteor hits player → game ends and restarts | `Meteor.OnTriggerEnter2D` → `GameManager.PlayerHitByMeteor()` — instant game over | ✅ |
| 4 | Player health system (N hits before losing) | `GameManager.cs` — float health, maxHealth=3 | ✅ |
| 5 | Display health using N player icons | `GameManager.DrawHUD()` — player_ship sprite icons, top-right | ✅ |
| 6 | Remove icon on enemy bullet hit | `EnemyBullet` → `PlayerController.OnHit()` → `GameManager.PlayerHitByBullet()` | ✅ |
| 7 | Score increases when enemies destroyed | `Bullet.OnTriggerEnter2D` → `GameManager.EnemyDestroyed()` → +100 pts | ✅ |
| 8 | Score displayed on screen | `GameManager.DrawHUD()` — live score top-left | ✅ |
| 9 | Explosion sound when enemy destroyed | `GameManager.EnemyDestroyed()` → `explosionCrunch_000.ogg` | ✅ |
| 10 | Explosion sound when player loses | `GameManager.GameOver()` → `lowFrequency_explosion_000.ogg` | ✅ |
| 11 | Complete gameplay loop | Start screen → Play → Game Over → Restart (SPACE / R) | ✅ |

---

## Enhancements (Beyond Requirements)

| Feature | Detail |
|---------|--------|
| Float health system | Lives stored as float — supports half-life deductions |
| Half-life per bullet | Enemy bullet = −0.5 life (not full −1) |
| Bonus life at 500 pts | Every 500 score milestone = +1 life, capped at maxHealth |
| HUD bonus hint | On-screen message when bonus life awarded or lives already full |
| Meteor size reduced 60% | `transform.localScale *= 0.4f` + collider radius = 0.2 |
| Meteor speed | Slow (0.5–1.25 units/s) — fair and avoidable |
| Meteor difficulty ramp | Spawn rate increases over time (min 0.8s interval) |
| Meteor avoided tracking | Counts meteors that left screen without hitting player |
| Guardian Mode | G key cycles OFF → WARN → AUTO |
| Guardian WARN | Arrow + pulsing banner when meteor within danger threshold |
| Guardian AUTO | Auto-dodges player if no input for 1.2s |
| Guardian countdown | Shows "Auto-dodge in 0.8s" timer on screen |
| Player full-screen movement | Bounds auto-calculated from `Camera.main.orthographic` |
| Player hit flash | Red sprite flash on bullet hit |
| Live HUD | Score, enemies killed, meteors shot, survival time — all live |
| Game over report | Two-column: Session Stats + A3 Requirements Checklist |
| Kill accuracy | Calculated from kills vs hits taken |
| Score breakdown | Enemies score vs meteors score shown separately |
| Professional start screen | Dark card layout, all rules, guardian mode explained |
| A3 checklist on start | 6 requirements with ✓ shown before game starts |

---

## Game Rules

### Health
| Event | Effect |
|-------|--------|
| Start | 3 lives (float) |
| Enemy bullet hit | −0.5 life |
| 0 lives remaining | Game over |
| Meteor hit | **Instant game over** (Req 3) |
| Every 500 pts | +1 bonus life (capped at maxHealth=3) |

### Score
| Event | Points |
|-------|--------|
| Enemy ship destroyed | +100 pts |
| Meteor shot | +50 pts |

### Guardian Mode (G key)
| Mode | Behaviour |
|------|-----------|
| OFF | Fully manual — no assistance |
| WARN | Pulsing banner + directional arrow when meteor is within 2.5 units |
| AUTO | Warns + auto-moves player sideways if no input for 1.2s |

---

## Controls

| Key | Action |
|-----|--------|
| WASD / Arrow Keys | Move ship |
| Space / Left Click | Shoot |
| G | Cycle Guardian Mode (OFF → WARN → AUTO) |
| R | Restart (on game over screen) |
| Space | Start game / Restart after game over |

---

## Architecture

```
GameManager.cs       — Singleton. Health, score, SFX, OnGUI (all screens + HUD)
PlayerController.cs  — Movement clamped to camera bounds, shoot, OnHit()
Enemy.cs             — Sine-wave patrol, fires EnemyBullet at fireRate
EnemySpawner.cs      — Spawns enemies from left/right edge, upper half
Bullet.cs            — Player projectile, notifies GameManager on kill
EnemyBullet.cs       — Enemy projectile, calls PlayerController.OnHit()
Meteor.cs            — NEW: spawns lower half, tracks player, instant game over
MeteorSpawner.cs     — NEW: spawns meteors with difficulty ramp
MeteorGuardian.cs    — NEW: warn/auto-dodge assistant (G key toggle)
```

---

## File Structure

```
Assets/
├── Scripts/
│   ├── GameManager.cs        central state, health, score, all OnGUI
│   ├── PlayerController.cs   movement, shoot, hit response
│   ├── Enemy.cs              patrol + shoot (base)
│   ├── EnemySpawner.cs       enemy spawning (base)
│   ├── Bullet.cs             player bullet + kill notification
│   ├── EnemyBullet.cs        enemy bullet + player hit notification
│   ├── Meteor.cs             NEW — meteor hazard (Req 1-3)
│   ├── MeteorSpawner.cs      NEW — meteor spawning with difficulty ramp
│   └── MeteorGuardian.cs     NEW — warn/auto-dodge assistant
│
├── Prefabs/
│   ├── Bullet.prefab
│   ├── Enemy.prefab
│   ├── EnemyBullet.prefab
│   └── Meteor.prefab         NEW — spaceMeteors_001.png, CircleCollider2D trigger
│
├── Sprites/
│   ├── player_ship.png       used for health icons
│   ├── enemy_ship.png
│   ├── meteors_001.png       meteor sprite
│   └── background.jpeg
│
└── Audio/
    ├── player_laser.ogg      player shoot
    ├── enemy_laser.ogg       enemy shoot
    ├── explosion.ogg         enemy death
    ├── player_hit.ogg        player bullet hit
    ├── explosionCrunch_000.ogg   enemy explosion (Kenney Sci-Fi)
    └── lowFrequency_explosion_000.ogg  player death (Kenney Sci-Fi)
```

---

## Setup

```
1. Open SampleScene
2. Add scripts to GameObjects (see table below)
3. Create Meteor prefab (see below)
4. Press Play ▶
```

### Script Attachment

| GameObject | Scripts |
|-----------|---------|
| `_GameManager` | `GameManager` + `AudioSource` |
| `Player` | `PlayerController`, `MeteorGuardian` |
| `Spawner` | `EnemySpawner`, `MeteorSpawner` |
| Bullet prefab | `Bullet` |
| Enemy prefab | `Enemy` |
| EnemyBullet prefab | `EnemyBullet` |
| Meteor prefab | `Meteor` |

### GameManager Inspector Slots

| Slot | File |
|------|------|
| `Enemy Explosion Sound` | `explosionCrunch_000.ogg` |
| `Player Explosion Sound` | `lowFrequency_explosion_000.ogg` |
| `Health Icon Sprite` | `player_ship.png` (set Texture Type = Sprite first) |

### Meteor Prefab Setup

1. Hierarchy → right-click → **2D Object → Sprite**
2. Set sprite → `spaceMeteors_001.png`
3. Tag → `Meteor`
4. Add `Rigidbody2D` → Body Type = **Kinematic**
5. Add `CircleCollider2D` → **Is Trigger ✅** → Radius = `0.2`
6. Add Component → `Meteor`
7. Drag to `Assets/Prefabs/` → delete from Hierarchy
8. Drag prefab into `MeteorSpawner → Meteor Prefab` slot

---

## Debug Console Reference

```
[GM]            Meteor Rush A3 ready. health=3 scorePerEnemy=100
[Player]        Bounds set from camera: X=-5.8..5.8 Y=-4.7..4.7
[MeteorSpawner] Spawn zone reset: Y=-7.5 to -5.5, firstSpawn in 4s
[Guardian]      Started in mode: Off. Press G to cycle modes.
[MeteorSpawner] Spawned meteor at (-1.1, -6.5)
[Meteor]        Scale=0.40 collider=0.20
[GM]            Enemy killed #1 +100 → total=100 | lives=3.0/3
[GM]            Meteor shot #1 +50 → total=150
[GM]            Bullet hit #1 — health=2.5/3
[Guardian]      Mode switched to: WarnOnly
[Guardian]      AUTO-DODGE triggered → target=(2.1, -1.5)
[GM]            ★ SCORE BONUS LIFE at 500 pts! health=3.0/3
[Meteor]        Hit player — game over!
[GM]            GAME OVER — score=550
╔══════════════════════════════════════════╗
║  Final Score        :    550 pts         ║
║  Enemies Killed     :      4             ║
║  Meteors Shot       :      3             ║
║  Kill Accuracy      : 100.0%             ║
╚══════════════════════════════════════════╝
```

---

## Credits

| Asset | Source | License |
|-------|--------|---------|
| Space Shooter Extension sprites | [Kenney Space Shooter Extension](https://kenney.nl/assets/space-shooter-extension) | CC0 Public Domain |
| Sci-Fi sound effects | [Kenney Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | CC0 Public Domain |
| Project skeleton | [MeteorRush-Skeleton](https://github.com/ajariwala1/MeteorRush-Skeleton) | MIT |

---

*Meteor Rush — COMP 6970 Game Development, Auburn University, Summer 2026*