using UnityEngine;

/// <summary>
/// The NPC attack state. Swings on a fixed cadence while the target stays in strike range,
/// optionally circling between swings, and hands back to EnemyMove once the target escapes.
///
/// This state does NOT deal damage. It only fires the Attack animator trigger; the attack
/// clip carries an animation event that calls EnemyHitbox.EnableHitbox/DisableHitbox, and
/// the hitbox is what applies damage. That mirrors how the player's AttackHitbox works, so
/// both sides of combat resolve hits the same way.
///
/// Setup:
///   1. Tune swing rhythm in EnemyBase > Attack Tuning.
///   2. Tick EnemyBase > Does Attack Pivot for enemies that should circle their target.
///   3. Add an EnemyHitbox to a child of the enemy and add EnableHitbox/DisableHitbox
///      animation events to the attack clip, or the enemy will swing harmlessly.
/// </summary>
public class EnemyAttack : EnemyState
{
    float attackTimer;
    float exitTimer;

    /// <summary>Where this enemy is circling to. Chosen once per swing rather than every
    /// frame - see PivotAround.</summary>
    Vector3 pivotTarget;
    float cumulativeRotation;
    float totalTargetAngle;
    float angleDirection = 1f;
    bool hasPivotTarget;

    public EnemyAttack(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public override void EnterState()
    {
        // Swing straight away on arrival rather than idling out a full cooldown first.
        attackTimer = enemy.attackTuning.timeBetweenAttacks;
        exitTimer = 0f;
        hasPivotTarget = false;

        if (enemy.navMeshAgent != null && enemy.navMeshAgent.isOnNavMesh)
            enemy.navMeshAgent.isStopped = true;

        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, 0f);
    }

    public override void ExitState()
    {
        if (enemy.navMeshAgent != null && enemy.navMeshAgent.isOnNavMesh)
            enemy.navMeshAgent.isStopped = false;
    }

    public override void FrameUpdate()
    {
        // De-aggro / target lost. The old version gated this on a private canSeeTarget field
        // that was set true in the constructor and never written again, so neither branch
        // could ever run and an enemy could get stuck attacking a corpse.
        if (enemy.currentTarget == null || !enemy.isAggroed)
        {
            enemy.currentTarget = null;
            enemyStateMachine.ChangeState(enemy.idleState);
            return;
        }

        EnemyAttackTuning tuning = enemy.attackTuning;

        attackTimer += Time.deltaTime;

        if (attackTimer >= tuning.timeBetweenAttacks)
        {
            attackTimer = 0f;
            hasPivotTarget = false;   // pick a fresh circling arc after each swing

            if (enemy.animator != null)
                enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, 0f);

            OnAttack();
        }
        else if (enemy.doesAttackPivot)
        {
            PivotAround();
        }

        // Give-up handling, with hysteresis so the enemy does not flip between chasing and
        // attacking every frame at the exact boundary of the strike collider. isWithinRange
        // is the primary signal (pushed in by EnemyStrikeDistanceCheck); the distance test is
        // a backstop for prefabs whose strike collider is missing or mis-sized.
        float distanceToTarget = Vector3.Distance(enemy.transform.position,
                                                  enemy.currentTarget.transform.position);

        if (!enemy.isWithinRange || distanceToTarget > tuning.distanceToCountExit)
            exitTimer += Time.deltaTime;
        else
            exitTimer = 0f;

        if (exitTimer >= tuning.timeToExitAfterAttack)
        {
            exitTimer = 0f;
            enemyStateMachine.ChangeState(enemy.moveState);
        }
    }

    /// <summary>
    /// Fires the swing. This state never applies damage itself - EnemyHitbox does, either
    /// from EnableHitbox/DisableHitbox animation events on the attack clip, or from the
    /// timer fallback below for enemies whose attack animation does not exist yet.
    /// </summary>
    public void OnAttack()
    {
        if (enemy == null) return;

        if (enemy.animator != null)
            enemy.animator.SetTrigger(EnemyAnimatorParams.AttackHash);

        // No-op when the hitbox has Use Swing Window unticked, i.e. once the clip has real
        // animation events driving the hit window instead.
        if (enemy.hitbox != null)
            enemy.hitbox.FireSwingWindow();
    }

    /// <summary>
    /// Circles the target between swings.
    ///
    /// This used to pick a brand new random pivot point AND reset cumulativeRotation to 0 on
    /// every single frame, so the "progress" tracking never accumulated and the enemy jittered
    /// in place instead of orbiting. The arc is now chosen once per swing (guarded by
    /// hasPivotTarget) and rotated toward incrementally across frames.
    /// </summary>
    void PivotAround()
    {
        if (!hasPivotTarget)
        {
            float distance = Vector3.Distance(enemy.transform.position,
                                              enemy.currentTarget.transform.position);
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            pivotTarget = enemy.currentTarget.transform.position + (randomOffset * distance);

            Vector3 currentDir = enemy.currentTarget.transform.position - enemy.transform.position;
            Vector3 targetDir = enemy.currentTarget.transform.position - pivotTarget;

            float signedAngle = Vector3.SignedAngle(currentDir, targetDir, Vector3.up);
            totalTargetAngle = Mathf.Abs(signedAngle);
            angleDirection = Mathf.Sign(signedAngle);
            cumulativeRotation = 0f;
            hasPivotTarget = true;
        }

        if (cumulativeRotation >= totalTargetAngle) return;

        float deltaAngle = enemy.attackTuning.pivotSpeed * Time.deltaTime;
        if (cumulativeRotation + deltaAngle > totalTargetAngle)
            deltaAngle = totalTargetAngle - cumulativeRotation;

        enemy.transform.RotateAround(enemy.currentTarget.transform.position,
                                     Vector3.up,
                                     deltaAngle * angleDirection);
        cumulativeRotation += deltaAngle;

        // Circling is movement, so keep the walk blend running while it happens. The pivot
        // moves the transform directly rather than through the agent, so agent.velocity is
        // zero here and cannot be used.
        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, enemy.moveTuning.moveSpeed);
    }
}
