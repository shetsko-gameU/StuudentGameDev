using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// NavMeshAgent-driven movement: drives agent.Move() every frame (not SetDestination/
/// pathing) with agent.updateRotation off, since this script rotates the model manually
/// toward movement direction instead. The Rigidbody is forced kinematic in code — kept
/// only so existing trigger/collision callbacks (pickups, the CraftPot zone) still fire.
///
/// Also owns the ledge-fall handoff: raycasts ahead for a drop deeper than minFallHeight,
/// disables the agent, and lets gravity take over (IsFalling) until landing resamples back
/// onto the NavMesh. See Docs/DESIGNER_GUIDE.md's "Ledge falling" section for the full
/// mechanism and the several movement-feel gotchas (agent.speed/acceleration sync, isGround
/// mask coverage, haltSpeed tuning) that are easy to reintroduce if this script is touched
/// carelessly.
///
/// This script does NOT write animator parameters. PlayerStateMachine owns Speed, Grounded
/// and Dead - see PlayerAnimatorParams.
///
/// Setup:
///   1. Requires a NavMeshAgent (auto-required) and a baked NavMesh under the player.
///   2. Assign stats/playerModel/agent/rb/animator in the Inspector (or leave stats/agent/
///      rb empty — they auto-find on Start).
///   3. Set isGround to every walkable layer (not just flat floor) — an incomplete mask
///      makes ramps misread as ledges.
///   4. Tune acceleration/haltSpeed/airControl/minFallHeight to taste; see
///      Docs/DESIGNER_GUIDE.md for what "feels sluggish" or "ice-skating" symptoms map
///      back to.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration;
    public float haltSpeed;

    [Header("Ground follow")]
    [Tooltip("How fast the player settles onto the raycast ground height (1/s). The baked NavMesh is a " +
             "voxel approximation of the terrain and is lumpy by ~10cm, so riding the height agent.Move() " +
             "snaps to reads as a bob; this follows the real collider surface instead.")]
    public float groundFollowSharpness = 40f;

    [Tooltip("How far below the feet the ground raycast looks before giving up and leaving the agent's own height alone.")]
    public float maxGroundSnapDistance = 1.5f;

    [Header("Falling")]
    [Tooltip("Layers used for the ledge and landing raycasts. Should include the ground (and ideally walls). " +
             "If left empty, everything except the player's own layer is used.")]
    public LayerMask isGround;

    [Tooltip("How far ahead of the player (in the input direction) to look for a ledge.")]
    public float ledgeProbeDistance = 0.45f;

    [Tooltip("A drop deeper than this counts as a ledge you can fall off; anything shallower is a step/slope and stays agent-driven.")]
    public float minFallHeight = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Fraction of normal acceleration available for steering while airborne.")]
    public float airControl = 0.3f;

    [Header("Stats")]
    public StatsManager stats;

    [Header("Model")]
    [Tooltip("Turn rate multiplier. Effective yaw ≈ modelRotateSpeed * 90 deg/s (Inspector 5 ≈ 450°/s).")]
    public float modelRotateSpeed;
    public Transform playerModel;
    public NavMeshAgent agent;
    public Rigidbody rb;
    public Animator animator;
    public Animator objectAnimator;

    [Header("Camera")]
    public GameObject Cam;

    /// <summary>True while gravity/physics owns the player instead of the NavMeshAgent.</summary>
    public bool IsFalling { get; private set; }

    /// <summary>
    /// Horizontal gameplay speed from the internal velocity used by agent.Move — not
    /// NavMeshAgent.velocity, which often reads ~0 with manual Move() and broke Idle↔Walk.
    /// </summary>
    public float HorizontalSpeed
    {
        get
        {
            Vector3 v = currentVelocity;
            v.y = 0f;
            float speed = v.magnitude;
            return float.IsFinite(speed) ? speed : 0f;
        }
    }

    /// <summary>True when stick/keyboard move input is above the grounded movement threshold.</summary>
    public bool HasMoveInput => isMoving;

    private bool isMoving;
    private Vector2 moveInput;
    private Vector3 moveDir;
    private Vector3 currentVelocity;
    private bool warnedNotOnNavMesh;
    private bool warnedEmptyGroundMask;
    private float fallStartY;
    private float fallStartTime;
    private float groundHeight;
    private bool hasGroundHeight;

    // The player's solid body collider. Probes measure foot level from its bounds because
    // the transform root is NOT at the feet on this player (capsule center 0 / base offset 1).
    private Collider bodyCollider;

    private void Start()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError($"PlayerMove on '{name}': No NavMeshAgent found. Add one in the Inspector.");
            enabled = false;
            return;
        }

        // We drive the agent by hand via Move() every frame instead of SetDestination,
        // so it must not also try to auto-rotate towards its steering target.
        agent.updateRotation = false;

        // This script owns the transform; the agent only simulates where it *would* be
        // (agent.nextPosition), which we read back below. Left on, the agent writes its own
        // position to the transform in its internal post-script phase — after Update — which
        // stomped FollowGroundHeight()'s correction every frame and put the player back on the
        // lumpy NavMesh height. Horizontal movement and NavMesh-edge clamping are unaffected:
        // both happen inside Move() on the internal position, which we still follow.
        agent.updatePosition = false;

        // Obstacle avoidance is RVO steering for autopilot navigation around other agents —
        // irrelevant here since the player never uses SetDestination(). Left on (the Inspector
        // had it at High Quality), it still runs its own internal velocity smoothing on top of
        // Move() calls, based on the agent's own Speed/Acceleration (3.5 / 8) — much lower than
        // what GroundedUpdate() computes itself (acceleration = 100) — so the two fight each
        // other and movement feels sluggish/"heavy". Static geometry blocking (walls, ledges)
        // is unaffected — that comes from the baked NavMesh itself via Move()'s own clamping.
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // Kept kinematic while agent-driven so existing trigger/collision callbacks
        // (pickups, CraftPot zone, etc.) still fire — physics only takes over during falls.
        if (rb != null)
        {
            rb.isKinematic = true;

            // The agent's baseOffset leaves only a few cm of clearance between the capsule's
            // feet and the ground while grounded. Unity's default depenetration speed is
            // unlimited, so if the capsule is even slightly overlapping the ground the instant
            // StartFalling() flips isKinematic to false, the physics solver can shove it out
            // in a single step — looking exactly like being launched into the air. Clamping
            // this forces any overlap to resolve gently over a few frames instead.
            rb.maxDepenetrationVelocity = 2f;
        }
        

        foreach (Collider c in GetComponents<Collider>())
        {
            if (!c.isTrigger)
            {
                bodyCollider = c;
                break;
            }
        }

        if (bodyCollider == null)
            Debug.LogWarning($"PlayerMove on '{name}': No solid Collider found — ledge/landing probes will assume the transform root is at foot level.");
    }

    /// <summary>
    /// World-space Y of the ground the player is standing on, i.e. foot level.
    /// Taken from the agent's baseOffset, which is *defined* as how far the root floats above
    /// the surface. Do NOT take it from the body collider: that capsule is authored around the
    /// torso, and when its bottom sat 1.1m above the feet, every probe on flat ground reported
    /// a ledge — so simply walking kicked the player into a fall/land loop that read as bobbing
    /// and sank them through the floor on the way down.
    /// </summary>
    private float FootY()
    {
        if (agent != null)
            return transform.position.y - agent.baseOffset;

        return bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;
    }

    private bool pendingDrop;

    /// <summary>
    /// Called by StartPortal.DropPlayer() (an Animation Event) to make the player physically
    /// fall from wherever the portal spawned them, using the same fall/land state machine as
    /// walking off a ledge. Deferred to a flag checked at the top of Update() rather than
    /// calling StartFalling() directly, since the player GameObject starts deactivated and
    /// Start() (which sets up rb/agent/bodyCollider) may not have run yet at the exact moment
    /// the animation event fires — Update() is guaranteed to run only after Start() has.
    /// </summary>
    public void DropFromPortal()
    {
        pendingDrop = true;
    }

    private void Update()
    {
        if (pendingDrop)
        {
            pendingDrop = false;
            StartFalling();
        }

        // MoveSpeed from the stat system IS the max speed (e.g. base 5 units/s).
        // Modifiers add to or multiply it directly — no extra multiplier gymnastics needed.
        float maxSpeed = (stats != null) ? stats.MoveSpeed : 5f;

        moveDir = -moveInput.x * Cam.transform.right + -moveInput.y * Cam.transform.forward;
        moveDir = new Vector3(moveDir.x, 0, moveDir.z);

        Vector3 desiredDir = moveDir * -1;
        isMoving = moveInput.magnitude > 0.25f;

        if (IsFalling)
            FallingUpdate(desiredDir, maxSpeed);
        else
            GroundedUpdate(desiredDir, maxSpeed);

        // Rotate to face movement. Quaternion path avoids NaN from zero-length forward.
        // modelRotateSpeed is a multiplier: *90 ≈ deg/s (Inspector 5 → ~450°/s).
        if (moveInput.magnitude > .1f && desiredDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                modelRotateSpeed * 90f * Time.deltaTime);
        }
    }

    // ------------------------------------------------------------------ Grounded (agent-driven)

    private void GroundedUpdate(Vector3 desiredDir, float maxSpeed)
    {
        // Accelerate unconditionally (matches the old AddForce, which was naturally zero
        // when there was no input) — magnitude scales with analog stick deflection.
        currentVelocity += desiredDir * acceleration * Time.deltaTime;

        if (currentVelocity.magnitude > maxSpeed)
            currentVelocity = currentVelocity.normalized * maxSpeed;

        // Steer velocity toward the facing/input direction so turns do not strafe-slide
        // while the root rotates separately.
        if (isMoving && desiredDir.sqrMagnitude > 0.0001f && currentVelocity.sqrMagnitude > 0.0001f)
        {
            float speed = currentVelocity.magnitude;
            Vector3 targetVel = desiredDir.normalized * speed;
            float maxRadiansDelta = modelRotateSpeed * 90f * Mathf.Deg2Rad * Time.deltaTime;
            currentVelocity = Vector3.RotateTowards(currentVelocity, targetVel, maxRadiansDelta, 0f);
        }

        // Exponential-decay damping, same curve the old AddForce(-velocity * haltSpeed) gave.
        if (!isMoving)
        {
            currentVelocity *= Mathf.Exp(-haltSpeed * Time.deltaTime);

            // Exponential decay only approaches zero asymptotically — snap away the last
            // sliver so the player comes to a true stop instead of an imperceptible,
            // never-ending drift.
            if (currentVelocity.magnitude < 0.05f)
                currentVelocity = Vector3.zero;
        }

        // The agent clamps at the NavMesh edge, so it can never walk off a drop by itself.
        // When the player is actively pushing toward a ledge, hand control to physics.
        if (isMoving && rb != null && LedgeAhead(desiredDir))
        {
            StartFalling();
            return;
        }

        if (agent.isOnNavMesh)
        {
            // Keep the agent's own speed/acceleration ceiling at or above what this system is
            // actually driving. Move() bypasses pathing, but the agent still uses its own
            // Speed/Acceleration internally to conform to the mesh surface (slopes, height
            // changes, crossing between polygons) — on flat single-polygon ground that's
            // negligible, but on ramps/uneven terrain it does real work every frame, and a
            // ceiling below maxSpeed throttles it (the same class of fight that made obstacle
            // avoidance feel "heavy" before it was disabled above).
            agent.speed = maxSpeed;
            agent.acceleration = acceleration;

            // Horizontal only — never feed Y into Move or the agent amplifies mesh noise.
            currentVelocity.y = 0f;

            agent.Move(currentVelocity * Time.deltaTime);
            FollowGroundHeight();
        }
        else if (!warnedNotOnNavMesh)
        {
            warnedNotOnNavMesh = true;
            Debug.LogWarning($"PlayerMove on '{name}': NavMeshAgent isn't on a baked NavMesh — bake one under the spawn point (Window > AI > Navigation, or a NavMeshSurface). Movement is disabled until it is.");
        }
    }

    /// <summary>
    /// Replaces the height agent.Move() just snapped to with the real ground surface.
    /// The baked NavMesh is a voxelised approximation of the terrain — here it sits ~12cm above
    /// it and wanders ~14cm over a few metres — so conforming to it makes the player undulate
    /// while crossing ground that looks flat. The terrain collider is smooth, so sample that.
    /// The eased value is kept in its own field rather than blended against transform.position,
    /// because the agent re-snaps Y every frame and would keep dragging the blend back toward
    /// the lumpy mesh height.
    /// </summary>
    private void FollowGroundHeight()
    {
        // Horizontal comes from the agent's own simulation (already clamped to the NavMesh
        // edge by Move); only the height is ours.
        Vector3 next = agent.nextPosition;
        float height = next.y;

        Vector3 origin = new Vector3(next.x, next.y - agent.baseOffset + 0.5f, next.z);
        float probeLength = 0.5f + Mathf.Max(0.1f, maxGroundSnapDistance);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeLength,
                GroundMask(), QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + agent.baseOffset;

            if (!hasGroundHeight)
            {
                // First frame of contact (spawn, landing, walking back onto masked ground):
                // adopt the height outright, since easing from a stale value looks like a slide.
                groundHeight = targetY;
                hasGroundHeight = true;
            }
            else
            {
                groundHeight = Mathf.Lerp(groundHeight, targetY,
                    1f - Mathf.Exp(-Mathf.Max(0.01f, groundFollowSharpness) * Time.deltaTime));
            }

            height = groundHeight;
        }
        else
        {
            // Nothing in the ground mask below — a gap, a ledge lip, or a platform on a layer
            // the mask misses. Fall back to the agent's own height rather than guessing.
            hasGroundHeight = false;
        }

        Vector3 planted = new Vector3(next.x, height, next.z);
        transform.position = planted;
        agent.nextPosition = planted;
    }

    /// <summary>
    /// True when the input direction leads over a drop deeper than minFallHeight.
    /// A solid hit at knee height ahead means a wall is clamping the agent, not a ledge.
    /// Origins sit at foot level from the collider bounds — the transform root is not at
    /// the feet — and the probe must reach past the agent's radius, since the NavMesh edge
    /// (where the agent clamps) is inset from the physical ledge by that radius.
    /// </summary>
    private bool LedgeAhead(Vector3 dir)
    {
        dir = dir.normalized;

        float probeDistance = Mathf.Max(ledgeProbeDistance, agent.radius + 0.2f);
        Vector3 kneeOrigin = new Vector3(transform.position.x, FootY() + 0.3f, transform.position.z);

        if (Physics.Raycast(kneeOrigin, dir, probeDistance, GroundMask(), QueryTriggerInteraction.Ignore))
            return false;

        Vector3 probeOrigin = kneeOrigin + dir * probeDistance;
        return !Physics.Raycast(probeOrigin, Vector3.down, 0.3f + minFallHeight, GroundMask(), QueryTriggerInteraction.Ignore);
    }

    // ------------------------------------------------------------------ Falling (physics-driven)

    private void StartFalling()
    {
        // A merely-stopped agent still snaps to the mesh; it has to be fully disabled.
        agent.enabled = false;

        // Nudge up a hair BEFORE going non-kinematic. The agent's baseOffset only leaves a
        // thin clearance above the ground mesh, so the capsule is often slightly embedded
        // in it right at a ledge edge. Kinematic bodies can be freely repositioned with zero
        // physics cost, so this clears that overlap pre-emptively — otherwise, the instant
        // isKinematic flips off, PhysX depenetrates the overlap over several physics steps,
        // imparting real upward velocity. TryLand() then refuses to land while rb.linearVelocity.y
        // is positive, so that velocity has to fully decay under gravity first — turning what
        // should be a clean drop into a visible multi-second launch/float.
        rb.position += Vector3.up * 0.1f;

        IsFalling = true;
        fallStartY = transform.position.y;
        fallStartTime = Time.time;
        hasGroundHeight = false; // gravity owns Y now; re-acquire on landing

        rb.isKinematic = false;
        rb.linearVelocity = currentVelocity; // carry momentum over the edge
    }

    private void FallingUpdate(Vector3 desiredDir, float maxSpeed)
    {
        // Air control — a fraction of normal acceleration, framerate-independent.
        rb.AddForce(desiredDir * (acceleration * airControl) * Time.deltaTime, ForceMode.VelocityChange);

        // Same horizontal speed cap as grounded movement.
        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontal.magnitude > maxSpeed)
        {
            Vector3 clamped = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }

        // There's no jump — the player should never legitimately gain upward velocity while
        // falling. Right at a ledge the capsule can still be clipping the edge/corner of the
        // ground geometry (not just sinking straight down into it), so PhysX's depenetration
        // can shove it sideways-and-up together; a purely vertical pre-emptive nudge (above)
        // can't fully prevent that. Clamping this away every frame kills the launch outright,
        // regardless of which direction the overlap resolves in, with zero effect on a real
        // fall (whose Y velocity is never positive past the very first instant anyway).
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        TryLand();
    }

    private void TryLand()
    {
        // Give the fall a moment to actually leave the ledge before checking for ground.
        if (Time.time - fallStartTime < 0.1f) return;
        if (rb.linearVelocity.y > 0.05f) return;

        Vector3 landOrigin = new Vector3(transform.position.x, FootY() + 0.3f, transform.position.z);
        if (!Physics.Raycast(landOrigin, Vector3.down,
                out RaycastHit hit, 0.55f, GroundMask(), QueryTriggerInteraction.Ignore))
            return;

        // Right after stepping off, the player often slides across the physical lip that
        // sticks out past the NavMesh edge — don't count that as a landing. The stuck
        // check recovers the rare case of stopping dead on the lip without ever dropping.
        bool droppedEnough = fallStartY - transform.position.y > minFallHeight * 0.5f;
        bool stuckOnLip = Time.time - fallStartTime > 1f && rb.linearVelocity.magnitude < 0.5f;
        if (!droppedEnough && !stuckOnLip) return;

        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
            Land(navHit.position);
        // else: landed somewhere with no NavMesh underneath — stay physics-driven with
        // air control as crude movement; TryLand retries every frame and recovers the
        // moment the player reaches mesh again. (Void/kill-plane handling is future work.)
    }

    private void Land(Vector3 navPosition)
    {
        // Keep horizontal momentum so movement flows straight through the landing.
        currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        IsFalling = false;
        hasGroundHeight = false;

        agent.enabled = true;
        agent.Warp(navPosition);

        // With updatePosition off the agent never writes the transform itself, so place it
        // explicitly instead of trusting Warp to carry the visual position across.
        transform.position = navPosition;
    }

    // ------------------------------------------------------------------ Helpers

    private int GroundMask()
    {
        if (isGround.value != 0)
            return isGround.value;

        if (!warnedEmptyGroundMask)
        {
            warnedEmptyGroundMask = true;
            Debug.LogWarning($"PlayerMove on '{name}': isGround mask is empty — falling back to " +
                             "'everything except the player layer' for ledge/landing raycasts. " +
                             "Set it to your ground (and wall) layers in the Inspector.");
        }

        return ~(1 << gameObject.layer);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
