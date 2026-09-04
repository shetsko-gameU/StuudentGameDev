//using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static BaseStatsSO;

public class EnemyBase : MonoBehaviour, TriggerCheck
{
   [Header("Stats")]
    public StatsManager stats;
    public StatsManager Player_Stats;
    public Animator animator;
    public List<Transform> raycasts;
    public bool doesAttackPivot;

    [Tooltip("Excludes this enemy from guaranteed-kill effects (e.g. Legendary Roasted Whole Slime).")]
    public bool isBoss;

    public float sightRange;
    //Vector3 moveValue;

    // TriggerCheck implementation
    public bool isAggroed { get; set; }
    public bool isWithinRange { get; set; }

    //States and State Machine
    public EnemyStateMachine stateMachine {get; set;}
    public EnemyIdle idleState { get; set; }
    public EnemyAttack attackState { get; set; }
    public EnemyMove moveState { get; set; }
    public EnemyDeath deathState { get; set; }

    public float randomMovementRange;
    public float randomMovementSpeed;

    public List<AttackBase> attacks;
    public float baseFlyHeight;
    public bool canFly;
   public GameObject currentTarget;
   public NavMeshAgent navMeshAgent;
   public LayerMask layer;



    void Awake()
    {
        stateMachine = new EnemyStateMachine();
        // construct state instances and pass references they need
        idleState = new EnemyIdle(this, stateMachine);
        // Create constructed state instances
        attackState = new EnemyAttack(this, stateMachine);
        moveState = new EnemyMove(this, stateMachine);
        deathState = new EnemyDeath(this, stateMachine);
        
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(currentTarget == null)
            stateMachine.Initialize(idleState);
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.CurrentEnemyState?.FrameUpdate();
        Debug.Log("Current State: " + stateMachine.CurrentEnemyState?.ToString());
        if(navMeshAgent.velocity.magnitude != 0 || stateMachine.CurrentEnemyState == idleState)
        {
            animator.SetBool("Move", true);
        }
        else
        {
            animator.SetBool("Move", false);
        }
        if(stats != null && stats.currentHealth <= 0)
        {
            stateMachine.ChangeState(deathState);
        }
    }
    void FixedUpdate()
    {
        stateMachine?.CurrentEnemyState?.PhysicsUpdate();
        CheckForDummy();
    }
    public enum AnimationTriggerType
    {
        Attack,
        Damaged,
        Death,
        Move
    }
    public void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentEnemyState?.AnimationTriggerEvent(triggerType);
    }
    // Commented out 2026-06-29: this duplicated AttackHitbox's damage-dealing. The sword mesh
    // ("warrior blade") is tagged PlayerAttack and carries the trigger collider AttackHitbox
    // controls, so every hitbox connection fired BOTH AttackHitbox.OnTriggerEnter (the real
    // pipeline — combo-scaled damage, HealthSteal, OnEnemyHit for passives) AND this method
    // (raw Attack stat, no multiplier, no HealthSteal, no passive triggers) from the same
    // physical contact — enemies were taking damage twice per hit through two different formulas.
    //public void OnTriggerEnter(Collider other)
    //{
    //    if (!other.gameObject.CompareTag("PlayerAttack")) return;
    //
    //    if (stats == null)
    //    {
    //        Debug.LogError($"EnemyBase on '{name}': stats is null — assign StatsManager in Inspector.");
    //        return;
    //    }
    //
    //    if (Player_Stats == null)
    //    {
    //        Debug.LogError($"EnemyBase on '{name}': playerStats is null — assign the Player StatsManager in Inspector.");
    //        return;
    //    }
    //
    //    float damage = Player_Stats.GetDamageRoll();
    //    Debug.Log($"EnemyBase: '{name}' taking {damage} damage. Health before: {stats.currentHealth}");
    //
    //    // IMPORTANT: must call TakeDamage — not currentHealth directly.
    //    // TakeDamage is where OnDied fires, which LootDropper listens to.
    //    stats.TakeDamage(damage);
    //
    //    Debug.Log($"EnemyBase: '{name}' health after: {stats.currentHealth}. IsDead: {stats.IsDead}");
    //}

    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }
    public void MoveEnemy(Vector3 velocity)
    {
        moveState.rb.linearVelocity = velocity;
    }
    public void SetAggroStatus(bool isAggroed)
    {
        this.isAggroed = isAggroed;
    }

    public void SetRangeBool(bool isWithinRange)
    {
        this.isWithinRange = isWithinRange;
    }
    public void CheckForDummy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightRange);
        foreach (var hitCollider in hitColliders)
        {
            if(hitCollider.gameObject.CompareTag("Dummy"))
            {
                currentTarget = hitCollider.gameObject;
            }
        }
        
    }
    /*public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {

        }
    }*/
}
