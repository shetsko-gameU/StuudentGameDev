using UnityEngine;

/// <summary>
/// Place this on a child GameObject — your weapon, hand, or attack pivot point.
/// ComboRunner calls FireHit() when a hit should land.
///
/// Setup:
///   1. Create a child GameObject on the player (e.g. "WeaponHitbox").
///   2. Add this component to it.
///   3. Set hitRadius and enemyLayer in the Inspector.
///   4. Drag this component into the ComboRunner's Hitbox field.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Hit Settings")]
    [Tooltip("Radius of the hit check sphere. Shown as a yellow gizmo in the Scene view.")]
    public float hitRadius = 1f;

    [Tooltip("Which layer enemies are on. Only objects on this layer take damage.")]
    public LayerMask enemyLayer;

    // ------------------------------------------------------------------ Called by ComboRunner

    /// <summary>
    /// Fires immediately — checks for enemies in range and deals damage.
    /// Called by ComboRunner at the right moment in the combo.
    /// </summary>
    public void FireHit(float damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            StatsManager enemyStats = hit.GetComponent<StatsManager>()
                                   ?? hit.GetComponentInParent<StatsManager>();

            if (enemyStats == null) continue;

            enemyStats.TakeDamage(damage);
            Debug.Log($"AttackHitbox: Hit '{hit.name}' for {damage} damage.");
        }
    }

    // ------------------------------------------------------------------ Gizmo

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, hitRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}