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
    public Transform playerModel;
    bool isMoving;
    public Rigidbody rb;
    public Animator animator;
    public Animator ObjectAnimator;

    [Header("Ground Check")]
    public float playerHight;
    public LayerMask isGround;
    bool grounded;
    public float groundDrag;

    Vector2 movevalue;
    Vector3 moveDir;


    public Enemy_Move(Enemy_Base enemy, Enemy_State_Machine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
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
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    public override void PhysicsUpdate()
    {
        float moveSpeedMultiplier = (stats != null) ? stats.MoveSpeed : 1f;

        float finalAcceleration = acceleration * moveSpeedMultiplier;
        float finalMaxSpeed = maxSpeed * moveSpeedMultiplier;

        moveDir = enemy.transform.forward * movevalue.y + enemy.transform.right * movevalue.x;
        rb.AddForce(moveDir * finalAcceleration);

        if (movevalue.magnitude > 0)
        {
            playerModel.transform.rotation = Quaternion.Slerp(
                playerModel.transform.rotation,
                Quaternion.LookRotation(moveDir),
                modelRotateSpeed * Time.deltaTime
            );
        }

        isMoving = movevalue.magnitude > 0.25f;

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        Vector3 speedCheck = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (speedCheck.magnitude > finalMaxSpeed)
        {
            Vector3 newSpeed = speedCheck.normalized * finalMaxSpeed;
            rb.linearVelocity = new Vector3(newSpeed.x, rb.linearVelocity.y, newSpeed.z);
        }

        if (!isMoving)
        {
            rb.AddForce(-rb.linearVelocity * haltSpeed);
        }

        if (ObjectAnimator != null)
            ObjectAnimator.SetFloat("Face_Direction", moveDir.x);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {
        }
    }


}

