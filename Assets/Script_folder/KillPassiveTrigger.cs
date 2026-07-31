using UnityEngine;

/// <summary>
/// Add to the player alongside PassiveManager and StatsManager.
///
/// Listens to the global StatsManager.OnAnyDied event and, when the killer is this
/// player, rolls and applies every active KillPassiveSO's buffTemplate.
///
/// Unlike ComboPassiveTrigger/OnHitPassiveSO, there is no separate "duration override" —
/// KillPassiveSO.buffTemplate's own durationSeconds is used as-is (0 = permanent).
/// Combine with canStack + maxStacks on the template's stat line to get
/// "gain a stack of X for each enemy killed" that lasts the rest of the run.
///
/// To make a food grant a kill passive: create a KillPassiveSO asset and link it
/// through PlayerConsume's Food → Kill Passive list (or PassiveManager.startingKillPassives
/// for testing). Every KillPassiveSO active in PassiveManager fires on every kill —
/// there's no per-asset toggle, since being a KillPassiveSO at all IS the trigger.
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

        foreach (KillPassiveSO passive in passiveManager.ActiveKillPassives())
        {
            if (passive == null) continue;

            Debug.Log($"KillPassiveTrigger: Kill confirmed — firing '{passive.displayName}'");

            if (passive.buffTemplate != null)
            {
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
