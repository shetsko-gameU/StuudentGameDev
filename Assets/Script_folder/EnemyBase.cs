using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public StatsManager stats;
    public StatsManager playerStats;
    public Animator animator;

    public void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerAttack")) return;
        if (playerStats == null) return;

        // Use TakeDamage so defense reduction, dodge rolls, OnDied event,
        // and health bar updates all fire correctly.
        // OnDied firing is what triggers LootDropper to spawn items.
        stats.TakeDamage(playerStats.GetDamageRoll());
    }

    public void OnAttack()
    {
        animator.SetTrigger("Attack");
    }
}
