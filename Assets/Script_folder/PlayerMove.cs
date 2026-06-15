using UnityEngine;
using UnityEngine.InputSystem;

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
    public Rigidbody rb;
    public Animator animator;
    public Animator objectAnimator;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask isGround;
    public float groundDrag;

    private bool isMoving;
    private bool grounded;
    private Vector2 moveInput;
    private Vector3 moveDir;

    private void Start()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, isGround);
        rb.linearDamping = grounded ? groundDrag : 0f;
    }

    private void FixedUpdate()
    {
        // MoveSpeed from the stat system IS the max speed (e.g. base 5 units/s).
        // Modifiers add to or multiply it directly — no extra multiplier gymnastics needed.
        float maxSpeed = (stats != null) ? stats.MoveSpeed : 5f;

        moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        rb.AddForce(moveDir * acceleration);

        // Rotate model to face movement direction
        if (moveInput.magnitude > 0f && moveDir.sqrMagnitude > 0.001f)
        {
            playerModel.rotation = Quaternion.Slerp(
                playerModel.rotation,
                Quaternion.LookRotation(moveDir),
                modelRotateSpeed * Time.deltaTime);
        }

        isMoving = moveInput.magnitude > 0.25f;
       // animator.SetBool("isMoving", isMoving);

        // Clamp horizontal speed to MoveSpeed from stats
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVel.magnitude > maxSpeed)
        {
            Vector3 clamped = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }

        // Slow down when not giving input
        if (!isMoving)
            rb.AddForce(-rb.linearVelocity * haltSpeed);

       // objectAnimator.SetFloat("FaceDirection", moveDir.x);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}

