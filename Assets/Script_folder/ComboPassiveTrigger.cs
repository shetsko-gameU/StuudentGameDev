using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner and PassiveManager.
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

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (stats == null) stats = GetComponent<StatsManager>();

        if (comboRunner == null) Debug.LogError($"ComboPassiveTrigger on '{name}': ComboRunner missing.");
        if (passiveManager == null) Debug.LogError($"ComboPassiveTrigger on '{name}': PassiveManager missing.");
        if (stats == null) Debug.LogError($"ComboPassiveTrigger on '{name}': StatsManager missing.");
    }

    private void OnEnable()
    {
        if (comboRunner == null) return;
        comboRunner.OnComboStarted += HandleFirstHit;
        comboRunner.OnComboFinished += HandleLastHit;
    }

    private void OnDisable()
    {
        if (comboRunner == null) return;
        comboRunner.OnComboStarted -= HandleFirstHit;
        comboRunner.OnComboFinished -= HandleLastHit;
    }

    // ------------------------------------------------------------------ Handlers

    private void HandleFirstHit()
    {
        if (passiveManager == null) return;

        // Loop through every active food passive in PassiveManager
        // PassiveManager implements IEnumerable so foreach works directly on it
        foreach (OnHitPassiveSO passive in passiveManager)
        {
            if (passive == null) continue;

            // Only fire passives that have triggerOnFirstHit ticked
            if (passive.triggerOnFirstHit)
            {
                Debug.Log($"ComboPassiveTrigger: First hit — firing '{passive.displayName}'");
                ApplyPassive(passive);
            }
        }
    }

    private void HandleLastHit()
    {
        if (passiveManager == null) return;

        foreach (OnHitPassiveSO passive in passiveManager)
        {
            if (passive == null) continue;

            // Only fire passives that have triggerOnLastHit ticked
            if (passive.triggerOnLastHit)
            {
                Debug.Log($"ComboPassiveTrigger: Last hit — firing '{passive.displayName}'");
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