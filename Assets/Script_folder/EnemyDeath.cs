using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The NPC death state, and the single owner of an enemy's destruction.
///
/// Death used to be split across three places that fought each other: EnemyBase.Update
/// changed to this state whenever health hit 0 (so it re-entered every frame),
/// FrameUpdate below re-fired the Death animator trigger every frame, and
/// EnemyManager.HandleEnemyDied called Destroy immediately on the OnDied event - which won
/// the race, so the death animation was skipped entirely for every wave-spawned enemy.
///
/// Now: EnemyBase transitions here exactly once, this state runs its teardown once in
/// EnterState, and EnemyManager only stops tracking the enemy. Destruction is scheduled
/// here and nowhere else.
///
/// Loot is unaffected and still drops from LootDropper listening to StatsManager.OnDied,
/// which fires inside TakeDamage - well before this state runs.
///
/// Setup: nothing to configure. Give the enemy's Animator Controller a "Dead" bool
/// parameter and a death state with no outgoing transitions.
/// </summary>
public class EnemyDeath : EnemyState
{
    public EnemyDeath(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public override void EnterState()
    {
        // Stop steering and stop colliding. Without this the corpse keeps pathing toward the
        // player and keeps blocking movement for however long the death clip lasts.
        if (enemy.navMeshAgent != null)
        {
            if (enemy.navMeshAgent.isOnNavMesh)
                enemy.navMeshAgent.isStopped = true;

            enemy.navMeshAgent.enabled = false;
        }

        foreach (Collider collider in enemy.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        float deathAnimationLength = 0f;

        if (enemy.animator != null)
        {
            enemy.animator.SetFloat(EnemyAnimatorParams.SpeedHash, 0f);
            enemy.animator.SetBool(EnemyAnimatorParams.DeadHash, true);
            deathAnimationLength = GetDeathClipLength(enemy.animator);
        }

        Object.Destroy(enemy.gameObject, deathAnimationLength);
    }

    public override void FrameUpdate()
    {
        // Deliberately empty. Everything happens once in EnterState - this used to re-fire
        // the Death trigger and re-schedule Destroy on every frame.
    }

    /// <summary>
    /// How long to leave the corpse up before destroying it. Falls back to 0 when the
    /// controller has no clip whose name contains "death", so an enemy with no death
    /// animation disappears immediately instead of lingering for an arbitrary 2 seconds.
    /// </summary>
    float GetDeathClipLength(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null) return 0f;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip == null) continue;

            if (clip.name.IndexOf("death", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return clip.length;
        }

        return 0f;
    }
}
