using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PassiveManager : MonoBehaviour, IEnumerable<OnHitPassiveSO>
{
    // Holds an OnHitPassiveSO and the permanent stat roll that came with eating the food.
    // Kept together so when we upgrade, we can cleanly remove the old stats.
    private class FoodPassiveEntry
    {
        public OnHitPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

    // ------------------------------------------------------------------ Inspector

    [Header("References")]
    public StatsManager stats;

    [Header("Always-On Passives")]
    [Tooltip("Permanent stat effects applied once on Start. Drag PassiveEffectSO assets here.")]
    public List<PassiveEffectSO> alwaysOnPassives = new List<PassiveEffectSO>();

    [Header("Starting Food Passives")]
    [Tooltip("Food passives the character starts with. Applied automatically on Start.")]
    public List<OnHitPassiveSO> startingFoodPassives = new List<OnHitPassiveSO>();

    // ------------------------------------------------------------------ Debug (read-only in Inspector during play)

    [Header("─── Active Passives (Read Only) ───────────────────────")]
    [Tooltip("Always-on passives currently applied. Updated at runtime.")]
    [SerializeField] private List<string> debugAlwaysOnPassives = new List<string>();

    [Tooltip("Food passives currently active. Updated at runtime.")]
    [SerializeField] private List<string> debugFoodPassives = new List<string>();

    // ------------------------------------------------------------------ Runtime data

    private readonly List<FoodPassiveEntry> activeFoodEntries = new List<FoodPassiveEntry>();
    private bool subscribed = false;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        // Only grab the reference here — do NOT subscribe yet.
        // StatsManager.Awake() may not have run yet at this point.
        // We subscribe in Start(), which Unity guarantees runs after ALL Awake() calls finish.
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (stats == null)
        {
            Debug.LogError($"PassiveManager on '{name}': No StatsManager found. " +
                           "Assign it in the Inspector or add StatsManager to the same GameObject.");
        }
    }

    private void Start()
    {
        // Subscribe here — all Awake() calls are done so stats is guaranteed to exist
        Subscribe();

        // Apply permanent always-on passives (PassiveEffectSO)
        foreach (PassiveEffectSO p in alwaysOnPassives)
        {
            if (p != null && p.applyOnStart)
                ApplyAlwaysOnPassive(p);
        }

        // Apply starting food passives with no permanent stat roll
        foreach (OnHitPassiveSO p in startingFoodPassives)
        {
            if (p != null)
                AddFoodPassive(p, null);
        }
    }

    private void OnEnable()
    {
        // Re-subscribe if the component is toggled back on mid-game
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // ------------------------------------------------------------------ Subscription helpers

    private void Subscribe()
    {
        // Guard flag prevents double-subscribing if OnEnable fires multiple times
        if (subscribed || stats == null) return;
        stats.OnDamaged += HandleDamaged;
        subscribed = true;
        Debug.Log($"PassiveManager on '{name}': Subscribed to OnDamaged.");
    }

    private void Unsubscribe()
    {
        if (!subscribed || stats == null) return;
        stats.OnDamaged -= HandleDamaged;
        subscribed = false;
    }

    // ------------------------------------------------------------------ Always-on passives (PassiveEffectSO)

    /// <summary>
    /// Applies a permanent always-on passive and its stats immediately.
    /// Replaces what PassiveEffectRunner used to do.
    /// </summary>
    public void ApplyAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (stats == null)
        {
            Debug.LogWarning($"PassiveManager: Cannot apply '{passive.displayName}' — StatsManager is missing.");
            return;
        }

        if (passive.modifierToApply == null)
        {
            Debug.LogWarning($"PassiveManager: '{passive.displayName}' has no modifierToApply assigned.");
            return;
        }

        RolledModifierInstance rolled = ModifierRoller.Roll(passive.modifierToApply);
        passive.ActiveRoll = rolled;
        stats.AddRolledModifier(rolled);

        RefreshDebugLists();
        Debug.Log($"PassiveManager: Applied always-on passive '{passive.displayName}'");
    }

    /// <summary>
    /// Removes a permanent always-on passive and un-applies its stats.
    /// </summary>
    public void RemoveAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (passive.ActiveRoll != null && stats != null)
        {
            stats.RemoveRolledInstance(passive.ActiveRoll);
            passive.ActiveRoll = null;
        }

        RefreshDebugLists();
    }

    // ------------------------------------------------------------------ Food passives (OnHitPassiveSO)

    /// <summary>
    /// Adds a food passive and permanently applies its stat roll to the player.
    /// Called by PlayerConsume after the player eats food.
    ///
    /// rolledStats — pass the result of inventory.ConsumeItem() so stats can be removed on upgrade.
    ///               Pass null if the passive has no permanent stat component.
    ///
    /// Returns true if added or upgraded, false if blocked.
    /// </summary>
    public bool AddFoodPassive(OnHitPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
        {
            Debug.LogWarning("PassiveManager.AddFoodPassive: passive was null.");
            return false;
        }

        FoodPassiveEntry existing = FindEntryByFamily(newPassive);

        // No passive from this family yet — add it
        if (existing == null)
        {
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugLists();
            Debug.Log($"PassiveManager: Added food passive '{newPassive.displayName}'");
            return true;
        }

        // Exact same passive already active — do nothing
        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have '{newPassive.displayName}', ignoring.");
            return false;
        }

        // Compare rarities — Common=0, Rare=1, Epic=2, Legendary=3
        int existingRarity = GetRarityValue(existing.passive);
        int newRarity = GetRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            // Higher rarity — remove old passive and its stats, apply new ones
            Debug.Log($"PassiveManager: Upgrading '{existing.passive.displayName}' → '{newPassive.displayName}'");
            RemoveFoodEntry(existing);
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugLists();
            return true;
        }
        else
        {
            // Same or lower rarity — block it
            Debug.Log($"PassiveManager: Already have equal or higher rarity of '{newPassive.displayName}', ignoring.");
            return false;
        }
    }

    /// <summary>
    /// Removes a food passive and un-applies its permanent stats.
    /// </summary>
    public void RemoveFoodPassive(OnHitPassiveSO passive)
    {
        if (passive == null)
            return;

        FoodPassiveEntry entry = FindEntry(passive);
        if (entry != null)
        {
            RemoveFoodEntry(entry);
            RefreshDebugLists();
        }
    }

    /// <summary>Returns true if this exact food passive is currently active.</summary>
    public bool HasFoodPassive(OnHitPassiveSO passive)
    {
        return FindEntry(passive) != null;
    }

    /// <summary>Returns true if any passive from this family is currently active.</summary>
    public bool HasFamily(string family)
    {
        if (string.IsNullOrEmpty(family))
            return false;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive != null && e.passive.passiveFamily == family)
                return true;
        }
        return false;
    }

    public int FoodPassiveCount => activeFoodEntries.Count;

    // ------------------------------------------------------------------ IEnumerable
    // Lets you write: foreach (var p in passiveManager) { ... }

    public IEnumerator<OnHitPassiveSO> GetEnumerator()
    {
        foreach (FoodPassiveEntry e in activeFoodEntries)
            yield return e.passive;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // ------------------------------------------------------------------ Private helpers

    private void ApplyFoodEntry(OnHitPassiveSO passive, RolledModifierInstance rolledStats)
    {
        var entry = new FoodPassiveEntry
        {
            passive = passive,
            permanentStatRoll = rolledStats
        };

        activeFoodEntries.Add(entry);

        // Apply the permanent stat boost now (can be null for starting passives)
        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveFoodEntry(FoodPassiveEntry entry)
    {
        // Remove the permanent stat boost from StatsManager
        if (entry.permanentStatRoll != null && stats != null)
            stats.RemoveRolledInstance(entry.permanentStatRoll);

        activeFoodEntries.Remove(entry);
    }

    private FoodPassiveEntry FindEntry(OnHitPassiveSO passive)
    {
        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == passive)
                return e;
        }
        return null;
    }

    private FoodPassiveEntry FindEntryByFamily(OnHitPassiveSO passive)
    {
        // Empty family = unique passive, never matches anything
        if (string.IsNullOrEmpty(passive.passiveFamily))
            return null;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive != null && e.passive.passiveFamily == passive.passiveFamily)
                return e;
        }
        return null;
    }

    private int GetRarityValue(OnHitPassiveSO passive)
    {
        if (passive.buffTemplate == null)
            return 0;

        return (int)passive.buffTemplate.rarity;
    }

    private void RefreshDebugLists()
    {
        debugAlwaysOnPassives.Clear();
        foreach (PassiveEffectSO p in alwaysOnPassives)
        {
            if (p != null && p.ActiveRoll != null)
                debugAlwaysOnPassives.Add(p.displayName);
        }

        debugFoodPassives.Clear();
        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null)
                continue;

            string rarityLabel = e.passive.buffTemplate != null
                ? e.passive.buffTemplate.rarity.ToString()
                : "No Template";

            bool hasStat = e.permanentStatRoll != null;
            debugFoodPassives.Add($"{e.passive.displayName}  [{rarityLabel}]  Stat: {hasStat}");
        }
    }

    // ------------------------------------------------------------------ On-hit handler

    private void HandleDamaged(float finalDamage)
    {
        if (stats == null)
            return;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null || e.passive.buffTemplate == null)
                continue;

            // Roll a fresh temporary buff each time the player takes damage
            RolledModifierInstance onHitRoll = ModifierRoller.Roll(e.passive.buffTemplate);
            onHitRoll.durationSeconds = e.passive.buffDurationSeconds;
            stats.AddRolledModifier(onHitRoll);
        }
    }
}