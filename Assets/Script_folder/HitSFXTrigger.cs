using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner and AttackHitbox.
///
/// Listens to AttackHitbox.OnEnemyHit — which only fires when a swing actually connects
/// and wasn't dodged — and plays a positional impact sound at the enemy that was hit.
/// </summary>
public class HitSFXTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject. hitbox falls back to ComboRunner.hitbox, " +
             "then to a child search, since AttackHitbox usually lives on a child (the weapon).")]
    public ComboRunner comboRunner;
    public AttackHitbox hitbox;

    [Header("Sound")]
    public SoundSO hitSound;

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (hitbox == null && comboRunner != null) hitbox = comboRunner.hitbox;
        if (hitbox == null) hitbox = GetComponentInChildren<AttackHitbox>();

        if (hitbox == null) Debug.LogError($"HitSFXTrigger on '{name}': AttackHitbox missing (checked ComboRunner.hitbox and children).");
    }

    private void OnEnable()
    {
        if (hitbox != null)
            hitbox.OnEnemyHit += HandleEnemyHit;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.OnEnemyHit -= HandleEnemyHit;
    }

    private void HandleEnemyHit(StatsManager enemyHit)
    {
        if (hitSound == null || enemyHit == null) return;
        SoundManager.Instance.PlaySFXAtPoint(hitSound, enemyHit.transform.position);
    }
}
