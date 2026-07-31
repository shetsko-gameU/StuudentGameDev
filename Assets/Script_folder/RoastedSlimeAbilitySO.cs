using System.Collections;
using UnityEngine;

/// <summary>
/// The Slime+Slime ultimate — activated (like DashAbilitySO/SnaghettiAbilitySO), not a
/// passive. On activation, deals AOE damage in a radius centered on the caster's position
/// AT THE MOMENT OF CASTING — a stationary "rain" zone that doesn't follow the player as
/// they move, matching "rains down slime over [an] area" rather than a personal aura.
/// Damage ticks repeatedly for rainDuration seconds, same DoT-over-an-area shape as
/// MushroomMine's cloud phase (Physics.OverlapSphere + StatsManager.TakeDamage per tick).
///
/// Rarity note: same pattern as SnaghettiAbilitySO — this script has no built-in rarity
/// awareness. To build the GDD's Legendary tier ("can kill all enemies, not including
/// boss"), create a SEPARATE RoastedSlimeAbilitySO asset for Legendary with
/// guaranteedKillNonBoss ticked on, and separate lower-tier assets with it left off —
/// each wrapped by its own UltFoodSO, same as Snaghetti's per-tier assets.
/// </summary>
[CreateAssetMenu(menuName = "Game/Abilities/Roasted Whole Slime")]
public class RoastedSlimeAbilitySO : AbilitySO
{
    [Header("Rain Area")]
    [Tooltip("Radius of the damage zone, centered on the caster's position at the moment of activation.")]
    public float radius = 8f;

    [Tooltip("Which layer enemies are on.")]
    public LayerMask enemyLayer;

    [Header("Damage Over Time")]
    public float rainDuration = 4f;
    public float tickInterval = 0.5f;
    public float damagePerTick = 10f;

    [Header("Legendary Behavior")]
    [Tooltip("If true, every tick guarantees a kill on any non-boss enemy in range (goes " +
             "through the normal TakeDamage pipeline with a huge damage value, so it still " +
             "respects DodgeChance) instead of dealing damagePerTick. Only tick this on a " +
             "Legendary-tier asset — leave off for Common/Rare/Epic.")]
    public bool guaranteedKillNonBoss = false;

    [Tooltip("Optional VFX spawned once at the rain's center when activated.")]
    public GameObject rainEffect;

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
            Debug.LogWarning("RoastedSlimeAbilitySO: No MonoBehaviour found on user to run coroutine.");
            return;
        }

        runner.StartCoroutine(RainRoutine(user));
    }

    private IEnumerator RainRoutine(GameObject user)
    {
        Vector3 center = user.transform.position;

        if (rainEffect != null)
            Instantiate(rainEffect, center, Quaternion.identity);

        float elapsed = 0f;
        while (elapsed < rainDuration)
        {
            DamageInRadius(center);

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
    }

    private void DamageInRadius(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayer);

        foreach (Collider hit in hits)
        {
            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats == null) continue;

            if (guaranteedKillNonBoss)
            {
                EnemyBase enemyBase = hit.GetComponent<EnemyBase>()
                                   ?? hit.GetComponentInParent<EnemyBase>();

                bool isBoss = enemyBase != null && enemyBase.isBoss;

                if (!isBoss)
                {
                    // Huge, deliberately non-infinite damage value — goes through the normal
                    // TakeDamage pipeline (dodge check, defense subtraction, OnDied/OnAnyDied)
                    // so loot drops and kill passives still fire correctly for these kills.
                    enemyStats.TakeDamage(float.MaxValue);
                    continue;
                }
            }

            enemyStats.TakeDamage(damagePerTick);
        }
    }
}
