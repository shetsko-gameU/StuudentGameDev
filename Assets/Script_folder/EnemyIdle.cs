using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The NPC resting state: wanders inside randomMovementRange of wherever the enemy started,
/// and watches for a target. Hands over to EnemyMove the moment isAggroed goes true.
///
/// Two things notice the player. EnemyAggroCheck's trigger collider is the primary path and
/// pushes isAggroed in from outside. CheckDistance below is a backstop OverlapSphere using
/// EnemyBase.sightRange, which covers enemies whose trigger collider is missing or
/// mis-sized. A sightRange of 0 disables the backstop.
///
/// Setup: set randomMovementRange (how far it drifts from its spawn point) and
/// randomMovementSpeed on EnemyBase. Set randomMovementRange to 0 for a sentry that holds
/// position.
/// </summary>
public class EnemyIdle : EnemyState
{
    public List<Transform> raycastPoints;

    public Vector3 targetPosition;
    public Vector3 direction;
    Vector3 startPosition;

    public EnemyIdle(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        raycastPoints = enemy.raycasts;
    }

    public override void EnterState()
    {
        startPosition = enemy.transform.position;
        targetPosition = GetRandomPointInRadius();
    }

    public override void ExitState()
    {
        // Park the locomotion blend so the model is not left mid-walk when we hand over.
        if (enemy.animator != null)
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, 0f);
    }

    public override void FrameUpdate()
    {
        if (enemy.isAggroed && enemy.currentTarget != null)
        {
            enemyStateMachine.ChangeState(enemy.moveState);
            return;
        }

        Vector3 offsetFromStart = enemy.transform.position - startPosition;
        if (offsetFromStart.magnitude >= enemy.randomMovementRange)
        {
            targetPosition = GetRandomPointInRadius();

            // Warp, not a direct transform.position write. Assigning transform.position on an
            // object with an active NavMeshAgent desyncs the agent from its own internal
            // position and it snaps back the next frame; Warp moves both together.
            Vector3 clamped = startPosition + (offsetFromStart.normalized * enemy.randomMovementRange);
            if (enemy.navMeshAgent != null && enemy.navMeshAgent.isOnNavMesh)
                enemy.navMeshAgent.Warp(clamped);
            else
                enemy.transform.position = clamped;
        }

        direction = (targetPosition - enemy.transform.position).normalized;
        enemy.MoveEnemy(direction * enemy.randomMovementSpeed);

        if ((enemy.transform.position - targetPosition).magnitude < 0.5f)
            targetPosition = GetRandomPointInRadius();

        if (enemy.animator != null)
        {
            float wanderSpeed = enemy.navMeshAgent != null ? enemy.navMeshAgent.velocity.magnitude : 0f;
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, wanderSpeed);
        }

        CheckDistance();
    }

    Vector3 GetRandomPointInRadius()
    {
        Vector2 randomPosition = UnityEngine.Random.insideUnitCircle * enemy.randomMovementRange;
        Vector3 adjustedPosition = new Vector3(randomPosition.x, 0, randomPosition.y);
        return startPosition + adjustedPosition;
    }

    /// <summary>
    /// Backstop target detection. Deliberately cheap and dumb: any Player or Dummy inside
    /// sightRange aggros this enemy, with no line-of-sight check.
    ///
    /// A CheckLineOfSite() raycast variant used to sit alongside this, but its call site was
    /// commented out so it never ran, and it depended on the raycasts list being populated -
    /// which it is on the slime and mushroom scout, but on no other enemy prefab. It has
    /// been removed rather than left looking functional. If proper line-of-sight matters
    /// later, add it here as a Physics.Linecast between this enemy and currentTarget instead
    /// of a fan of per-point raycasts.
    /// </summary>
    void CheckDistance()
    {
        if (enemy.sightRange <= 0f) return;

        Collider[] hitColliders = Physics.OverlapSphere(enemy.transform.position, enemy.sightRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Dummy") || hitCollider.gameObject.CompareTag("Player"))
            {
                enemy.currentTarget = hitCollider.gameObject;
                enemy.isAggroed = true;
                return;
            }
        }
    }
}
