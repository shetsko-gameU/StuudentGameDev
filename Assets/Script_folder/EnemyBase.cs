//using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static BaseStatsSO;

/// <summary>
/// The NPC brain. Owns the state machine and the four states every enemy shares
/// (EnemyIdle, EnemyMove, EnemyAttack, EnemyDeath), constructs them in Awake, and ticks the
/// current one from Update/FixedUpdate. Movement itself is NavMesh-only and lives in
/// EnemyMove; this class just holds the shared references and tuning those states read.
///
/// Aggro is pushed in from two child trigger colliders rather than polled here:
/// EnemyAggroCheck sets isAggroed (sight range) and EnemyStrikeDistanceCheck sets
/// isWithinRange (attack range). EnemyIdle additionally does an OverlapSphere sweep using
/// sightRange as a backstop.
///
/// Setup:
///   1. Put this on the enemy prefab ROOT, alongside a NavMeshAgent and a StatsManager.
///   2. Assign stats to that StatsManager. Leave animator empty and it auto-finds the
///      Animator on the nested model child.
///   3. Add two child GameObjects with trigger colliders (Is Trigger MUST be on): one with
///      EnemyAggroCheck sized to sight range, one with EnemyStrikeDistanceCheck sized to
///      attack range. See mushroom scout.prefab for a correct reference setup.
///   4. Set sightRange, moveTuning, and attackTuning. A sightRange of 0 disables the
///      OverlapSphere aggro backstop.
///   5. Bake a NavMesh in the room, or the agent cannot move.
/// </summary>
public class EnemyBase : MonoBehaviour, TriggerCheck
{
   [Header("Stats")]
    [Tooltip("This enemy's own StatsManager. Health, damage and MoveSpeed all come from here, " +
             "and its OnDied event is what LootDropper and EnemyManager listen to.")]
    public StatsManager stats;

    [Tooltip("The player's StatsManager. Assigned automatically by EnemyManager when this enemy " +
             "is spawned as part of a wave; only set it by hand for enemies placed directly in a scene.")]
    public StatsManager Player_Stats;

    [Tooltip("Leave empty. Auto-found on the nested model child at Start, which is where the " +
             "Animator actually lives on these prefabs.")]
    public Animator animator;

    [Tooltip("Optional raycast origins used by EnemyIdle's line-of-sight sweep.")]
    public List<Transform> raycasts;

    [Tooltip("If ticked, this enemy circles its target between swings instead of standing still. " +
             "Speed comes from Attack Tuning > Pivot Speed.")]
    public bool doesAttackPivot;

    [Tooltip("Excludes this enemy from guaranteed-kill effects (e.g. Legendary Roasted Whole Slime).")]
    public bool isBoss;

    [Tooltip("Radius of EnemyIdle's OverlapSphere aggro backstop, in units. Set it to roughly the " +
             "EnemyAggroCheck trigger radius. 0 disables the backstop entirely, leaving only the " +
             "trigger collider to notice the player.")]
    public float sightRange;

    [Header("Move Tuning")]
    [Tooltip("Chase speed and steering for this prefab. Read by EnemyMove and pushed onto the NavMeshAgent.")]
    public EnemyMoveTuning moveTuning = new EnemyMoveTuning();

    [Header("Attack Tuning")]
    [Tooltip("Swing rhythm and give-up timing for this prefab. Read by EnemyAttack.")]
    public EnemyAttackTuning attackTuning = new EnemyAttackTuning();

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

    // Removed 2026-09-10: public List<AttackBase> attacks.
    // AttackBase/MeleeAttack/ProjectileAttack were a data-driven attack system that was never
    // finished - Execute() was not called from anywhere in the project, every prefab had
    // attacks: [], and ProjectileAttack.Execute() even logged "melee attack". Leaving the list
    // and the SO types in place cost a reviewer real time working out that assets wired into
    // them could never run. Enemy damage now goes through EnemyHitbox, matching the player.

    [Tooltip("This enemy's attack hitbox. Leave empty to auto-find it in children at Start.")]
    public EnemyHitbox hitbox;

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

        // GetComponentInChildren, not GetComponent. On every spawnable enemy prefab the model
        // is a nested child, so the Animator lives there and the old root-only lookup always
        // returned null. The unguarded SetBool that used to follow then threw a
        // NullReferenceException every frame, which also meant the death check at the bottom
        // of Update was never reached. An Inspector assignment still wins if one is set.
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning($"EnemyBase on '{name}': no Animator on this object or its children. " +
                             "Animation states are skipped; the state machine still runs.");

        if (hitbox == null)
            hitbox = GetComponentInChildren<EnemyHitbox>();

        if (hitbox == null)
            Debug.LogWarning($"EnemyBase on '{name}': no EnemyHitbox in children, so this enemy " +
                             "cannot damage the player. Add one on a child positioned where its " +
                             "attack should connect.");
    }

    // Update is called once per frame
    void Update()
    {
        // Death is checked before anything else so that no failure further down this method can
        // starve the one transition that must always happen. The extra CurrentEnemyState guard
        // stops us re-entering EnemyDeath every frame while the death animation plays.
        if (stats != null && stats.currentHealth <= 0f && stateMachine.CurrentEnemyState != deathState)
        {
            stateMachine.ChangeState(deathState);
            return;
        }

        stateMachine.CurrentEnemyState?.FrameUpdate();
    }
    void FixedUpdate()
    {
        stateMachine?.CurrentEnemyState?.PhysicsUpdate();
        // Optional Dummy-training backstop. Disabled when sightRange == 0 (default for
        // trigger-aggro enemies) so we do not OverlapSphere every physics tick per enemy.
        if (sightRange > 0f)
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
        // Guarded because an enemy prefab with no Animator is a valid (if unanimated) setup,
        // and this used to be a second unguarded dereference alongside the one in Update.
        if (animator != null)
            animator.SetTrigger(EnemyAnimatorParams.AttackHash);
    }
    /// <summary>
    /// Nudges the enemy along the NavMesh. Used by EnemyIdle for its wander.
    ///
    /// This used to write moveState.rb.linearVelocity directly, driving the Rigidbody while
    /// the NavMeshAgent was simultaneously steering the same object - the two fought each
    /// other. Routing through agent.Move keeps NavMesh as the single movement authority and
    /// means wandering enemies stay on the mesh instead of sliding off it.
    /// </summary>
    public void MoveEnemy(Vector3 velocity)
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        navMeshAgent.Move(velocity * Time.deltaTime);
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
        if (sightRange <= 0f) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Dummy"))
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
