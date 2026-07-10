using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;
//using System.Diagnostics;
//using System.Numerics;

public class EnemyMove : EnemyState
{
    [Header("Speed Stuff")]
    public float acceleration;
    public float maxSpeed;
    public float haltSpeed;

    [Header("Stats")]
    public StatsManager stats;

    [Header("Model Stuff")]
    public float modelRotateSpeed;
    public Transform enemyModel;
    bool isMoving;
    public Rigidbody rb;

    [Header("Ground Check")]
    public float playerHight;
    public LayerMask isGround;
    bool grounded;
    bool canSeeTarget;
    public float groundDrag;
    Vector2 movevalue;
    Vector3 moveDir;
    public float obstacleDetectionRange = 3f;
    public float avoidanceForce = 4f;



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

