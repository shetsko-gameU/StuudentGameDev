using System.Collections;
using UnityEngine;

/// <summary>
/// The Dryad+Dryad ultimate — "large vine grows in front of the player and slams down,
/// dealing large damage." Activated like DashAbilitySO/SnaghettiAbilitySO/
/// RoastedSlimeAbilitySO, not a passive.
///
/// Reuses VineAttack.cs's cone-damage math (Physics.OverlapSphere + Vector3.Angle check)
/// since that's this project's established pattern for "vine deals damage in front of
/// something" — but as a direct player-cast burst instead of a spawned entity that tracks
/// the nearest enemy on its own. Origin and facing direction are captured once at the
/// moment of casting and held for the growDuration windup, then resolved as a single
/// damage cone — not a repeating tick like RoastedSlimeAbilitySO's rain.
/// </summary>
[CreateAssetMenu(menuName = "Game/Abilities/Dryad Delights")]
public class DryadDelightsAbilitySO : AbilitySO
{
    [Header("Wind-Up")]
    [Tooltip("How long the vine grows before slamming down. 0 = instant.")]
    public float growDuration = 0.5f;

    [Tooltip("Optional VFX spawned at the caster's position when the vine starts growing.")]
    public GameObject growEffect;

    [Header("Slam")]
    [Tooltip("How far the slam reaches.")]
    public float slamRange = 4f;

    [Tooltip("Cone angle of the slam in front of the caster, in degrees. 90 = wide sweep, 45 = narrow.")]
    [Range(10f, 180f)]
    public float slamAngle = 90f;

    public float slamDamage = 40f;

    [Tooltip("Which layer enemies are on.")]
    public LayerMask enemyLayer;

    [Tooltip("Optional VFX spawned at the caster's position when the vine slams down.")]
    public GameObject slamEffect;

    public override bool CanUse(GameObject user)
    {
        StatsManager stats = user.GetComponent<StatsManager>();
        if (stats != null && stats.IsDead) return false;

        return true;
    }

    public override void Activate(GameObject user)
    {
        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            Debug.LogWarning("DryadDelightsAbilitySO: No MonoBehaviour found on user to run coroutine.");
            return;
        }

        runner.StartCoroutine(SlamRoutine(user));
    }

    private IEnumerator SlamRoutine(GameObject user)
    {
        // Captured once — the slam lands where the player was facing when they cast it,
        // not wherever they happen to be facing once the wind-up finishes.
        Vector3 origin = user.transform.position;
        Vector3 forward = user.transform.forward;

        if (growEffect != null)
            Instantiate(growEffect, origin, Quaternion.LookRotation(forward));

        if (growDuration > 0f)
            yield return new WaitForSeconds(growDuration);

        if (slamEffect != null)
            Instantiate(slamEffect, origin, Quaternion.LookRotation(forward));

        DamageInCone(origin, forward);
    }

    private void DamageInCone(Vector3 origin, Vector3 forward)
    {
        Collider[] hits = Physics.OverlapSphere(origin, slamRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            Vector3 directionToEnemy = (hit.transform.position - origin).normalized;
            float angleTo = Vector3.Angle(forward, directionToEnemy);

            if (angleTo > slamAngle * 0.5f) continue;

            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats != null)
                enemyStats.TakeDamage(slamDamage);
        }
    }
}
