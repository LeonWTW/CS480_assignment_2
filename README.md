# CS480 Assignment 2 — Haunted Jaunt (Modified)

This is my Assignment 2 for CS480. I started from the Unity "3D Beginner: John Lemon's Haunted Jaunt — Complete" tutorial project and added a few new gameplay elements on top of it so the player gets more feedback about the ghosts in the house.

The four new things I added, as required by the assignment:
- A dot product "danger" warning
- A lerp-based arrow that points at the nearest ghost
- A new blue ghost-fire particle effect
- A heartbeat sound effect that plays when the danger warning triggers

## How to Run

1. Open the project in Unity Hub using editor version **6000.3.11f1**.
2. Open the scene `Assets/UnityTechnologies/3DBeginnerTutorialComplete/Scenes/MainScene.unity`.
3. Press Play.
4. Move John Lemon with the arrow keys or WASD. The goal of the base game is to sneak out of the haunted house without getting caught by a ghost or gargoyle.

The scene relies on two tags: `Player` (on JohnLemon) and `Enemy` (on every Ghost and Gargoyle).

---

## 1. Dot Product — `DangerDetector.cs`

**What it does:** When the player is directly facing a ghost within 15 meters, a "⚠ DANGER ⚠" text shows up above John Lemon's head and the heartbeat sound starts. When the player looks away or moves far enough, the warning goes away.

**How the dot product is used:** Every frame, for each enemy in range, the script does:

```csharp
Vector3 directionToEnemy = (enemy.position - transform.position).normalized;
float dotResult = Vector3.Dot(transform.forward, directionToEnemy);
```

The dot product between the player's forward direction and the direction-to-enemy gives a value between -1 and 1:
- `1.0` = enemy is right in front
- `0.0` = enemy is off to the side (90°)
- `-1.0` = enemy is behind

If `dotResult > 0.7`, the enemy is inside roughly a 45° cone in front of the player, so we count that as "looking at it".

**Trigger:** The player's facing direction. Looking at an enemy → DANGER turns on. Looking away → DANGER turns off. There's also a distance gate (≤ 15 m) so far-away ghosts don't count.

---

## 2. Linear Interpolation — `EnemyRadar.cs` + the Arrow prefab

**What it does:** A small glowing arrow floats above John Lemon's head. It always points at the nearest ghost, and its color shifts from green (safe) to red (about to die) based on how close that ghost is. It helps the player figure out where trouble is even through walls.

Lerp is used in three places in this script:

**a) `Vector3.Lerp`** — smoothly keeps the arrow above John Lemon's head each frame instead of snapping:
```csharp
transform.position = Vector3.Lerp(transform.position, headPos, 12f * Time.deltaTime);
```

**b) `Quaternion.Slerp`** — the spherical version of lerp, used for rotations. Smoothly turns the arrow toward the nearest enemy:
```csharp
transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
```

**c) `Color.Lerp`** — blends the arrow's color based on distance:
```csharp
float t = Mathf.InverseLerp(20f, 3f, nearestDistance);
Color c = Color.Lerp(safeColor, dangerColor, t);
```
At ≥ 20 m the arrow is pure green, at ≤ 3 m it's pure red, and it fades in between.

**Trigger:** All three run every frame based on the current state — the player's position, the nearest enemy's position, and the distance between them. The arrow is active from the moment the scene starts.

---

## 3. Particle Effect — `GhostFireWisp.cs` + `GhostFireSpawner.cs`

**What it does:** 5 blue ghost-fire flames spawn at random spots around the map and drift around slowly. They pop in and out of existence randomly, like spooky wisps floating through a haunted house. They don't hurt the player — they're just visual atmosphere — and they can pass through walls.

**How the particle effect is built:**
- A narrow **cone shape** pointing upward so particles form a flame silhouette instead of a cloud
- Local simulation space so each flame stays concentrated on its wisp
- **Color over lifetime** going from bright white-blue → mid blue → dark fade
- **Size over lifetime** tapering to a point to give the flame tip
- **Noise** module for organic flicker
- Additive blending so they glow in the dark hallways

**Triggers (three of them):**

1. **Scene-start trigger** — `GhostFireSpawner.Start()` spawns 5 wisps at random positions the moment the scene begins playing.
2. **Timer trigger (appear / disappear)** — Each wisp has its own random timer: visible 4-8 seconds, hidden 1-3 seconds. When it goes hidden the `ParticleSystem` is `Stop()`'d; when it comes back it teleports to a new random spot and `Play()`'s again.
3. **Waypoint trigger** — Every few seconds (or when it reaches its current target), the wisp picks a new random point in the bounds and drifts toward it.

This makes the wisps feel like they're alive and drifting around, not just static decoration.

---

## 4. Sound Effect — heartbeat via `DangerDetector.cs`

**What it does:** When the player is facing an enemy (same condition as the dot product warning), a looping "I See You" heartbeat clip plays to let the player know something is watching them. The sound stops the moment the player looks away.

**How it's set up:**
At `Start()`, `DangerDetector` adds an `AudioSource` component to John Lemon at runtime, assigns `ISeeYou.mp3` to it, sets loop = true and playOnAwake = false:

```csharp
audioSource = gameObject.AddComponent<AudioSource>();
audioSource.clip = dangerSound;
audioSource.loop = true;
audioSource.playOnAwake = false;
```

**Trigger:** Same dot-product facing check as element 1. When the player starts facing an enemy, `audioSource.Play()` fires. When the player stops facing an enemy, `audioSource.Stop()` fires. The sound is synced with the DANGER UI text.

---

## Team Members

This project was done solo.

- **Leon Wong** — did everything: forked the base tutorial project, wrote all four custom runtime scripts (`DangerDetector.cs`, `EnemyRadar.cs`, `GhostFireWisp.cs`, `GhostFireSpawner.cs`), wrote the editor helper scripts (`ArrowPrefabBuilder.cs`, `GhostFireBuilder.cs`) that build the Arrow and ghost-fire prefabs from Unity primitives, wired up the scene (tags, UI canvas, prefab instances), and wrote this README.
