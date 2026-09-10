# Designer Guide

How this game is put together, and how to build content for it without touching C#.

Written for whoever picks the project up next. Read the first two sections even if you only
want one recipe — most of the traps in this codebase come from not knowing which system owns
what.

- Unity **6000.2.10f1**, Universal Render Pipeline 17.2.0
- Everything is authored in the Inspector via ScriptableObjects and prefabs
- Scripts live in `Assets/Script_folder`, editor tools in `Assets/editor`

---

## 1. The big picture

There are two independent state machines. They are deliberately different shapes, and that
difference is the single most important thing to understand about this codebase.

| | NPCs | Player |
|---|---|---|
| Entry point | `EnemyBase` | `PlayerStateMachine` |
| Shape | Classic FSM — one class per state | Coordinator over existing systems |
| States | `EnemyIdle`, `EnemyMove`, `EnemyAttack`, `EnemyDeath` | `Idle`, `Move`, `Attack`, `Dash`, `Consume`, `Death` (enum) |
| Owns behaviour? | Yes — the states *do* the work | No — it observes and arbitrates |
| Movement | `NavMeshAgent` only | `NavMeshAgent` + a ledge-fall handoff |

**Why the difference?** The NPCs had no working behaviour at all, so their states own their
logic outright. The player's systems (`ComboRunner`, `AbilityRunner`, `PlayerConsume`,
`PlayerMove`, `PlayerDeathHandler`) already worked and were already decoupled. Rewriting
those into state classes would have deleted working code to arrive back where it started, so
`PlayerStateMachine` coordinates them instead: it derives one authoritative state per frame,
owns the animator parameters, and arbitrates the rules between systems.

### Runtime flow of a level

1. The player walks into an `EnemyManager` trigger volume.
2. `EnemyManager` fires `OnCombatStarted` and spawns wave 1 at its spawn points.
3. Each spawned enemy runs its own `EnemyBase` state machine.
4. Enemies damage the player through `EnemyHitbox`; the player damages enemies through
   `AttackHitbox`. Both call `StatsManager.TakeDamage`, so dodge/armour/death behave
   identically in both directions.
5. When an enemy's health hits 0, `StatsManager.OnDied` fires. `LootDropper` drops loot and
   `EnemyManager` stops tracking it. `EnemyDeath` plays the death clip and destroys the body.
6. When the list empties, the next wave spawns. After the last wave, `OnAllWavesCleared`.
7. If the *player* dies, `PlayerDeathHandler` disables every player system and spawns the
   death screen.

### Scenes

`ObjectLibrary`, `MainMenu`, `hub`, and `forest level 01`–`05` are in Build Settings.
`MainMenu` and `hub` are the two that `PlayerDeathHandler` loads by name.

---

## 2. Making a new enemy

Everything below is Inspector work. No scripting.

### 2.1 Build the prefab

1. Drag your model into the scene. Add these to the **root**:
   - `EnemyBase`
   - `StatsManager` (assign a `Stats` asset — see §4)
   - `NavMeshAgent`
   - `LootDropper` (optional)
   - A solid (non-trigger) collider for the body
2. Add **two child GameObjects**, each with a **sphere collider that has `Is Trigger` ticked**:
   - One with `EnemyAggroCheck` — radius = how far it can notice you
   - One with `EnemyStrikeDistanceCheck` — radius = how close before it swings
   - Assign the `enemy` field on both to the root `EnemyBase`
3. Set the enemy's layer to **Enemy**.
4. On `EnemyBase`, set `Sight Range` to roughly the aggro collider's radius.
5. Save as a prefab. Copy `slime.prefab` or `mushroom scout.prefab` if you want a known-good
   starting point.

> **The single most common mistake:** forgetting `Is Trigger` on the two detection spheres.
> They are `OnTriggerEnter`-based, so without it the enemy simply never notices the player and
> stands there forever. This is exactly what was wrong with the slime.

### 2.2 Tune it

On `EnemyBase`:

**Move Tuning**
| Field | What it does |
|---|---|
| `Move Speed` | Base chase speed. The `StatsManager` MoveSpeed stat multiplies this, so slows and haste work. |
| `Acceleration` | Low = floaty, high = snappy. |
| `Angular Speed` | Turn rate, degrees/sec. |
| `Stopping Distance` | **Keep below the strike collider radius**, or the enemy halts just out of range and never attacks. |
| `Repath Interval` | Seconds between destination updates. 0.15–0.25 looks identical to every frame and costs far less. |

**Attack Tuning**
| Field | What it does |
|---|---|
| `Time Between Attacks` | Swing cadence. |
| `Time To Exit After Attack` | How long the target must stay away before it gives up and chases again. Acts as hysteresis — don't set it to 0 or the enemy flickers between chasing and attacking. |
| `Distance To Count Exit` | Distance at which the target counts as escaped. |
| `Pivot Speed` | Circling speed. Only used when `Does Attack Pivot` is ticked. |

**Other**
- `Random Movement Range` — how far it wanders from its spawn point when idle. **0 = a sentry that holds position.**
- `Is Boss` — excludes it from guaranteed-kill effects.

### 2.3 Make it deal damage

Add a child where the attack lands (claw, mouth, body) with a trigger collider and an
`EnemyHitbox`. Set `Player Layer` to **Default** (see the gotcha in §6).

Then choose one of:

- **With an attack animation:** put `EnemyAnimationEventRelay` on the Animator's GameObject,
  add `EnableHitbox` and `DisableHitbox` animation events to the attack clip, and **untick**
  `Use Swing Window`.
- **Without one yet:** leave `Use Swing Window` ticked. The hitbox opens on a timer whenever
  the enemy swings, so combat is playable before animation exists.

### 2.4 Bake a NavMesh

**Enemies cannot move without one.** Bake a NavMesh (or add a `NavMeshSurface`) covering the
floor of every room that spawns enemies. If you forget, you'll see a one-time console warning
naming the enemy.

---

## 3. Animation

### 3.1 The parameter contracts

Two contracts, defined in code so a typo is a compile error rather than an animation that
silently never plays. Every enemy controller must expose all four:

**Enemies** — `EnemyAnimatorParams`

| Parameter | Type | Written by | Meaning |
|---|---|---|---|
| `Speed` | Float | `EnemyMove`, `EnemyIdle` | Agent speed. Drives idle↔walk. |
| `Attack` | Trigger | `EnemyAttack` | One per swing. |
| `Hit` | Trigger | — | Reserved for damage reactions. |
| `Dead` | Bool | `EnemyDeath` | Latched true; never cleared. |

**Player** — `PlayerAnimatorParams`

| Parameter | Type | Written by | Meaning |
|---|---|---|---|
| `Speed` | Float | `PlayerStateMachine` | Horizontal speed. |
| `Grounded` | Bool | `PlayerStateMachine` | False mid-ledge-fall. |
| `Dead` | Bool | `PlayerStateMachine` | Latched true. |
| `Attack` / `WizardAttack` | Trigger | `ComboRunner` | **Exception** — see below. |

> **The one exception to "the FSM owns the animator":** `ComboRunner` writes its own attack
> triggers. Their names come from `ComboSO` data (each hit carries its own `animatorTrigger`),
> which is the entire point of the data-driven combo system, so they can't be constants.
> `PlayerStateMachine` reads `ComboRunner.IsAttacking` rather than writing it. Everything else
> that writes an animator parameter is a bug.

The player state machine skips any parameter its controller doesn't declare, so a
half-finished controller degrades quietly instead of burying the console in warnings.

### 3.2 Baked FBX takes are the animation source of truth

Each character already ships with skeletal Idle / Walk / Attack (or Slash) / Hit / Death
takes **baked into its FBX** under `Assets/Fbx_exports/`. Controllers must play **those**
clips from **that** character’s export — not Mixamo retargets onto a different mesh, and not
the old transform-bob placeholders under `Assets/animations/Generated/` or
`PlayerIdle.anim`.

| Character | Baked FBX |
|---|---|
| Warrior | `Fbx_exports/Dirk Pekkanen6_sword.fbx` |
| Wizard | `Fbx_exports/molly_the_mage_staff.fbx` (Swing_01/02/03 for attack) |
| slime / crystal slime | `Fbx_exports/slime.fbx` |
| metal slime | `Fbx_exports/metal slime.fbx` |
| mushroom scout | `Fbx_exports/mushroom_creature1.fbx` |
| mage shroom | `Fbx_exports/Mage_Shroom.fbx` |
| flying snake / hammer | `Fbx_exports/flying snake.fbx` (Attack/Hit/Death; no Idle/Walk yet) |
| dryad crawler | `Fbx_exports/dryad_crawler.fbx` |
| static wolf2 | `Fbx_exports/static wolf2.fbx` |

**Rules**

1. Nest the export FBX as the visual on the prefab (Generic + Avatar from that model).
2. Put the Animator on that model with the Avatar set — never a null-avatar root Animator.
3. Point Idle / Walk / Attack / Hit / Death motions at the FBX sub-asset clips.
4. Idle / Walk / Run / Hop must have **Loop Time** enabled on the FBX importer.
5. Nested export visuals use **scale (1,1,1)** and **identity rotation**. Do not copy Blender’s
   classic scale-100 / -90° X compensations from older prefabs onto `Fbx_exports` models — that
   is what produced giant sideways mushrooms and doubled player meshes.
6. After dropping a new export into `Fbx_exports`, re-run **Tools → Animations → Wire Real FBX Clips**.

Generic rigs cannot retarget across different FBXs. Swapping the nested model to the FBX that
contains the bakes is required; pasting export clips onto an older mesh-only asset will not
deform bones. The wire tool unpacks old `models/Enemys` FBX instance roots so only one export
mesh remains.

Legacy bob `.anim` files may still exist on disk for history; they must not be assigned on
playable controllers. Flying snake Idle/Walk stay empty until art adds those takes to the FBX.

### 3.3 Regenerating / wiring tools

Safe to re-run:

- **Tools → Animations → Wire Real FBX Clips** — configures export importers (loop Idle/Walk),
  swaps player/enemy prefabs onto `Fbx_exports` models, and points controllers at baked takes.
- **Tools → Animations → Validate FBX Wiring** — asserts Avatar set, Idle from `Fbx_exports`,
  and Loop Time on locomotion clips.
- **Tools → Enemies → Wire Remaining NPCs (FSM + Placeholders)** — installs EnemyBase /
  StatsManager / NavMeshAgent / detection spheres. Do **not** rely on its placeholder anim
  pass for shipping art; run Wire Real FBX Clips after.
- **Tools → Enemies → Validate NPC Wiring** — smoke-checks components, triggers, Avatar, and
  that Idle is not still a Generated bob clip.
- **Tools → Player → Run Player Setup** — adds contract parameters / death screen /
  `PlayerStateMachine`. Skips placeholder locomotion if Idle already uses `Fbx_exports`.

**Do not** run **Generate Placeholder Animations** on characters that are already FBX-wired —
it overwrites controller motions with bob clips.

---

## 4. ScriptableObject reference

All created via **Assets → Create → …**

| Menu path | Type |
|---|---|
| `Game/Stats/Unit Stats` | `Stats` |
| `Game/Stats/Stats Modifier (Roguelite)` | `StatsModifierSO` |
| `Game/Combat/Combo` | `ComboSO` |
| `Game/Abilities/Dash` | `DashAbilitySO` |
| `Game/Abilities/Dryad Delights` | `DryadDelightsAbilitySO` |
| `Game/Abilities/Roasted Whole Slime` | `RoastedSlimeAbilitySO` |
| `Game/Abilities/Snaghetti` | `SnaghettiAbilitySO` |
| `Game/Passives/Passive Effect` | `PassiveEffectSO` |
| `Game/Food/Food Passive (On Hit Buff)` | `OnHitPassiveSO` |
| `Game/Food/Food Passive (On Kill)` | `KillPassiveSO` |
| `Game/Food/Food Passive (Debuff On Hit)` | `DebuffOnHitPassiveSO` |
| `Game/Food/Food Passive (Ult Ability)` | `UltFoodSO` |
| `Game/Food/Food Stat Passive` | `FoodStatPassiveSO` |
| `Game/Loot/Loot Table` | `LootTableSO` |
| `Game/Crafting/Recipe (Primary+Secondary)` | `CraftRecipeSO` |
| `Game/Crafting/Rarity Recipe` | `RarityRecipeSO` |
| `Game/Currency/Currency Type` | `CurrencySO` |
| `Audio/Sound` | `SoundSO` |

---

## 5. Common recipes

**A new wave encounter.** Put an `EnemyManager` on a trigger volume covering the room. Add
spawn point children. Fill in the wave list with enemy prefabs and counts. Bake a NavMesh.
Hook `OnAllWavesCleared` to your door/reward logic.

**A new combo.** Create a `Game/Combat/Combo` asset. Each hit has a damage multiplier, a
chain window, and an `animatorTrigger`. **The trigger name must exist as a Trigger parameter
on that character's Animator Controller** — `Player.controller` uses `Attack`,
`Player_Wizard.controller` uses `WizardAttack`. Assign the asset to `ComboRunner.combo`.

**A new food passive.** Create the matching `Game/Food/…` asset, then add a link entry in the
`PlayerConsume` list that corresponds to its type.

**Restyling the death screen.** Edit `Assets/UI/DeathScreen.prefab`. Keep the three button
references assigned on `DeathScreenController`; layout and labels are free. It spawns itself
on death and wires its own buttons, so **a new scene needs no death-screen setup at all**.

---

## 6. Gotchas

**`Is Trigger` on enemy detection spheres.** Covered above, but it's the number one cause of
"my enemy does nothing".

**`Stopping Distance` vs strike radius.** If stopping distance ≥ the strike collider radius,
the enemy parks just outside its own attack range and never swings.

**There is no dedicated Player layer.** The player sits on **Default** (0) and enemies on
**Enemy** (3). `EnemyHitbox` therefore filters on Default *and* requires a `StatsManager` on
whatever it touches — the player is currently the only Default-layer object with one, so it's
safe today. Adding a real `Player` layer would make it safer, and would be a good small
cleanup task.

**Ledge falling (player only).** `PlayerMove` raycasts ahead for drops deeper than
`minFallHeight`, disables the agent, and lets gravity take over until landing resamples back
onto the NavMesh. Two things to know:
- Set `isGround` to **every** walkable layer. An incomplete mask makes ramps misread as
  ledges and the player "falls" down slopes.
- Keep `agent.speed`/`acceleration` in sync with `acceleration`/`haltSpeed`. Sluggish
  response usually means acceleration is too low; ice-skating usually means `haltSpeed` is
  too low.
- Enemies deliberately have **no** ledge-fall behaviour. The NavMesh edge already stops them,
  which is what you want for NPCs.

**Dash rules live in one place.** `PlayerStateMachine.CanEnterDash()`. It currently forbids
dashing out of an attack or while eating — flip those two lines to allow dash-cancelling.
Don't add dash conditions elsewhere.

**Enemy death has exactly one owner.** `EnemyDeath` disables the agent and colliders, plays
the death clip, and destroys the object. `EnemyManager` only *untracks*. If you add loot,
score or VFX on death, hook `StatsManager.OnDied` — don't add another `Destroy` call.

**`hubSceneName` is `"Hub"` but the scene file is `hub.unity`.** Unity matches scene names
case-insensitively so this works today, but if "Return to Hub" ever silently fails, check
this first.

**Building a player rewrites some settings files.** A standalone build dirties
`ProjectSettings/ProjectSettings.asset`, `UnityConnectSettings.asset`, `GraphicsSettings.asset`
and two URP assets. That churn is not a real change — revert those files before committing
unless you actually meant to change a project setting.

---

## 7. Where things live

```
Assets/
  Script_folder/          all runtime C#
    EnemyBase.cs          NPC brain — start here for enemy work
    EnemyIdle/Move/Attack/Death.cs   the four NPC states
    EnemyAnimatorParams.cs, EnemyTuning.cs, EnemyHitbox.cs
    PlayerStateMachine.cs  player coordinator — start here for player work
    PlayerAnimatorParams.cs, PlayerMove.cs, Comborunner.cs
    PlayerDeathHandler.cs, DeathScreenController.cs
    StatsManager.cs        health/damage for BOTH sides
  editor/                 editor-only tools (not shipped in builds)
    PlaceholderEnemyAnimationGenerator.cs
    PlayerSetupGenerator.cs
  animations/Generated/   enemy Animator Controllers (motions = Fbx_exports clips)
  UI/DeathScreen.prefab
Docs/DESIGNER_GUIDE.md    this file
```

Nothing in `Assets/editor` ships in a build. Keep it that way — a single `using UnityEditor;`
in a runtime script breaks the player build while still compiling fine in the editor, which
is precisely how this project ended up unable to build for an extended period without anyone
noticing.
