using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner and PassiveManager.
///
/// Listens to AttackHitbox.OnEnemyHit — which only fires when a swing actually connects
/// and wasn't dodged — then checks AttackHitbox.CurrentIsFirstHit/CurrentIsLastHit (set by
/// ComboRunner at swing-start) to fire whatever passives are flagged for that hit type.
///
/// Instead of hardcoded passive lists, this reads whatever passives are currently
/// active in PassiveManager and fires the ones flagged for combo triggers.
///
/// To make a food passive trigger on a combo hit:
///   Open the OnHitPassiveSO asset → tick triggerOnFirstHit or triggerOnLastHit.
///
/// This means eating food dynamically unlocks combo effects.
/// No changes needed here when new food passives are added.
/// </summary>
public class ComboPassiveTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public ComboRunner comboRunner;
    public PassiveManager passiveManager;
    public StatsManager stats;

    [Tooltip("Auto-found from ComboRunner.hitbox, then from a child search, since AttackHitbox usually lives on a child (the weapon).")]
    public AttackHitbox hitbox;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (hitbox == null && comboRunner != null) hitbox = comboRunner.hitbox;
        if (hitbox == null) hitbox = GetComponentInChildren<AttackHitbox>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (stats == null) stats = GetComponent<StatsManager>();

        if (comboRunner == null) Debug.LogError($"ComboPassiveTrigger on '{name}': ComboRunner missing.");
        if (hitbox == null) Debug.LogError($"ComboPassiveTrigger on '{name}': AttackHitbox missing (checked ComboRunner.hitbox and children).");
        if (passiveManager == null) Debug.LogError($"ComboPassiveTrigger on '{name}': PassiveManager missing.");
        if (stats == null) Debug.LogError($"ComboPassiveTrigger on '{name}': StatsManager missing.");
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

    // ------------------------------------------------------------------ Handler

    private void HandleEnemyHit(StatsManager enemyHit)
    {
        if (passiveManager == null || hitbox == null) return;

        bool isFirst = hitbox.CurrentIsFirstHit;
        bool isLast = hitbox.CurrentIsLastHit;

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
        // Apply the stat buff if one is set
        if (passive.buffTemplate != null && stats != null)
        {
            RolledModifierInstance roll = ModifierRoller.Roll(passive.buffTemplate);
            roll.durationSeconds = passive.buffDurationSeconds;
            stats.AddRolledModifier(roll);
        }

        // Spawn the entity if one is set
        if (passive.SpawnEntity != null)
        {
            Instantiate(
                passive.SpawnEntity,
                new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z),
                transform.rotation);
        }
    }
}
