using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration;
    public float haltSpeed;

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

    private bool isMoving;
    private Vector2 moveInput;
    private Vector3 moveDir;
    private Vector3 currentVelocity;
    private bool warnedNotOnNavMesh;
    private bool warnedEmptyGroundMask;
    private float fallStartY;
    private float fallStartTime;

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

        // Kept kinematic while agent-driven so existing trigger/collision callbacks
        // (pickups, CraftPot zone, etc.) still fire — physics only takes over during falls.
        if (rb != null)
            rb.isKinematic = true;
    }

    private void Update()
    {
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

        // Rotate model to face movement direction (both grounded and airborne).
        // modelRotateSpeed is per-second; scale by deltaTime to stay framerate-independent.
        if (moveInput.magnitude > .1f)
        {
            transform.forward = Vector3.MoveTowards(transform.forward, desiredDir, modelRotateSpeed * Time.deltaTime);
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

        // Exponential-decay damping, same curve the old AddForce(-velocity * haltSpeed) gave.
        if (!isMoving)
            currentVelocity *= Mathf.Exp(-haltSpeed * Time.deltaTime);

        // The agent clamps at the NavMesh edge, so it can never walk off a drop by itself.
        // When the player is actively pushing toward a ledge, hand control to physics.
        if (isMoving && rb != null && LedgeAhead(desiredDir))
        {
            StartFalling();
            return;
        }

        if (agent.isOnNavMesh)
        {
            agent.Move(currentVelocity * Time.deltaTime);
        }
        else if (!warnedNotOnNavMesh)
        {
            warnedNotOnNavMesh = true;
            Debug.LogWarning($"PlayerMove on '{name}': NavMeshAgent isn't on a baked NavMesh — bake one under the spawn point (Window > AI > Navigation, or a NavMeshSurface). Movement is disabled until it is.");
        }
    }

    /// <summary>
    /// True when the input direction leads over a drop deeper than minFallHeight.
    /// A solid hit at knee height ahead means a wall is clamping the agent, not a ledge.
    /// </summary>
    private bool LedgeAhead(Vector3 dir)
    {
        dir = dir.normalized;
        Vector3 kneeOrigin = transform.position + Vector3.up * 0.3f;

        if (Physics.Raycast(kneeOrigin, dir, ledgeProbeDistance, GroundMask(), QueryTriggerInteraction.Ignore))
            return false;

        Vector3 probeOrigin = kneeOrigin + dir * ledgeProbeDistance;
        return !Physics.Raycast(probeOrigin, Vector3.down, 0.3f + minFallHeight, GroundMask(), QueryTriggerInteraction.Ignore);
    }

    // ------------------------------------------------------------------ Falling (physics-driven)

    private void StartFalling()
    {
        IsFalling = true;
        fallStartY = transform.position.y;
        fallStartTime = Time.time;

        // A merely-stopped agent still snaps to the mesh; it has to be fully disabled.
        agent.enabled = false;

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

        TryLand();
    }

    private void TryLand()
    {
        // Give the fall a moment to actually leave the ledge before checking for ground.
        if (Time.time - fallStartTime < 0.1f) return;
        if (rb.linearVelocity.y > 0.05f) return;

        if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down,
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

        agent.enabled = true;
        agent.Warp(navPosition);
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
