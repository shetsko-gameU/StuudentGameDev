using UnityEngine;

/// <summary>
/// Place this on a child GameObject — your weapon, hand, or attack pivot point.
/// Requires a BoxCollider on the same GameObject set to Is Trigger = ON.
///
/// Setup:
///   1. Create a child GameObject on the player (e.g. "WeaponHitbox").
///   2. Add a BoxCollider — set Is Trigger = ON, use Edit Collider to size it.
///   3. Add this AttackHitbox component.
///   4. Set enemyLayer in the Inspector.
///   5. Drag this component into the ComboRunner's Hitbox field.
///
/// The collider stays OFF outside of attacks so you can't accidentally
/// hit enemies just by walking near them.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("Which layer enemies are on. Only objects on this layer take damage.")]
    public LayerMask enemyLayer;

    private BoxCollider boxCollider;
    private float currentDamage;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        // Start disabled — ComboRunner enables it when a hit should land
        boxCollider.enabled = false;
    }

    // ------------------------------------------------------------------ Called by ComboRunner

    /// <summary>
    /// Enables the collider for one frame so it can detect enemies.
    /// ComboRunner calls this at the right moment in the combo.
    /// </summary>
    public void FireHit(float damage)
    {
        currentDamage = damage;
        boxCollider.enabled = true;

        // Disable again next frame so it only hits once per swing
        Invoke(nameof(DisableCollider), Time.fixedDeltaTime);
    }

    private void DisableCollider()
    {
        boxCollider.enabled = false;
    }

    // ------------------------------------------------------------------ Collision

    private void OnTriggerEnter(Collider other)
    {
        // Only damage objects on the enemy layer
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        StatsManager enemyStats = other.GetComponent<StatsManager>()
                               ?? other.GetComponentInParent<StatsManager>();

        if (enemyStats == null) return;

        enemyStats.TakeDamage(currentDamage);
        Debug.Log($"AttackHitbox: Hit '{other.name}' for {currentDamage} damage.");
    }
}