using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveManager : MonoBehaviour, IEnumerable<OnHitPassiveSO>
{
    // Holds a food passive and the permanent stat roll that came with it together.
    // Private — use AddFoodPassive / RemoveFoodPassive from outside.
    private class FoodPassiveEntry
    {
        public OnHitPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

    // ------------------------------------------------------------------ Inspector fields

    [Header("References")]
    public StatsManager stats;

    [Header("Always-On Passives")]
    [Tooltip("Permanent stat boosts applied once on Start. Drag PassiveEffectSO assets here.")]
    public List<PassiveEffectSO> alwaysOnPassives = new List<PassiveEffectSO>();

    [Header("Starting Food Passives")]
    [Tooltip("Food passives the character starts with at the beginning of the run.")]
    public List<OnHitPassiveSO> startingFoodPassives = new List<OnHitPassiveSO>();

    [Header("Debug — Active Food Passives (read only)")]
    [Tooltip("Shows which food passives are currently active. Updated automatically at runtime.")]
    [SerializeField] private List<string> debugActiveFoodPassives = new List<string>();
    // This list only exists so you can see what's active in the Inspector.
    // The real data lives in activeFoodEntries below.

    // ------------------------------------------------------------------ Runtime data

    // The real live list — private so nothing edits it directly from outside
    private readonly List<FoodPassiveEntry> activeFoodEntries = new List<FoodPassiveEntry>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        // Only assign the reference here — do NOT subscribe to events in Awake.
        // OnEnable runs immediately after Awake on first activation,
        // so subscribing in both Awake AND OnEnable would double-fire every hit.
        if (stats == null)
            stats = GetComponent<StatsManager>();
    }

    private void Start()
    {
        // Apply permanent always-on passives (PassiveEffectSO)
        foreach (PassiveEffectSO p in alwaysOnPassives)
        {
            if (p != null && p.applyOnStart)
                ApplyAlwaysOnPassive(p);
        }

        // Apply starting food passives with no stat roll
        foreach (OnHitPassiveSO p in startingFoodPassives)
        {
            if (p != null)
                AddFoodPassive(p, null);
        }
    }

    private void OnEnable()
    {
        // Subscribe here — this is the single place events are registered
        if (stats != null)
            stats.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        // Always unsubscribe when disabled so we don't get ghost calls
        if (stats != null)
            stats.OnDamaged -= HandleDamaged;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnDamaged -= HandleDamaged;
    }

    // ------------------------------------------------------------------ Always-on passives (PassiveEffectSO)

    /// <summary>
    /// Applies a permanent always-on passive and applies its stats immediately.
    /// Replaces what PassiveEffectRunner used to do.
    /// Call this when the player picks up a perk or equips an item.
    /// </summary>
    public void ApplyAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (stats == null)
        {
            Debug.LogWarning("PassiveManager: StatsManager is missing.");
            return;
        }

        if (passive.modifierToApply == null)
        {
            Debug.LogWarning($"PassiveManager: {passive.displayName} has no modifierToApply assigned.");
            return;
        }

        RolledModifierInstance rolled = ModifierRoller.Roll(passive.modifierToApply);

        // Store the roll on the SO so it can be cleanly removed later
        passive.ActiveRoll = rolled;

        stats.AddRolledModifier(rolled);
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
    }

    // ------------------------------------------------------------------ Food passives (OnHitPassiveSO)

    /// <summary>
    /// Adds a food passive and applies its permanent stat roll to the player.
    /// Called by PlayerConsume after the player eats food.
    ///
    /// rolledStats  — pass the result of inventory.ConsumeItem() so stats can be removed on upgrade.
    ///                Pass null if the passive has no permanent stat component.
    ///
    /// Returns true if added or upgraded, false if blocked.
    /// </summary>
    public bool AddFoodPassive(OnHitPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
            return false;

        FoodPassiveEntry existing = FindEntryByFamily(newPassive);

        // No passive from this family yet — add it
        if (existing == null)
        {
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugList();
            return true;
        }

        // Exact same passive already active — do nothing
        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have {newPassive.displayName}, ignoring.");
            return false;
        }

        // Compare rarities — Common=0, Rare=1, Epic=2, Legendary=3
        int existingRarity = GetRarityValue(existing.passive);
        int newRarity = GetRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            // Higher rarity — remove old passive and its stats, apply new ones
            Debug.Log($"PassiveManager: Upgrading {existing.passive.displayName} to {newPassive.displayName}");
            RemoveFoodEntry(existing);
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugList();
            return true;
        }
        else
        {
            // Same or lower rarity — block it
            Debug.Log($"PassiveManager: Already have equal or higher rarity of {newPassive.displayName}, ignoring.");
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
            RefreshDebugList();
        }
    }

    /// <summary>
    /// Returns true if this exact food passive is currently active.
    /// </summary>
    public bool HasFoodPassive(OnHitPassiveSO passive)
    {
        return FindEntry(passive) != null;
    }

    /// <summary>
    /// Returns true if any passive from this family is currently active.
    /// </summary>
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

        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveFoodEntry(FoodPassiveEntry entry)
    {
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
        // If this passive has no family set, treat it as unique — never matches anything
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

    /// <summary>
    /// Rebuilds the debug list so the Inspector stays in sync with activeFoodEntries.
    /// Called every time a passive is added or removed.
    /// </summary>
    private void RefreshDebugList()
    {
        debugActiveFoodPassives.Clear();

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null)
                continue;

            // Show name and rarity so you can tell which version is active
            string rarityLabel = e.passive.buffTemplate != null
                ? e.passive.buffTemplate.rarity.ToString()
                : "No Template";

            debugActiveFoodPassives.Add($"{e.passive.displayName}  [{rarityLabel}]");
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