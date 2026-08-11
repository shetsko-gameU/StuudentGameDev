using UnityEngine;

/// <summary>
/// Place this on a child GameObject � your weapon, hand, or attack pivot point.
/// Requires a BoxCollider on the same GameObject set to Is Trigger = ON.
///
/// Can be triggered two ways:
///   1. ComboRunner.FireHit() � enables for one frame automatically.
///   2. AnimationEventRelay � enables/disables exactly when animation events fire.
///      Use option 2 for precise hit timing synced to your animation frames.
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
        boxCollider.enabled = active;
    }

    private void DisableCollider()
    {
        boxCollider.enabled = false;
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