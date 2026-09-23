using UnityEngine;

/// <summary>
/// Quick burst of movement in the direction the player is facing. Stays NavMeshAgent-clamped
/// (unlike PlayerMove's ledge-fall system) so dashing across a gap carries the player over
/// it instead of triggering a fall. Temporarily disables PlayerMove for the dash's duration
/// so its own per-frame Move() calls don't fight the dash.
///
/// Setup:
///   1. Create via Assets, Create, Game, Abilities, Dash.
///   2. Fill in dashSpeed/dashDuration; leave usePlayerModelForward on unless you have a
///      reason to dash along the raw transform.forward instead.
///   3. Drag it into AbilityRunner.primaryAbility.ability (or secondary) on the player.
/// </summary>
[CreateAssetMenu(menuName = "Game/Abilities/Dash")]
public class DashAbilitySO : AbilitySO
{
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.12f;

    public bool usePlayerModelForward = true;

    public override bool CanUse(GameObject user)
    {
        StatsManager stats = user.GetComponent<StatsManager>();
        if (stats != null && stats.IsDead)
        {
            return false;
        }

        PlayerMove pm = user.GetComponent<PlayerMove>();
        if (pm == null || pm.agent == null)
        {
            return false;
        }

        // No dashing mid-air — the agent is disabled while falling, so Move() would no-op anyway.
        if (pm.IsFalling)
        {
            return false;
        }

        // The state machine has the final say on whether a dash may start (it knows about
        // attacking, eating and death). Falls through to the checks above when the player has
        // no PlayerStateMachine, so this ability still works on a bare prefab.
        PlayerStateMachine stateMachine = user.GetComponent<PlayerStateMachine>();
        if (stateMachine != null && !stateMachine.CanEnterDash())
        {
            return false;
        }

        return true;
    }

    public override void Activate(GameObject user)
    {
        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            Debug.LogWarning("DashAbilitySO: No MonoBehaviour found on user to run coroutine.");
            return;
        }

        runner.StartCoroutine(DashRoutine(user));
    }

    private System.Collections.IEnumerator DashRoutine(GameObject user)
    {
        PlayerMove pm = user.GetComponent<PlayerMove>();
        StatsManager stats = user.GetComponent<StatsManager>();

        if (pm == null || pm.agent == null)
        {
            yield break;
        }

        // Pick direction (your model rotates, not the root)
        Vector3 dir = user.transform.forward;

        if (usePlayerModelForward && pm.playerModel != null)
        {
            dir = pm.playerModel.forward;
        }

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.forward;
        }

        dir.Normalize();

        // Disabling PlayerMove for the dash's duration is what stops its per-frame Move()
        // calls from fighting the dash. Side effect we rely on: no ledge probes run during
        // the dash, and the agent's mesh clamp stays active — so dashing across a gap
        // carries you over it instead of falling.
        //
        // PlayerStateMachine owns that toggle when one is present, because PlayerDeathHandler
        // toggles the same flag. The try/finally is the important part: the loop below can
        // exit early, and without it an early exit left the player permanently unable to move.
        PlayerStateMachine stateMachine = user.GetComponent<PlayerStateMachine>();

        if (stateMachine != null)
            stateMachine.BeginDash();
        else
            pm.enabled = false;

        try
        {
            float t = 0f;
            while (t < dashDuration)
            {
                // Bail out if the player died mid-dash so the agent doesn't keep
                // sliding the corpse around after PlayerDeathHandler takes over.
                if (stats != null && stats.IsDead)
                {
                    yield break;
                }

                t += Time.deltaTime;
                if (pm.agent.enabled && pm.agent.isOnNavMesh)
                    pm.agent.Move(dir * dashSpeed * Time.deltaTime);

                yield return null;
            }
        }
        finally
        {
            // EndDash deliberately does NOT re-enable movement if the player died, so this is
            // safe to run on the death path too.
            if (stateMachine != null)
                stateMachine.EndDash();
            else if (stats == null || !stats.IsDead)
                pm.enabled = true;
        }
    }
}
