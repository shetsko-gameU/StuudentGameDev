using UnityEngine;

/// <summary>
/// Add to the player alongside PassiveManager and StatsManager.
///
/// Listens to the global StatsManager.OnAnyDied event and, when the killer is this
/// player, fires every active food passive flagged with triggerOnKill.
///
/// Unlike ComboPassiveTrigger, this path does NOT force buffDurationSeconds onto
/// the roll — the buffTemplate's own durationSeconds is used, so 0 = permanent.
/// Combine with canStack + maxStacks on the template's stat line to get
/// "gain a stack of X for each enemy killed" that lasts the rest of the run.
///
/// To make a food passive trigger on kills:
///   1. On the OnHitPassiveSO asset → tick triggerOnKill.
///   2. On its buffTemplate StatsModifierSO → tick canStack on the stat line,
///      set maxStacks (0/1 = uncapped), leave durationSeconds at 0.
/// </summary>
public class KillPassiveTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public PassiveManager passiveManager;
    public StatsManager stats;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (stats == null) stats = GetComponent<StatsManager>();

        if (passiveManager == null) Debug.LogError($"KillPassiveTrigger on '{name}': PassiveManager missing.");
        if (stats == null) Debug.LogError($"KillPassiveTrigger on '{name}': StatsManager missing.");
    }

    // OnAnyDied is STATIC — it outlives this object and the scene. Unsubscribing in
    // OnDisable is what keeps a destroyed/dead player from being called back.
    private void OnEnable() => StatsManager.OnAnyDied += HandleAnyDied;
    private void OnDisable() => StatsManager.OnAnyDied -= HandleAnyDied;

    // ------------------------------------------------------------------ Handler

    private void HandleAnyDied(StatsManager victim, StatsManager killer)
    {
        // Only our own kills count — killer is null for unattributed damage.
        // victim == stats is the player dying; that must never proc kill passives.
        if (killer != stats || victim == stats) return;
        if (passiveManager == null || stats == null) return;

        foreach (OnHitPassiveSO passive in passiveManager)
        {
            if (passive == null || !passive.triggerOnKill) continue;

            Debug.Log($"KillPassiveTrigger: Kill confirmed — firing '{passive.displayName}'");

            if (passive.buffTemplate != null)
            {
                // No buffDurationSeconds override here — the template's own
                // durationSeconds decides (0 = permanent for the run).
                RolledModifierInstance roll = ModifierRoller.Roll(passive.buffTemplate);
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
}
