using UnityEngine;

/// <summary>
/// Place this on a child GameObject � your weapon, hand, or attack pivot point.
/// Requires a BoxCollider on the same GameObject set to Is Trigger = ON.
///
/// Can be triggered two ways:
///   1. ComboRunner.FireHit() � enables for one frame automatically.
///   2. AnimationEventRelay - enables/disables exactly when animation events fire.
///      Use option 2 for precise hit timing synced to your animation frames.
///
/// Setup (ranged, optional): tick isRanged, drag a projectile prefab (needs a
/// WizardProjectiles component) into projectilePrefab, and optionally assign muzzlePoint
/// to wherever it should spawn from (falls back to this transform if left empty). With
/// isRanged on, both trigger methods above spawn a projectile instead of enabling the
/// melee collider - see WizardProjectiles.cs for the projectile side of this.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("Which layer enemies are on. Only objects on this layer take damage.")]
    public LayerMask enemyLayer;

    [HideInInspector]
    public BoxCollider boxCollider;

    /// <summary>
    /// The attacker's StatsManager. Used to apply HealthSteal when a hit lands.
    /// Assign this to the player/enemy's own StatsManager in the Inspector or via ComboRunner.
    /// </summary>
    public StatsManager attackerStats;

    [Header("Ranged (optional)")]
    [Tooltip("If true, FireHit/SetActive(true) spawns a projectile instead of enabling the melee trigger collider.")]
    public bool isRanged;

    [Tooltip("Projectile prefab to spawn — must have a WizardProjectiles component.")]
    public GameObject projectilePrefab;

    [Tooltip("Where the projectile spawns from and which way it's aimed (e.g. the wand tip). Falls back to this transform if left empty.")]
    public Transform muzzlePoint;

    [Tooltip("Speed handed to the spawned projectile.")]
    public float projectileSpeed = 20f;

    /// <summary>Fires with the enemy's StatsManager whenever a hit actually lands (not dodged).
    /// Used by DebuffOnHitTrigger to apply enemy-targeted debuffs only on confirmed hits.</summary>
    public event System.Action<StatsManager> OnEnemyHit;

    /// <summary>Whether the currently-armed hit is the first/last of its combo.
    /// Set by ComboRunner via SetHitContext() at the same time as SetDamage() — i.e. at
    /// swing-start, before ComboRunner's own currentHitIndex advances to the next hit.
    /// Reading ComboRunner.IsFirstHit/IsLastHit live from OnTriggerEnter would be wrong,
    /// since the index has already moved on by the time a trigger can possibly fire.</summary>
    public bool CurrentIsFirstHit { get; private set; }
    public bool CurrentIsLastHit { get; private set; }

    private float currentDamage;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.enabled = false;
    }

    // ------------------------------------------------------------------ Called by ComboRunner

    /// <summary>
    /// Sets the damage value for the next hit.
    /// ComboRunner calls this immediately when a hit is triggered � before
    /// the animation event fires EnableHitbox � so the damage is ready.
    /// </summary>
    public void SetDamage(float damage)
    {
        currentDamage = damage;
    }

    /// <summary>Called by ComboRunner alongside SetDamage(), before the hit actually fires,
    /// so OnTriggerEnter/OnEnemyHit reflect which swing this was.</summary>
    public void SetHitContext(bool isFirstHit, bool isLastHit)
    {
        CurrentIsFirstHit = isFirstHit;
        CurrentIsLastHit = isLastHit;
    }

    /// <summary>
    /// Enables the hitbox for one frame.
    /// Used when not using animation events � ComboRunner calls this after hitCheckDelay.
    /// </summary>
    public void FireHit(float damage)
    {
        currentDamage = damage;

        if (isRanged)
        {
            FireProjectile();
            return;
        }

        boxCollider.enabled = true;

        Invoke(nameof(DisableCollider), Time.fixedDeltaTime);
    }

    // ------------------------------------------------------------------ Called by AnimationEventRelay

    /// <summary>
    /// Directly enable or disable the hitbox.
    /// Called by AnimationEventRelay at the exact animation frame.
    /// Use this instead of FireHit when you have animation events set up.
    /// </summary>
    public void SetActive(bool active)
    {
        if (isRanged)
        {
            if (active)
                FireProjectile();
            return;
        }

        boxCollider.enabled = active;
    }

    private void DisableCollider()
    {
        boxCollider.enabled = false;
    }

    // ------------------------------------------------------------------ Ranged

    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"AttackHitbox on '{name}': isRanged is set but no projectilePrefab assigned.");
            return;
        }

        Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;

        // Use the attacker's facing direction, not spawnPoint.rotation — the weapon mesh's own
        // rotation is tuned for how it looks mid-swing (e.g. the sword's -90* tilt), which has
        // nothing to do with which way the character is actually facing.
        Quaternion launchRotation = attackerStats != null
            ? Quaternion.LookRotation(attackerStats.transform.forward, Vector3.up)
            : spawnPoint.rotation;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPoint.position, launchRotation);

        WizardProjectiles projectile = projectileObj.GetComponent<WizardProjectiles>();
        if (projectile == null)
        {
            Debug.LogWarning($"AttackHitbox on '{name}': projectilePrefab has no WizardProjectiles component.");
            return;
        }

        projectile.Initialize(currentDamage, attackerStats, enemyLayer, projectileSpeed, HandleProjectileHit);
    }

    private void HandleProjectileHit(StatsManager enemyHit)
    {
        OnEnemyHit?.Invoke(enemyHit);
    }

    // ------------------------------------------------------------------ Collision

    private void OnTriggerEnter(Collider other)
    {
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        StatsManager enemyStats = other.GetComponent<StatsManager>()
                               ?? other.GetComponentInParent<StatsManager>();

        if (enemyStats == null) return;

        bool landed = enemyStats.TakeDamage(currentDamage, attackerStats);
        Debug.Log($"AttackHitbox: Hit '{other.name}' for {currentDamage} damage.");

        if (landed)
            OnEnemyHit?.Invoke(enemyStats);
    }
}