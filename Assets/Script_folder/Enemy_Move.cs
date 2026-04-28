using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;
//using System.Diagnostics;
//using System.Numerics;

public class Enemy_Move : Enemy_State
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
    public float groundDrag;

    Transform playerTransform;
    Vector2 movevalue;
    Vector3 moveDir;


    public Enemy_Move(Enemy_Base enemy, Enemy_State_Machine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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
            enemy.stateMachine.ChangeState(enemy.attackState);
        }
    }

    public override void PhysicsUpdate()
    {
        // Get movespeed from stats (fallback to 1 if missing)
        float moveSpeedMultiplier = (stats != null) ? stats.MoveSpeed : 1f;

        float finalAcceleration = acceleration * moveSpeedMultiplier;
        float finalMaxSpeed = maxSpeed * moveSpeedMultiplier;

        moveDir = (playerTransform.position - enemy.transform.position).normalized;
        rb.AddForce(moveDir * finalAcceleration);

        // rotate the player body
        if (movevalue.magnitude > 0)
        {
            enemyModel.transform.rotation = Quaternion.Slerp(
                enemyModel.transform.rotation,
                Quaternion.LookRotation(moveDir),
                modelRotateSpeed * Time.deltaTime
            );
            Debug.Log("Change rotation");
        }

        // if we are moving or if we are not moving
        isMoving = movevalue.magnitude > 0.25f;

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
        if (!isMoving)
        {
            rb.AddForce(-rb.linearVelocity * haltSpeed);
        }

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


}

