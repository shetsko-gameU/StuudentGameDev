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

        Rigidbody rb = user.GetComponent<Rigidbody>();
        if (rb == null)
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
        Rigidbody rb = user.GetComponent<Rigidbody>();
        PlayerMove pm = user.GetComponent<PlayerMove>();

        if (rb == null)
        {
            yield break;
        }

        // Pick direction (your model rotates, not the root)
        Vector3 dir = user.transform.forward;

        if (usePlayerModelForward && pm != null && pm.playerModel != null)
        {
            dir = pm.playerModel.forward;
        }

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.forward;
        }

        dir.Normalize();

        // Temporarily disable PlayerMove so it doesn't overwrite velocity during dash
        if (pm != null)
        {
            pm.enabled = false;
        }

        Vector3 oldVel = rb.linearVelocity;

        float t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;

            // Set dash velocity every frame so nothing else “wins”
            rb.linearVelocity = new Vector3(dir.x * dashSpeed, rb.linearVelocity.y, dir.z * dashSpeed);

            yield return null;
        }

        // Restore
        rb.linearVelocity = oldVel;

        if (pm != null)
        {
            pm.enabled = true;
        }
    }
}