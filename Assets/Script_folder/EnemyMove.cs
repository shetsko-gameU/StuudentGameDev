using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;
//using System.Diagnostics;
//using System.Numerics;
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMove : EnemyState
{
    [Header("Speed Stuff")]
    public float acceleration;
    public float maxSpeed;
    public float haltSpeed;
    public float airControl = 0.3f;

    [Header("Stats")]
    public StatsManager stats;

    [Header("Falling")]
    [Tooltip("Layers used for the ledge and landing raycasts. Should include the ground (and ideally walls). " +
             "If left empty, everything except the player's own layer is used.")]

    [Header("Model Stuff")]
    public float modelRotateSpeed;
    public Transform enemyModel;
    float ledgeProbeDistance = 0.45f;
    float minFallHeight = 0.5f;

    private Vector3 currentVelocity;
      private bool warnedNotOnNavMesh;
    private bool warnedEmptyGroundMask;
    private float fallStartY;
    private float fallStartTime;

    // The player's solid body collider. Probes measure foot level from its bounds because
    // the transform root is NOT at the feet on this player (capsule center 0 / base offset 1).
    bool isMoving;
    public Rigidbody rb;

    [Header("Ground Check")]
    public float playerHight;
    public LayerMask isGround;
    bool grounded;
    bool IsFalling;
    bool canSeeTarget;
    public float groundDrag;
    Vector2 movevalue;
    Vector3 moveDir;
    public float obstacleDetectionRange = 3f;
    public float avoidanceForce = 4f;
    private Collider bodyCollider;

    public EnemyMove(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        rb = enemy.GetComponent<Rigidbody>();
        canSeeTarget = true;
    }

    public override void EnterState()
    {
        // Auto-grab StatManager if not assigned on the enemy
        if (stats == null && enemy != null)
            stats = enemy.GetComponent<StatsManager>();
    }
    private float FootY()
    {
        return bodyCollider != null ? bodyCollider.bounds.min.y : enemy.transform.position.y;
    }

    public override void FrameUpdate()
    {
        // ground check and non-physics per-frame logic
        grounded = Physics.Raycast(enemy.transform.position, Vector3.down, playerHight * 0.5f + 0.2f, isGround);
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }

        if (enemy.isWithinRange)
        {
            enemy.navMeshAgent.isStopped = true;
            enemy.stateMachine.ChangeState(enemy.attackState);
        }
        else
        {
            enemy.navMeshAgent.isStopped = false;
            enemy.navMeshAgent.destination = enemy.currentTarget.transform.position;
        }
        
        if (!canSeeTarget)
        {
            enemy.currentTarget = null;
            enemy.stateMachine.ChangeState(enemy.idleState);
        }
        
    }

    public override void PhysicsUpdate()
    {
        // Get movespeed from stats (fallback to 1 if missing)
        float moveSpeedMultiplier = (stats != null) ? stats.MoveSpeed : 1f;

        float finalAcceleration = acceleration * moveSpeedMultiplier;
        float finalMaxSpeed = maxSpeed * moveSpeedMultiplier;
        moveDir = (enemy.currentTarget.transform.position - enemy.transform.position).normalized;
        rb.AddForce(moveDir * finalAcceleration);
        Vector3 desiredDir = moveDir * -1;
        if (IsFalling)
            FallingUpdate(desiredDir, maxSpeed);
        else
            GroundedUpdate(desiredDir, maxSpeed);


        // rotate the player body
       /* if (movevalue.magnitude > 0)
        {
            enemyModel.transform.rotation = Quaternion.Slerp(
                enemyModel.transform.rotation,
                Quaternion.LookRotation(moveDir),
                modelRotateSpeed * Time.deltaTime
            );
            Debug.Log("Change rotation");
        }*/

        // if we are moving or if we are not moving
        //isMoving = movevalue.magnitude > 0.25f;

        if (enemy.animator != null)
            enemy.animator.SetBool("isMoving", isMoving);

        Vector3 speedCheck = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // will stop your speed if you exceed your max speed
        if (speedCheck.magnitude > finalMaxSpeed)
        {
            Vector3 newSpeed = speedCheck.normalized * finalMaxSpeed;
            rb.linearVelocity = new Vector3(newSpeed.x, rb.linearVelocity.y, newSpeed.z);
        }

        // halt your speed if not moving
        /*if (!isMoving)
        {
            rb.AddForce(-rb.linearVelocity * haltSpeed);
        }*/

    }

    public void StopMovement()
    {
        //to do
    }

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

        if (enemy.navMeshAgent.isOnNavMesh)
        {
            enemy.navMeshAgent.Move(currentVelocity * Time.deltaTime);
        }
        else if (!warnedNotOnNavMesh)
        {
            warnedNotOnNavMesh = true;
            //Debug.LogWarning($"PlayerMove on '{name}': NavMeshAgent isn't on a baked NavMesh — bake one under the spawn point (Window > AI > Navigation, or a NavMeshSurface). Movement is disabled until it is.");
        }
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

        float probeDistance = Mathf.Max(ledgeProbeDistance, enemy.navMeshAgent.radius + 0.2f);
        Vector3 kneeOrigin = new Vector3(enemy.transform.position.x, FootY() + 0.3f, enemy.transform.position.z);

        if (Physics.Raycast(kneeOrigin, dir, probeDistance, GroundMask(), QueryTriggerInteraction.Ignore))
            return false;

        Vector3 probeOrigin = kneeOrigin + dir * probeDistance;
        return !Physics.Raycast(probeOrigin, Vector3.down, 0.3f + minFallHeight, GroundMask(), QueryTriggerInteraction.Ignore);
    }

    // ------------------------------------------------------------------ Falling (physics-driven)

    private void StartFalling()
    {
        IsFalling = true;
        fallStartY = enemy.transform.position.y;
        fallStartTime = Time.time;

        // A merely-stopped agent still snaps to the mesh; it has to be fully disabled.
        enemy.navMeshAgent.enabled = false;

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

        Vector3 landOrigin = new Vector3(enemy.transform.position.x, FootY() + 0.3f, enemy.transform.position.z);
        if (!Physics.Raycast(landOrigin, Vector3.down,
                out RaycastHit hit, 0.55f, GroundMask(), QueryTriggerInteraction.Ignore))
            return;

        // Right after stepping off, the player often slides across the physical lip that
        // sticks out past the NavMesh edge — don't count that as a landing. The stuck
        // check recovers the rare case of stopping dead on the lip without ever dropping.
        bool droppedEnough = fallStartY - enemy.transform.position.y > minFallHeight * 0.5f;
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

        enemy.navMeshAgent.enabled = true;
        enemy.navMeshAgent.Warp(navPosition);
    }

    // ------------------------------------------------------------------ Helpers

    private int GroundMask()
    {
        if (isGround.value != 0)
            return isGround.value;

        if (!warnedEmptyGroundMask)
        {
            warnedEmptyGroundMask = true;
            /*//Debug.LogWarning($"PlayerMove on '{name}': isGround mask is empty — falling back to " +
                             "'everything except the player layer' for ledge/landing raycasts. " +
                             "Set it to your ground (and wall) layers in the Inspector.");*/
        }

        return ~(1 << enemy.layer);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {
        }
    }
   /* void DetectAndAvoidObstacles()
    {
         // 1. Calculate direction to the target on the flat ground plane
        Vector3 targetDirection = enemy.currentTarget.transform.position - enemy.transform.position;
        if(!enemy.canFly)
            targetDirection.y = 0; // Lock vertical movement
        targetDirection.Normalize();

        Vector3 finalDirection = targetDirection;

        // 2. Cast a ray forward to check for obstacles
        RaycastHit hit;
        // LayerMask can be added here to only detect specific obstacle layers
        if (Physics.Raycast(enemy.transform.position, enemy.transform.forward, out hit, obstacleDetectionRange))
        {
            if (hit.transform != enemy.currentTarget)
            {
                // Calculate a direction away from the obstacle's surface
                Vector3 avoidanceDirection = Vector3.Reflect(enemy.transform.forward, hit.normal);
                if(!enemy.canFly)
                    avoidanceDirection.y = 0; // Keep the avoidance strictly horizontal
                
                // Blend the path: prioritize avoiding the wall while pulling toward target
                finalDirection = (avoidanceDirection * avoidanceForce + targetDirection).normalized;
            }
        }

        // 3. Move and look toward the final calculated direction
        enemy.transform.position += finalDirection * enemy.randomMovementSpeed * Time.deltaTime;
        
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime);
        }
    }*/


}

