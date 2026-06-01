using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner and PassiveManager.
/// Fires a different passive at the start and end of a combo.
///
/// First hit passive — triggers the moment the combo starts.
/// Last hit passive  — triggers when the final hit of the combo lands.
///
/// Each passive can have a stat buff, a spawned entity, or both.
/// </summary>
public class ComboPassiveTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public ComboRunner comboRunner;
    public PassiveManager passiveManager;
    public StatsManager stats;

    [Header("First Hit Passive")]
    [Tooltip("Fires when the combo starts (first hit lands).")]
    public OnHitPassiveSO firstHitPassive;

    [Header("Last Hit Passive")]
    [Tooltip("Fires when the combo finishes (last hit lands).")]
    public OnHitPassiveSO lastHitPassive;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (stats == null) stats = GetComponent<StatsManager>();

        if (comboRunner == null) Debug.LogError($"ComboPassiveTrigger on '{name}': ComboRunner missing.");
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
        if (firstHitPassive == null) return;

        Debug.Log($"ComboPassiveTrigger: First hit — firing '{firstHitPassive.displayName}'");
        ApplyPassive(firstHitPassive);
    }

    private void HandleLastHit()
    {
        if (lastHitPassive == null) return;

        Debug.Log($"ComboPassiveTrigger: Last hit — firing '{lastHitPassive.displayName}'");
        ApplyPassive(lastHitPassive);
    }

    // ------------------------------------------------------------------ Apply

    /// <summary>
    /// Applies a passive's stat buff and spawns its entity if set.
    /// Same logic as PassiveManager.HandleDamaged but triggered by combo events.
    /// </summary>
    private void ApplyPassive(OnHitPassiveSO passive)
    {
        // Apply the stat buff if one is set
        if (passive.buffTemplate != null && stats != null)
        {
            RolledModifierInstance roll = ModifierRoller.Roll(passive.buffTemplate);
            roll.durationSeconds = passive.buffDurationSeconds;
            stats.AddRolledModifier(roll);
        }

        // Spawn the entity if one is set — spawns slightly above the player
        if (passive.SpawnEntity != null)
        {
            Instantiate(
                passive.SpawnEntity,
                new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z),
                transform.rotation);
        }
    }
}
