using System;
using UnityEngine;

/// <summary>
/// Place this on a child GameObject — your weapon, hand, or attack pivot point.
/// Requires a BoxCollider on the same GameObject set to Is Trigger = ON.
///
/// Animation events (via AnimationEventRelay) call SetActive(true/false) to control
/// exactly which frames the hitbox is live, synced to the sword swing.
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

    /// <summary>
    /// The ComboRunner on the player root. Used to read IsFirstHit / IsLastHit
    /// so passives know which hit type connected.
    /// </summary>
    public ComboRunner comboRunner;

    /// <summary>
    /// Fires when the hitbox actually touches an enemy.
    /// bool isFirstHit, bool isLastHit.
    /// ComboPassiveTrigger subscribes to this to fire passives only on real contact.
    /// </summary>
    public event Action<bool, bool> OnEnemyHit;

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

        enemyStats.TakeDamage(currentDamage, attackerStats);
        Debug.Log($"AttackHitbox: Hit '{other.name}' for {currentDamage} damage.");

        // Notify listeners (e.g. ComboPassiveTrigger) that a hit actually landed.
        // Passives only fire on real enemy contact, not on a miss.
        bool isFirst = comboRunner != null && comboRunner.IsFirstHit;
        bool isLast  = comboRunner != null && comboRunner.IsLastHit;
        OnEnemyHit?.Invoke(isFirst, isLast);
    }
}