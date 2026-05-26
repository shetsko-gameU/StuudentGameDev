using UnityEngine;

/// <summary>
/// Attach to a prefab. Drag into SpawnEntity on your OnHitPassiveSO.
///
/// SeedMode — what happens after the seed lands:
///   Mine  — enemy steps on it → instant AOE explosion → dies (or leaves damage cloud).
///   Vine  — timer → vine that winds up and slaps nearby enemies.
///   Both  — vine on timer, BUT explodes if enemy steps on it first.
///
/// Mine explosion can optionally leave a damage cloud:
///   leaveCloudAfterExplosion = true → ticks damage in cloudRadius until cloudDuration runs out.
///   This is separate from the vine — the cloud just sits and deals tick damage, no slap.
///
/// Vine attack cycle:
///   Idle → enemy spotted → WindUp (slapWindUpTime) → Slap (cone damage) → Cooldown → Idle
/// </summary>
public class MushroomMine : MonoBehaviour
{
    // ------------------------------------------------------------------ Enums

    public enum SeedMode { Mine, Vine, Both }

    private enum Phase { Seed, Mine, Cloud, Vine, Dead }
    private enum VineState { Idle, WindUp, Slap, Cooldown }

    // ------------------------------------------------------------------ State

    private Phase currentPhase = Phase.Seed;
    private VineState vineState = VineState.Idle;

    // ------------------------------------------------------------------ Inspector — Seed

    [Header("Seed Phase")]
    public SeedMode seedMode = SeedMode.Vine;
    public float seedDuration = 2f;
    public GameObject seedVisual;
    public float throwForceMin = 3f;
    public float throwForceMax = 6f;
    public LayerMask enemyLayer;

    // ------------------------------------------------------------------ Inspector — Mine

    [Header("Mine Settings")]
    public float explosionRadius = 3f;
    public float explosionDamage = 25f;
    public GameObject explosionEffect;

    [Tooltip("If true: after exploding, leaves a damage cloud before dying. " +
             "The cloud just ticks damage — it does NOT use the vine slap attack.")]
    public bool leaveCloudAfterExplosion = false;

    // ------------------------------------------------------------------ Inspector — Cloud (post-explosion)

    [Header("Cloud Settings (mine only — used when leaveCloudAfterExplosion is on)")]
    [Tooltip("Visual shown during the cloud phase. Can be a particle system or mesh.")]
    public GameObject cloudVisual;

    [Tooltip("How long the damage cloud lasts before the mine destroys itself.")]
    public float cloudDuration = 6f;

    [Tooltip("Radius enemies must be inside to take cloud tick damage.")]
    public float cloudRadius = 3f;

    [Tooltip("Damage dealt per tick inside the cloud.")]
    public float cloudDamagePerTick = 8f;

    [Tooltip("Seconds between each cloud tick.")]
    public float cloudTickInterval = 1f;

    // ------------------------------------------------------------------ Inspector — Vine

    [Header("Vine Settings")]
    public GameObject vineVisual;
    public float vineDuration = 12f;
    public float vineDetectRange = 4f;

    // ------------------------------------------------------------------ Inspector — Slap attack

    [Header("Slap Attack")]
    [Tooltip("How long the vine winds up before slapping.")]
    public float slapWindUpTime = 0.8f;

    [Tooltip("How far the slap reaches.")]
    public float slapRange = 2.5f;

    [Tooltip("Cone angle of the slap in degrees. 90 = wide sweep, 45 = narrow.")]
    [Range(10f, 180f)]
    public float slapAngle = 90f;

    [Tooltip("Damage dealt on slap.")]
    public float slapDamage = 20f;

    [Tooltip("Cooldown between slaps.")]
    public float slapCooldown = 1.5f;

    [Tooltip("How fast the vine rotates to face its target.")]
    public float rotateSpeed = 8f;

    // ------------------------------------------------------------------ Inspector — Animator (optional)

    [Header("Animator (optional)")]
    public Animator vineAnimator;
    public string windUpTrigger = "WindUp";
    public string slapTrigger = "Slap";
    public string idleTrigger = "Idle";

    // ------------------------------------------------------------------ Inspector — Mushroom visuals

    [Header("Mushroom Visuals (optional)")]
    [Tooltip("Shown when the seed lands — the mushroom growing from the ground. " +
             "Hidden when the mine explodes or transitions to vine.")]
    public GameObject mushroomVisual;
    public MeshRenderer mushroomTop;
    public MeshRenderer mushroomBottom;
    public CapsuleCollider capsule;

    // ------------------------------------------------------------------ Runtime

    private Rigidbody rb;
    private float seedTimer = 0f;
    private float phaseTimer = 0f;
    private float stateTimer = 0f;
    private float tickTimer = 0f;
    private bool planted = false;
    private bool triggered = false;
    private Transform currentTarget;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (seedVisual != null) seedVisual.SetActive(true);
        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (vineVisual != null) vineVisual.SetActive(false);
        if (cloudVisual != null) cloudVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = false;
        if (mushroomBottom != null) mushroomBottom.enabled = false;
        if (capsule != null) capsule.enabled = false;
    }

    private void Start()
    {
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
        switch (currentPhase)
        {
            case Phase.Seed: if (planted) HandleSeedPhase(); break;
            case Phase.Cloud: HandleCloudPhase(); break;
            case Phase.Vine: HandleVinePhase(); break;
        }
    }

    // ------------------------------------------------------------------ Landing + trigger

    private void OnCollisionEnter(Collision collision)
    {
        if (planted) return;

        planted = true;

        // Seed has landed — swap from seed to mushroom visual
        if (seedVisual != null) seedVisual.SetActive(false);
        if (mushroomVisual != null) mushroomVisual.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!planted) return;
        if (currentPhase != Phase.Seed) return;
        if (triggered) return;
        if (seedMode == SeedMode.Vine) return;
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        triggered = true;
        ActivateMine();
    }

    // ------------------------------------------------------------------ Seed phase

    private void HandleSeedPhase()
    {
        seedTimer += Time.deltaTime;
        if (seedTimer < seedDuration) return;

        if (seedMode == SeedMode.Mine || seedMode == SeedMode.Both)
            ArmMine();
        else
            ActivateVine();
    }

    // ------------------------------------------------------------------ Mine

    private void ArmMine()
    {
        if (seedVisual != null) seedVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = true;
        if (mushroomBottom != null) mushroomBottom.enabled = true;
        if (capsule != null) capsule.enabled = true;
    }

    private void ActivateMine()
    {
        currentPhase = Phase.Mine;

        StopPhysics();

        if (seedVisual != null) seedVisual.SetActive(false);
        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (mushroomTop != null) mushroomTop.enabled = false;
        if (mushroomBottom != null) mushroomBottom.enabled = false;

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        DamageInRadius(explosionRadius, explosionDamage);

        // After exploding — leave a cloud or die immediately
        if (leaveCloudAfterExplosion)
            ActivateCloud();
        else
            Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Cloud phase

    /// <summary>
    /// Simple tick damage area left behind after the mine explodes.
    /// No slap, no wind-up — just sits and damages enemies nearby until cloudDuration expires.
    /// </summary>
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

    // ------------------------------------------------------------------ Vine activation

    private void ActivateVine()
    {
        currentPhase = Phase.Vine;
        phaseTimer = 0f;
        stateTimer = 0f;

        StopPhysics();

        if (seedVisual != null) seedVisual.SetActive(false);
        if (mushroomVisual != null) mushroomVisual.SetActive(false);
        if (vineVisual != null) vineVisual.SetActive(true);
        if (mushroomTop != null) mushroomTop.enabled = true;
        if (mushroomBottom != null) mushroomBottom.enabled = true;
        if (capsule != null) capsule.enabled = true;

        EnterVineState(VineState.Idle);
    }

    // ------------------------------------------------------------------ Vine phase

    private void HandleVinePhase()
    {
        phaseTimer += Time.deltaTime;
        stateTimer += Time.deltaTime;

        if (phaseTimer >= vineDuration)
        {
            Destroy(gameObject);
            return;
        }

        switch (vineState)
        {
            case VineState.Idle: HandleIdle(); break;
            case VineState.WindUp: HandleWindUp(); break;
            case VineState.Slap: HandleSlap(); break;
            case VineState.Cooldown: HandleCooldown(); break;
        }
    }

    // ------------------------------------------------------------------ Vine states

    private void HandleIdle()
    {
        currentTarget = FindNearestEnemy();
        if (currentTarget == null) return;

        FaceTarget(currentTarget.position);
        EnterVineState(VineState.WindUp);
    }

    private void HandleWindUp()
    {
        if (currentTarget != null)
            FaceTarget(currentTarget.position);

        if (stateTimer >= slapWindUpTime)
            EnterVineState(VineState.Slap);
    }

    private void HandleSlap()
    {
        DamageInCone(slapRange, slapAngle, slapDamage);
        EnterVineState(VineState.Cooldown);
    }

    private void HandleCooldown()
    {
        if (stateTimer >= slapCooldown)
            EnterVineState(VineState.Idle);
    }

    // ------------------------------------------------------------------ Vine state transitions

    private void EnterVineState(VineState newState)
    {
        vineState = newState;
        stateTimer = 0f;

        if (vineAnimator == null) return;

        switch (newState)
        {
            case VineState.Idle:
                if (!string.IsNullOrEmpty(idleTrigger))
                    vineAnimator.SetTrigger(idleTrigger);
                break;
            case VineState.WindUp:
                if (!string.IsNullOrEmpty(windUpTrigger))
                    vineAnimator.SetTrigger(windUpTrigger);
                break;
            case VineState.Slap:
                if (!string.IsNullOrEmpty(slapTrigger))
                    vineAnimator.SetTrigger(slapTrigger);
                break;
        }
    }

    // ------------------------------------------------------------------ Helpers

    private void StopPhysics()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private Transform FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, vineDetectRange, enemyLayer);
        Transform nearest = null;
        float closest = float.MaxValue;

        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime);
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

    private void DamageInCone(float range, float angle, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        foreach (Collider hit in hits)
        {
            Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
            float angleTo = Vector3.Angle(transform.forward, directionToEnemy);

            if (angleTo > angle * 0.5f) continue;

            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats != null)
                enemyStats.TakeDamage(damage);
        }
    }

    // ------------------------------------------------------------------ Gizmos

    private void OnDrawGizmosSelected()
    {
        // Detection range — yellow
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, vineDetectRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, vineDetectRange);

        // Explosion radius — red
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Cloud radius — orange
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, cloudRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, cloudRadius);

        // Slap range and cone — green
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, slapRange);
        Vector3 leftEdge = Quaternion.Euler(0, -slapAngle * 0.5f, 0) * transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, slapAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftEdge * slapRange);
        Gizmos.DrawRay(transform.position, rightEdge * slapRange);
    }
}