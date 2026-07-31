using UnityEngine;

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

<<<<<<< HEAD
        Rigidbody rb = user.GetComponent<Rigidbody>();
        if (rb == null)
=======
        PlayerMove pm = user.GetComponent<PlayerMove>();
        if (pm == null || pm.agent == null)
        {
            return false;
        }

        // No dashing mid-air â€” the agent is disabled while falling, so Move() would no-op anyway.
        if (pm.IsFalling)
>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
        Rigidbody rb = user.GetComponent<Rigidbody>();
        PlayerMove pm = user.GetComponent<PlayerMove>();

        if (rb == null)
=======
        PlayerMove pm = user.GetComponent<PlayerMove>();
        StatsManager stats = user.GetComponent<StatsManager>();

        if (pm == null || pm.agent == null)
>>>>>>> ScriptBreanchfixs
        {
            yield break;
        }

        // Pick direction (your model rotates, not the root)
        Vector3 dir = user.transform.forward;

<<<<<<< HEAD
        if (usePlayerModelForward && pm != null && pm.playerModel != null)
=======
        if (usePlayerModelForward && pm.playerModel != null)
>>>>>>> ScriptBreanchfixs
        {
            dir = pm.playerModel.forward;
        }

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.forward;
        }

        dir.Normalize();

<<<<<<< HEAD
        // Temporarily disable PlayerMove so it doesn't overwrite velocity during dash
        if (pm != null)
        {
            pm.enabled = false;
        }

        Vector3 oldVel = rb.linearVelocity;
=======
        // Temporarily disable PlayerMove so it doesn't fight the dash with its own Move() calls.
        // Side effect we rely on: no ledge probes run during the dash, and the agent's mesh
        // clamp stays active â€” so dashing across a gap carries you over it instead of falling.
        pm.enabled = false;
>>>>>>> ScriptBreanchfixs

        float t = 0f;
        while (t < dashDuration)
        {
<<<<<<< HEAD
            t += Time.deltaTime;

            // Set dash velocity every frame so nothing else “wins”
            rb.linearVelocity = new Vector3(dir.x * dashSpeed, rb.linearVelocity.y, dir.z * dashSpeed);
=======
            // Bail out if the player died mid-dash so the agent doesn't keep
            // sliding the corpse around after PlayerDeathHandler takes over.
            if (stats != null && stats.IsDead)
            {
                yield break;
            }

            t += Time.deltaTime;
            if (pm.agent.enabled && pm.agent.isOnNavMesh)
                pm.agent.Move(dir * dashSpeed * Time.deltaTime);
>>>>>>> ScriptBreanchfixs

            yield return null;
        }

<<<<<<< HEAD
        // Restore
        rb.linearVelocity = oldVel;

        if (pm != null)
        {
            pm.enabled = true;
        }
    }
}
=======
        pm.enabled = true;
    }
}
>>>>>>> ScriptBreanchfixs
