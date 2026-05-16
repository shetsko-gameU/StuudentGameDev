//using System.Numerics;
using UnityEngine;
using System.Collections.Generic;

public class Enemy_Base : MonoBehaviour, TriggerCheck
{
   [Header("Stats")]
    public StatsManager stats;
    public StatsManager Player_Stats;
    public Animator animator;
    public List<Transform> raycasts;
    Vector2 movevalue;

    // TriggerCheck implementation
    public bool isAggroed { get; set; }
    public bool isWithinRange { get; set; }


    public Enemy_State_Machine stateMachine {get; set;}
    public Enemy_Idle idleState { get; set; }
    public Enemy_Attack attackState { get; set; }
    public Enemy_Move moveState { get; set; }

    public float randomMovementRange;
    public float randomMovementSpeed;

    void Awake()
    {
        stateMachine = new Enemy_State_Machine();
        // construct state instances and pass references they need
        idleState = new Enemy_Idle(this, stateMachine);
        // Create constructed state instances
        attackState = new Enemy_Attack(this, stateMachine);
        moveState = new Enemy_Move(this, stateMachine);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stateMachine.Initialize(idleState);
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.CurrentEnemyState?.FrameUpdate();
    }
    void FixedUpdate()
    {
        stateMachine?.CurrentEnemyState?.PhysicsUpdate();
    }
    public enum AnimationTriggerType
    {
        Damaged,
        Traveling
    }
    void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentEnemyState?.AnimationTriggerEvent(triggerType);
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player_Attack")
        {
            stats.currentHealth -= Player_Stats.Attack;
        }
    }
    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }
    public void MoveEnemy(Vector3 velocity)
    {
        moveState.rb.linearVelocity = velocity;
    }
    public void setAggroStatus(bool isAggroed)
    {
        this.isAggroed = isAggroed;
    }

    public void setRangeBool(bool isWithinRange)
    {
        this.isWithinRange = isWithinRange;
    }
    /*public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {
        }
    }*/
}
