//using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using static BaseStatsSO;

public class EnemyBase : MonoBehaviour, TriggerCheck
{
   [Header("Stats")]
    public StatsManager stats;
    public StatsManager Player_Stats;
    public Animator animator;
    public List<Transform> raycasts;
    public bool doesAttackPivot;
    //Vector3 moveValue;

    // TriggerCheck implementation
    public bool isAggroed { get; set; }
    public bool isWithinRange { get; set; }

    //States and State Machine
    public EnemyStateMachine stateMachine {get; set;}
    public EnemyIdle idleState { get; set; }
    public EnemyAttack attackState { get; set; }
    public EnemyMove moveState { get; set; }

    public float randomMovementRange;
    public float randomMovementSpeed;
   public GameObject currentTarget;

    void Awake()
    {
        stateMachine = new EnemyStateMachine();
        // construct state instances and pass references they need
        idleState = new EnemyIdle(this, stateMachine);
        // Create constructed state instances
        attackState = new EnemyAttack(this, stateMachine);
        moveState = new EnemyMove(this, stateMachine);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(currentTarget == null)
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
    public void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentEnemyState?.AnimationTriggerEvent(triggerType);
    }
    public void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerAttack")) return;

        if (stats == null)
        {
            Debug.LogError($"EnemyBase on '{name}': stats is null � assign StatsManager in Inspector.");
            return;
        }

        if (Player_Stats == null)
        {
            Debug.LogError($"EnemyBase on '{name}': playerStats is null � assign the Player StatsManager in Inspector.");
            return;
        }

        float damage = Player_Stats.GetDamageRoll();
        Debug.Log($"EnemyBase: '{name}' taking {damage} damage. Health before: {stats.currentHealth}");

        // IMPORTANT: must call TakeDamage � not currentHealth directly.
        // TakeDamage is where OnDied fires, which LootDropper listens to.
        stats.TakeDamage(damage);

        Debug.Log($"EnemyBase: '{name}' health after: {stats.currentHealth}. IsDead: {stats.IsDead}");
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
