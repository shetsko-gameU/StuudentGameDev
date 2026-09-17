using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The NPC chase state. NavMesh is the ONLY movement backend: this state sets
/// navMeshAgent.destination on an interval and lets the agent do the steering, pathfinding
/// and rotation. It owns no physics.
///
/// It previously did both at once. It was a copy of PlayerMove, so it set the agent's
/// destination toward the target and then, in the same frame, applied a Rigidbody force in
/// the OPPOSITE direction (desiredDir = moveDir * -1) and called agent.Move() with its own
/// hand-integrated velocity. It also carried PlayerMove's whole ledge-fall system, which
/// disabled the agent mid-chase. All of that is gone. Enemies do not fall off ledges - the
/// NavMesh edge already stops them, which is the behaviour we want for NPCs.
///
/// Tuning lives on EnemyBase.moveTuning, not here. This class is a plain C# object rather
/// than a MonoBehaviour, so [SerializeField] on it does nothing - the old fields looked
/// Inspector-tunable but silently sat at 0, which is why enemies never moved.
///
/// Setup: nothing to configure on this class. Tune EnemyBase.moveTuning on the prefab and
/// make sure a NavMesh is baked in the room.
/// </summary>
public class EnemyMove : EnemyState
{
    private readonly StatsManager stats;

    /// <summary>Countdown to the next SetDestination call. Repathing every frame is wasted
    /// work; the agent keeps following its existing path in between.</summary>
    private float repathTimer;

    private bool warnedNotOnNavMesh;

    public EnemyMove(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        stats = enemy.GetComponent<StatsManager>();
    }

    public override void EnterState()
    {
        NavMeshAgent agent = enemy.navMeshAgent;
        if (agent == null) return;

        // The agent steers and rotates itself. PlayerMove turns updateRotation off because it
        // rotates the player model by hand for feel; NPCs have no such requirement.
        agent.updateRotation = true;
        ApplyTuning(agent);

        if (agent.isOnNavMesh)
            agent.isStopped = false;

        // Repath immediately on entry instead of waiting out the interval.
        repathTimer = 0f;
    }

    public override void ExitState()
    {
        NavMeshAgent agent = enemy.navMeshAgent;
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;

        // Leave the locomotion blend at rest, or the model keeps its walk pose while the
        // enemy stands still in Attack or Idle.
        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, 0f);
    }

    public override void FrameUpdate()
    {
        // De-aggro. This branch used to be unreachable: it was gated on a private
        // canSeeTarget field that was set true in the constructor and never written again.
        // isAggroed is now the real signal, pushed in by EnemyAggroCheck's OnTriggerExit
        // (which only started firing once the slime prefab's detection colliders were
        // actually marked Is Trigger).
        if (enemy.currentTarget == null || !enemy.isAggroed)
        {
            enemy.currentTarget = null;
            enemyStateMachine.ChangeState(enemy.idleState);
            return;
        }

        // In strike range - hand over to the attack state.
        if (enemy.isWithinRange)
        {
            enemyStateMachine.ChangeState(enemy.attackState);
            return;
        }

        NavMeshAgent agent = enemy.navMeshAgent;
        if (agent == null)
        {
            if (!warnedNotOnNavMesh)
            {
                warnedNotOnNavMesh = true;
                Debug.LogWarning($"EnemyMove on '{enemy.name}': no NavMeshAgent, so this enemy cannot chase.");
            }
            return;
        }

        // Re-read tuning every frame so buffs/debuffs to MoveSpeed take effect mid-chase.
        ApplyTuning(agent);

        // Scenes like GamePlayTesting ship with no baked NavMesh (m_NavMeshData is null), which
        // left every enemy frozen in playtests. When the agent is off-mesh, fall back to a
        // straight-line transform move so combat is still testable. Prefer baking a NavMesh
        // (Window > AI > Navigation, or NavMeshSurface) for real pathfinding around obstacles.
        if (!agent.isOnNavMesh)
        {
            if (!warnedNotOnNavMesh)
            {
                warnedNotOnNavMesh = true;
                Debug.LogWarning($"EnemyMove on '{enemy.name}': not on a baked NavMesh — using " +
                                 "straight-line chase fallback. Bake a NavMesh for proper pathfinding.");
            }

            ChaseWithoutNavMesh();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            agent.destination = enemy.currentTarget.transform.position;
            repathTimer = Mathf.Max(0.05f, enemy.moveTuning.repathInterval);
        }

        // Report the agent's actual achieved speed, not the target speed, so the walk blend
        // matches what the model is really doing (e.g. slowed by a corner or a crowd).
        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, agent.velocity.magnitude);
    }

    /// <summary>
    /// Direct chase used only when there is no baked NavMesh under this enemy. Disables the
    /// agent so it stops fighting the transform write, then walks straight at the target.
    /// </summary>
    private void ChaseWithoutNavMesh()
    {
        NavMeshAgent agent = enemy.navMeshAgent;
        if (agent.enabled)
            agent.enabled = false;

        Vector3 toTarget = enemy.currentTarget.transform.position - enemy.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.05f) return;

        Vector3 dir = toTarget / distance;
        float speed = enemy.moveTuning.moveSpeed * (stats != null ? Mathf.Max(0.05f, stats.MoveSpeed) : 1f);
        enemy.transform.position += dir * speed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                Quaternion.LookRotation(dir),
                enemy.moveTuning.angularSpeed * Mathf.Deg2Rad * Time.deltaTime);

        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, speed);
    }

    /// <summary>
    /// Pushes the prefab's tuning onto the agent. StatsManager.MoveSpeed is treated as a
    /// multiplier (1 = normal). Enemy BaseStatsSO moveSpeed should stay near 1 so chase
    /// speed is roughly moveTuning.moveSpeed (not tuning × 5).
    /// </summary>
    private void ApplyTuning(NavMeshAgent agent)
    {
        EnemyMoveTuning tuning = enemy.moveTuning;
        float speedMultiplier = stats != null ? Mathf.Max(0.05f, stats.MoveSpeed) : 1f;

        agent.speed = tuning.moveSpeed * speedMultiplier;
        agent.acceleration = tuning.acceleration;
        agent.angularSpeed = tuning.angularSpeed;
        agent.stoppingDistance = tuning.stoppingDistance;
    }
}
