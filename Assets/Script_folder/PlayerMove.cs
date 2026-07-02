using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration;
    public float haltSpeed;

    [Header("Stats")]
    public StatsManager stats;

    [Header("Model")]
    public float modelRotateSpeed;
    public Transform playerModel;
    public NavMeshAgent agent;
    public Rigidbody rb;
    public Animator animator;

    [Header("Camera")]
    public GameObject Cam;

    private bool isMoving;
    private Vector2 moveInput;
    private Vector3 moveDir;
    private Vector3 currentVelocity;
    private bool warnedNotOnNavMesh;

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

        // Kept only so existing trigger/collision callbacks (pickups, CraftPot zone, etc.)
        // still fire � the agent owns actual movement now, not physics.
        if (rb != null)
            rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        // MoveSpeed from the stat system IS the max speed (e.g. base 5 units/s).
        // Modifiers add to or multiply it directly � no extra multiplier gymnastics needed.
        float maxSpeed = (stats != null) ? stats.MoveSpeed : 5f;

        moveDir = -moveInput.x * Cam.transform.right + -moveInput.y * Cam.transform.forward;
        moveDir = new Vector3(moveDir.x, 0, moveDir.z);

        // Accelerate unconditionally (matches the old AddForce, which was naturally zero
        // when there was no input) — magnitude scales with analog stick deflection.
        Vector3 desiredDir = moveDir * -1;
        currentVelocity += desiredDir * acceleration * Time.deltaTime;

        isMoving = moveInput.magnitude > 0.25f;

        if (currentVelocity.magnitude > maxSpeed)
            currentVelocity = currentVelocity.normalized * maxSpeed;

        // Matches the old AddForce(-velocity * haltSpeed) exponential-decay damping,
        // now that there's no Rigidbody integrating forces for us.
        if (!isMoving)
            currentVelocity *= Mathf.Exp(-haltSpeed * Time.deltaTime);

        if (agent.isOnNavMesh)
        {
            agent.Move(currentVelocity * Time.deltaTime);
        }
        else if (!warnedNotOnNavMesh)
        {
            warnedNotOnNavMesh = true;
            Debug.LogWarning($"PlayerMove on '{name}': NavMeshAgent isn't on a baked NavMesh — bake one under the spawn point (Window > AI > Navigation, or a NavMeshSurface). Movement is disabled until it is.");
        }

        // Rotate model to face movement direction.
        // Runs in Update now instead of FixedUpdate, so modelRotateSpeed must be scaled
        // by deltaTime to stay framerate-independent — the old Inspector value (tuned
        // for a per-fixed-tick rate) will need to be scaled up to match the old feel.
        if (moveInput.magnitude > .1f)
        {
            transform.forward = Vector3.MoveTowards(transform.forward, desiredDir, modelRotateSpeed * Time.deltaTime);
        }

    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}

