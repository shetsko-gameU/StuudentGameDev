using UnityEngine;



/// <summary>
/// Attach to a prefab. Drag into SpawnEntity on your OnHitPassiveSO.
///
/// Phases:
///   Seed  — thrown with random force, lands on ground, shows mushroom visual.
///   Mine  — enemy steps on trigger → instant AOE explosion.
///   Cloud — optional tick damage area left after explosion.
///
/// For the vine slap attack use VineAttack.cs instead.
/// </summary>
public class MushroomMine : MonoBehaviour
{
    // ------------------------------------------------------------------ Phase

    private enum Phase { Seed, Mine, Cloud, Dead }
    private Phase currentPhase = Phase.Seed;

    // ------------------------------------------------------------------ Inspector — Seed

    [Header("Seed Phase")]
    [Tooltip("How long after landing before the mine arms itself.")]
    public float seedDuration = 2f;
    public GameObject seedVisual;
    public float throwForceMin = 3f;
    public float throwForceMax = 6f;

    [Tooltip("Layer that enemies are on. Used for trigger detection and damage.")]
    public LayerMask enemyLayer;

    // ------------------------------------------------------------------ Inspector — Mine

    [Header("Mine Settings")]
    public float explosionRadius = 3f;
    public float explosionDamage = 25f;

    [Tooltip("Optional particle or effect spawned at the moment of explosion.")]
    public GameObject explosionEffect;

    [Tooltip("If true: leaves a damage cloud after exploding instead of dying immediately.")]
    public bool leaveCloudAfterExplosion = false;

    // ------------------------------------------------------------------ Inspector — Cloud

    [Header("Cloud Settings")]
    [Tooltip("Visual shown during the cloud phase (e.g. a particle system).")]
    public GameObject cloudVisual;

    [Tooltip("How long the cloud stays active before the object destroys itself.")]
    public float cloudDuration = 6f;

    [Tooltip("Radius enemies must be inside to take cloud tick damage.")]
    public float cloudRadius = 3f;

    [Tooltip("Damage dealt per tick to each enemy inside the cloud.")]
    public float cloudDamagePerTick = 8f;

    [Tooltip("Seconds between each cloud damage tick.")]
    public float cloudTickInterval = 1f;

    // ------------------------------------------------------------------ Inspector — Mushroom visuals

    [Header("Mushroom Visuals")]
    [Tooltip("Shown when the seed lands. Hidden when the mine explodes.")]
    public GameObject mushroomVisual;
    public MeshRenderer mushroomTop;
    public MeshRenderer mushroomBottom;
    public CapsuleCollider capsule;

    // ------------------------------------------------------------------ Runtime

    private Rigidbody rb;
    private float seedTimer = 0f;
    private float phaseTimer = 0f;
    private float tickTimer = 0f;
    private bool planted = false;
    private bool triggered = false;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (seedVisual != null) seedVisual.SetActive(true);
        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (cloudVisual != null) cloudVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = false;
        if (mushroomBottom != null) mushroomBottom.enabled = false;
        if (capsule != null) capsule.enabled = false;
    }

    private void Start()
    {
        ThrowSeed();
    }

    private void Update()
    {
        switch (currentPhase)
        {
            case Phase.Seed: if (planted) HandleSeedPhase(); break;
            case Phase.Cloud: HandleCloudPhase(); break;
        }
    }

    // ------------------------------------------------------------------ Seed throw

    private void ThrowSeed()
    {
        if (rb == null) return;

        float force = Random.Range(throwForceMin, throwForceMax);
        Vector3 direction = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    // ------------------------------------------------------------------ Landing

    private void OnCollisionEnter(Collision collision)
    {
        if (planted) return;

        planted = true;

        // Seed landed — show mushroom visual
        if (seedVisual != null) seedVisual.SetActive(false);
        if (mushroomVisual != null) mushroomVisual.SetActive(true);
    }

    // ------------------------------------------------------------------ Trigger detection

    private void OnTriggerEnter(Collider other)
    {
        if (!planted) return;
        if (currentPhase != Phase.Seed) return;
        if (triggered) return;
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        triggered = true;
        ActivateMine();
    }

    // ------------------------------------------------------------------ Seed phase

    private void HandleSeedPhase()
    {
        seedTimer += Time.deltaTime;

        if (seedTimer >= seedDuration)
            ArmMine();
    }

    // ------------------------------------------------------------------ Mine

    private void ArmMine()
    {
        // Show armed state visually — mushroom top and bottom enable
        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = true;
        if (mushroomBottom != null) mushroomBottom.enabled = true;
        if (capsule != null) capsule.enabled = true;

        // Phase stays as Seed so trigger can still fire
    }

    private void ActivateMine()
    {
        currentPhase = Phase.Mine;

        StopPhysics();

        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = false;
        if (mushroomBottom != null) mushroomBottom.enabled = false;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        DamageInRadius(explosionRadius, explosionDamage);

        if (leaveCloudAfterExplosion)
            ActivateCloud();
        else
            Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Cloud

    private void ActivateCloud()
    {
        currentPhase = Phase.Cloud;
        phaseTimer = 0f;
        tickTimer = 0f;

        if (cloudVisual != null) cloudVisual.SetActive(true);
    }

    private void HandleCloudPhase()
    {
        phaseTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= cloudTickInterval)
        {
            tickTimer = 0f;
            DamageInRadius(cloudRadius, cloudDamagePerTick);
        }

        if (phaseTimer >= cloudDuration)
            Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Helpers

    private void StopPhysics()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void DamageInRadius(float radius, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (Collider hit in hits)
        {
            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats != null)
                enemyStats.TakeDamage(damage);
        }
    }

    // ------------------------------------------------------------------ Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, cloudRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, cloudRadius);
    }
}