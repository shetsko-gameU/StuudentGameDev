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

### 3.2 Placeholder clips — read this before replacing them

Every enemy currently uses **generated placeholder animation**. The clips are crude on
purpose. They exist so combat is playable and the wiring is proven end-to-end before anyone
spends time animating.

Why they're crude: the enemy models are imported as **Generic** rigs with no skeleton
entries. Generic rigs cannot retarget, so Mixamo and Asset Store humanoid clips **cannot** be
dropped onto a slime. What *does* work on an unrigged mesh is transform-level animation —
moving, rotating and scaling the model object. That's what the generator produces, following
the one pre-existing enemy clip in the project (`Assets/animations/Flying Snake Hover.anim`,
which animates nothing but `m_LocalPosition` on a child called `Model`).

Clips are always bound to the **model child, never the enemy root**, because the
`NavMeshAgent` owns the root's position. Animating the root fights the agent.

**Wired enemies (all nine):** slime, crystal slime, metal slime, mushroom scout, mage shroom,
flying snake, flying hammer snake, dryad crawler1, static wolf2. Re-run
**Tools → Enemies → Wire Remaining NPCs (FSM + Placeholders)** after adding a new creature
prefab under `Assets/models/Enemys/Prefabs` (add its path to both editor generators first).

**To replace them with real animation:** open the generated controller in
`Assets/animations/Generated/<enemy>.controller` and swap the Motion on each state. Change
nothing else — the parameters and transitions stay valid. Don't delete the states.

**If you want to retarget humanoid animation instead**, you must first change the model's
import settings from Generic to Humanoid and configure an avatar. That's a modelling task,
not a scripting one.

### 3.3 Regenerating

Two editor tools, both safe to re-run:

- **Tools → Enemies → Wire Remaining NPCs (FSM + Placeholders)** — installs EnemyBase /
  StatsManager / NavMeshAgent / detection spheres on visual-only creature prefabs, then runs
  the placeholder animation pass. Safe to re-run; repairs incomplete prefabs.
- **Tools → Enemies → Validate NPC Wiring** — smoke-checks all nine spawnable enemies for the
  required components and trigger colliders.
- **Tools → Enemies → Generate Placeholder Animations** — rebuilds clips and controllers only.
  *This overwrites the generated controllers*, so don't put hand-authored work in them.
- **Tools → Player → Run Player Setup** — adds the contract parameters to the player
  controllers, builds the death screen prefab, and adds `PlayerStateMachine` to the player
  prefabs. Only *adds* — it never touches existing states, transitions or clips.

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
  animations/Generated/   generated placeholder clips + enemy controllers
  UI/DeathScreen.prefab
Docs/DESIGNER_GUIDE.md    this file
```

Nothing in `Assets/editor` ships in a build. Keep it that way — a single `using UnityEditor;`
in a runtime script breaks the player build while still compiling fine in the editor, which
is precisely how this project ended up unable to build for an extended period without anyone
noticing.
