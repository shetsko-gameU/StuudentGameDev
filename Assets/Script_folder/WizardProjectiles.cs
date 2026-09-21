using UnityEngine;

/// <summary>
/// The wizard's basic-attack projectile — spawned by AttackHitbox when its isRanged flag
/// is set, instead of enabling a melee trigger collider. Deals damage through the same
/// StatsManager.TakeDamage pipeline (dodge/defense/HealthSteal) melee hits use, and relays
/// a confirmed hit back to the AttackHitbox that spawned it via the onHit callback, so
/// every passive/sfx trigger that listens for AttackHitbox.OnEnemyHit keeps working
/// unchanged for a ranged attacker.
///
/// Setup:
///   1. Build a projectile prefab: any visual + a Rigidbody + a trigger Collider.
///   2. Add this component to it.
///   3. On the wand/staff's AttackHitbox, tick isRanged and drag this prefab into
///      projectilePrefab (and optionally assign muzzlePoint to the wand tip).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WizardProjectiles : MonoBehaviour
{
    public float Speed = 20f;
    public float Damage;
    public Rigidbody rb;

    [Tooltip("Seconds before the projectile despawns on its own if it never hits anything.")]
    public float lifeTime = 5f;

    private LayerMask hittableLayer;
    private StatsManager attackerStats;
    private System.Action<StatsManager> onHit;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    /// <summary>Called by AttackHitbox right after spawning this projectile.</summary>
    public void Initialize(float damage, StatsManager attacker, LayerMask enemyLayer, float speed, System.Action<StatsManager> onHitCallback)
    {
        Damage = damage;
        attackerStats = attacker;
        hittableLayer = enemyLayer;
        Speed = speed;
        onHit = onHitCallback;

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + transform.forward * Speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((hittableLayer.value & (1 << other.gameObject.layer)) == 0) return;

        StatsManager enemyStats = other.GetComponent<StatsManager>()
                               ?? other.GetComponentInParent<StatsManager>();

        if (enemyStats == null) return;

        bool landed = enemyStats.TakeDamage(Damage, attackerStats);

        if (landed)
            onHit?.Invoke(enemyStats);

        Destroy(gameObject);
    }
}
