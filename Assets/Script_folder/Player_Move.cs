using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;

public class Player_Move : MonoBehaviour
{
    [Header("Speed Stuff")]
    public float acceleration;
    public float maxSpeed;
    public float haltSpeed;

    [Header("Stats")]
    public StatManager stats; 

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

    void Start()
    {
        // Auto-grab StatManager if you forget to assign it
        if (stats == null)
            stats = GetComponent<StatManager>();
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHight * 0.5f + 0.2f, isGround);

        // handle drag
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    void FixedUpdate()
    {
        // Get movespeed from stats (fallback to 1 if missing)
        float moveSpeedMultiplier = (stats != null) ? stats.MoveSpeed : 1f;

        float finalAcceleration = acceleration * moveSpeedMultiplier;
        float finalMaxSpeed = maxSpeed * moveSpeedMultiplier;

        // get move direction and add force
        moveDir = transform.forward * movevalue.y + transform.right * movevalue.x;
        rb.AddForce(moveDir * finalAcceleration);

        // rotate the player body
        if (movevalue.magnitude > 0)
        {
            playerModel.transform.rotation = Quaternion.Slerp(
                playerModel.transform.rotation,
                Quaternion.LookRotation(moveDir),
                modelRotateSpeed * Time.deltaTime
            );
            Debug.Log("Change rotation");
        }

        // if we are moving or if we are not moving
        isMoving = movevalue.magnitude > 0.25f;

        animator.SetBool("isMoving", isMoving);

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

