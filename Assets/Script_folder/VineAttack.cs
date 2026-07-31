using UnityEngine;

/// <summary>
/// Attach to a prefab. Drag into SpawnEntity on your OnHitPassiveSO.
///
/// Phases:
///   Seed — thrown with random force, lands on ground, shows mushroom visual.
///   Vine — grows after seedDuration, detects nearby enemies, winds up and slaps them.
///
/// Vine attack cycle:
///   Idle → enemy spotted → WindUp (slapWindUpTime) → Slap (cone damage) → Cooldown → Idle
///
/// For the mine explosion and cloud use MushroomMine.cs instead.
/// </summary>
public class VineAttack : MonoBehaviour
{
    // ------------------------------------------------------------------ Phase / state

    private enum Phase { Seed, Vine, Dead }
    private enum VineState { Idle, WindUp, Slap, Cooldown }

    private Phase currentPhase = Phase.Seed;
    private VineState vineState = VineState.Idle;

    // ------------------------------------------------------------------ Inspector — Seed

    [Header("Seed Phase")]
    [Tooltip("How long after landing before the vine grows.")]
    public float seedDuration = 2f;
    public GameObject seedVisual;
    public float throwForceMin = 3f;
    public float throwForceMax = 6f;

    [Tooltip("Layer that enemies are on.")]
    public LayerMask enemyLayer;

    // ------------------------------------------------------------------ Inspector — Vine

    [Header("Vine Settings")]
    public GameObject vineVisual;

    [Tooltip("How long the vine stays active before destroying itself.")]
    public float vineDuration = 12f;

    [Tooltip("Radius the vine scans for enemies.")]
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

    // ------------------------------------------------------------------ Inspector — Animator

    [Header("Animator (optional)")]
    [Tooltip("Animator on the vine visual. Leave empty if no animations.")]
    public Animator vineAnimator;
    public string windUpTrigger = "WindUp";
    public string slapTrigger = "Slap";
    public string idleTrigger = "Idle";

    // ------------------------------------------------------------------ Runtime

    private Rigidbody rb;
    private float seedTimer = 0f;
    private float vineTimer = 0f;
    private float stateTimer = 0f;
    private bool planted = false;
    private Transform currentTarget;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (seedVisual != null) seedVisual.SetActive(true);
        if (vineVisual != null) vineVisual.SetActive(false);
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
            case Phase.Vine: HandleVinePhase(); break;
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

        // Seed landed — hide seed visual
        if (seedVisual != null) seedVisual.SetActive(false);
    }

    // ------------------------------------------------------------------ Seed phase

    private void HandleSeedPhase()
    {
        seedTimer += Time.deltaTime;

        if (seedTimer >= seedDuration)
            ActivateVine();
    }

    // ------------------------------------------------------------------ Vine activation

    private void ActivateVine()
    {
        currentPhase = Phase.Vine;
        vineTimer = 0f;
        stateTimer = 0f;

        StopPhysics();

        if (vineVisual != null) vineVisual.SetActive(true);

        EnterVineState(VineState.Idle);
    }

    // ------------------------------------------------------------------ Vine phase

    private void HandleVinePhase()
    {
        vineTimer += Time.deltaTime;
        stateTimer += Time.deltaTime;

        if (vineTimer >= vineDuration)
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

        // Slap range and cone — green
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, slapRange);
        Vector3 leftEdge = Quaternion.Euler(0, -slapAngle * 0.5f, 0) * transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, slapAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftEdge * slapRange);
        Gizmos.DrawRay(transform.position, rightEdge * slapRange);
    }
}