using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner and PassiveManager.
///
/// Listens to AttackHitbox.OnEnemyHit — passives only fire when the weapon
/// actually connects with an enemy, not on a miss.
///
/// To make a food passive trigger on a combo hit:
///   Open the OnHitPassiveSO asset → tick triggerOnFirstHit or triggerOnLastHit.
/// </summary>
public class ComboPassiveTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public ComboRunner comboRunner;
    public PassiveManager passiveManager;
    public StatsManager stats;

    [Tooltip("The AttackHitbox on the weapon. Assign in Inspector.")]
    public AttackHitbox hitbox;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (stats == null) stats = GetComponent<StatsManager>();

        // Fall back to the hitbox already assigned on ComboRunner
        if (hitbox == null && comboRunner != null) hitbox = comboRunner.hitbox;

        if (comboRunner == null)   Debug.LogError($"ComboPassiveTrigger on '{name}': ComboRunner missing.");
        if (passiveManager == null) Debug.LogError($"ComboPassiveTrigger on '{name}': PassiveManager missing.");
        if (stats == null)          Debug.LogError($"ComboPassiveTrigger on '{name}': StatsManager missing.");
        if (hitbox == null)         Debug.LogError($"ComboPassiveTrigger on '{name}': AttackHitbox missing — assign it or assign it on ComboRunner.");
    }

    private void OnEnable()
    {
        if (hitbox != null) hitbox.OnEnemyHit += HandleEnemyHit;
    }

    private void OnDisable()
    {
        if (hitbox != null) hitbox.OnEnemyHit -= HandleEnemyHit;
    }

    // ------------------------------------------------------------------ Handler

    /// <summary>
    /// Called by AttackHitbox when the weapon touches an enemy.
    /// isFirst / isLast reflect which hit in the combo this was.
    /// </summary>
    private void HandleEnemyHit(bool isFirst, bool isLast)
    {
        if (passiveManager == null) return;

        foreach (OnHitPassiveSO passive in passiveManager)
        {
            if (passive == null) continue;

            if (isFirst && passive.triggerOnFirstHit)
            {
                Debug.Log($"ComboPassiveTrigger: First hit connected — firing '{passive.displayName}'");
                ApplyPassive(passive);
            }
            else if (isLast && passive.triggerOnLastHit)
            {
                Debug.Log($"ComboPassiveTrigger: Last hit connected — firing '{passive.displayName}'");
                ApplyPassive(passive);
            }
        }
    }

    // ------------------------------------------------------------------ Apply

    private void ApplyPassive(OnHitPassiveSO passive)
    {
        if (passive.buffTemplate != null && stats != null)
        {
            RolledModifierInstance roll = ModifierRoller.Roll(passive.buffTemplate);
            roll.durationSeconds = passive.buffDurationSeconds;
            stats.AddRolledModifier(roll);
        }

        if (passive.SpawnEntity != null)
        {
            Instantiate(
                passive.SpawnEntity,
                new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z),
                transform.rotation);
        }
    }
}
