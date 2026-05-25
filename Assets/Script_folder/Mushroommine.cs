using UnityEngine;

/// <summary>
/// Attach to a prefab. Drag that prefab into SpawnEntity on your OnHitPassiveSO.
///
/// Three behaviours — mix and match in the Inspector:
///
///   Land Mine    — enemy steps on the trigger → instant AOE explosion damage.
///                  Toggle: explodeOnEnemyContact = true
///
///   Damage Cloud — after exploding (or after seed timer), leaves a cloud that
///                  ticks damage to all enemies inside it every tickInterval seconds.
///                  Toggle: leaveCloudAfterExplosion = true
///
///   Timed Vine   — if no enemy triggers it, transitions to vine after seedDuration.
///                  Toggle: transitionToVineOnTimer = true
///
/// Prefab setup:
///   MushroomMine (root)
///     ├── Rigidbody
///     ├── SphereCollider  (non-trigger — physics landing)
///     ├── SphereCollider  (IS trigger  — enemy detection, set radius to desired trigger size)
///     ├── MushroomMine script
///     ├── SeedVisual  (child — shown before explosion)
///     └── VineVisual  (child — shown during cloud/vine phase)
/// </summary>
public class MushroomMine : MonoBehaviour
{
    // ------------------------------------------------------------------ Phase

    private enum Phase { Seed, Active, Dead }
    private Phase currentPhase = Phase.Seed;

    // ------------------------------------------------------------------ Inspector — Seed / Throw

    [Header("Seed Phase")]
    [Tooltip("Visual shown before the mine activates.")]
    public GameObject seedVisual;

    [Tooltip("How long before automatically transitioning (if transitionToVineOnTimer is on).")]
    public float seedDuration = 2f;

    [Tooltip("Min/Max force applied when spawned so seeds scatter.")]
    public float throwForceMin = 3f;
    public float throwForceMax = 6f;

    // ------------------------------------------------------------------ Inspector — Behaviour toggles

    [Header("Behaviour — toggle what this mine does")]

    [Tooltip("If true: explodes when an enemy enters the trigger collider.")]
    public bool explodeOnEnemyContact = true;

    [Tooltip("If true: when the seed timer runs out, transitions to vine without needing an enemy to step on it.")]
    public bool transitionToVineOnTimer = false;

    [Tooltip("Layer mask used to detect enemy triggers. Set this to your enemy layer.")]
    public LayerMask enemyLayer;

    // ------------------------------------------------------------------ Inspector — Explosion

    [Header("Explosion (instant AOE on contact)")]

    [Tooltip("Radius of the instant explosion damage.")]
    public float explosionRadius = 3f;

    [Tooltip("Damage dealt instantly to all enemies in explosion radius.")]
    public float explosionDamage = 25f;

    [Tooltip("Particle or effect prefab spawned at explosion. Optional.")]
    public GameObject explosionEffect;

    // ------------------------------------------------------------------ Inspector — Cloud / Vine

    [Header("Cloud / Vine (tick damage after explosion)")]

    [Tooltip("If true: after exploding, leaves a damage cloud. If false: mine is destroyed after explosion.")]
    public bool leaveCloudAfterExplosion = true;

    [Tooltip("How long the damage cloud / vine stays active.")]
    public float cloudDuration = 8f;

    [Tooltip("Radius enemies must be inside to take cloud tick damage.")]
    public float cloudRadius = 3f;

    [Tooltip("Damage dealt per tick to each enemy in the cloud.")]
    public float cloudDamagePerTick = 8f;

    [Tooltip("Seconds between each tick of cloud damage.")]
    public float cloudTickInterval = 1f;

    [Tooltip("Visual shown during the cloud / vine phase.")]
    public GameObject vineVisual;

    // ------------------------------------------------------------------ Runtime

    private Rigidbody rb;
    private float seedTimer = 0f;
    private float cloudTimer = 0f;
    private float tickTimer = 0f;
    private bool triggered = false;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (seedVisual != null) seedVisual.SetActive(true);
        if (vineVisual != null) vineVisual.SetActive(false);
    }

    private void Start()
    {
        // Throw with random force so seeds scatter when spawned
        if (rb != null)
        {
            float force = Random.Range(throwForceMin, throwForceMax);
            Vector3 direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }

    private void Update()
    {
        if (currentPhase == Phase.Seed)
        {
            HandleSeedPhase();
        }
        else if (currentPhase == Phase.Active)
        {
            HandleActivePhase();
        }
    }

    // ------------------------------------------------------------------ Trigger detection

    private void OnTriggerEnter(Collider other)
    {
        // Only react during seed phase and only to the enemy layer
        if (currentPhase != Phase.Seed) return;
        if (triggered) return;
        if (!explodeOnEnemyContact) return;

        // Check if the colliding object is on the enemy layer
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        triggered = true;
        Activate();
    }

    // ------------------------------------------------------------------ Phases

    private void HandleSeedPhase()
    {
        if (!transitionToVineOnTimer) return;

        seedTimer += Time.deltaTime;
        if (seedTimer >= seedDuration)
            Activate();
    }

    private void HandleActivePhase()
    {
        if (!leaveCloudAfterExplosion)
        {
            Destroy(gameObject);
            return;
        }

        // Tick cloud damage
        tickTimer += Time.deltaTime;
        cloudTimer += Time.deltaTime;

        if (tickTimer >= cloudTickInterval)
        {
            tickTimer = 0f;
            DamageEnemiesInCloud();
        }

        if (cloudTimer >= cloudDuration)
            Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Activation

    private void Activate()
    {
        currentPhase = Phase.Active;

        // Stop physics so the mine stays put
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Swap visuals
        if (seedVisual != null) seedVisual.SetActive(false);

        if (leaveCloudAfterExplosion && vineVisual != null)
            vineVisual.SetActive(true);

        // Spawn explosion effect
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Deal instant explosion damage
        if (explosionDamage > 0f)
            DamageEnemiesInExplosion();

        // If no cloud, destroy immediately
        if (!leaveCloudAfterExplosion)
            Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Damage

    private void DamageEnemiesInExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats != null)
                enemyStats.TakeDamage(explosionDamage);
        }
    }

    private void DamageEnemiesInCloud()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, cloudRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats != null)
                enemyStats.TakeDamage(cloudDamagePerTick);
        }
    }

    // ------------------------------------------------------------------ Scene view debug

    private void OnDrawGizmosSelected()
    {
        // Explosion radius — red
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Cloud radius — green
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.2f);
        Gizmos.DrawSphere(transform.position, cloudRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, cloudRadius);
    }
}