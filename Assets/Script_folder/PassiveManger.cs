using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single script that handles ALL passive effects on a character.
///
/// Handles two types of passives:
///
///   1. PassiveEffectSO  — permanent always-on stat boosts.
///      Applied once on Start and stay for the whole run.
///      Example: character trait, equipment perk, room reward.
///
///   2. OnHitPassiveSO   — food buffs tied to combat.
///      Two parts: a permanent stat boost from eating, and a
///      temporary buff that fires every time the player takes damage.
///      Can be upgraded — higher rarity replaces lower rarity of same family.
///
/// Implements IEnumerable so you can loop over food passives from outside:
///   foreach (var p in passiveManager) { ... }
/// </summary>
public class PassiveManager : MonoBehaviour, IEnumerable<OnHitPassiveSO>
{
    // Holds an OnHitPassiveSO and the permanent stat roll that came with it.
    // Kept private — nothing outside needs to touch this directly.
    private class FoodPassiveEntry
    {
        public OnHitPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

    [Header("References")]
    public StatsManager stats;

    [Header("Always-On Passives (from PassiveEffectSO)")]
    [Tooltip("Permanent stat effects applied once on Start. " +
             "These replace what PassiveEffectRunner used to do. " +
             "Drag your PassiveEffectSO assets here.")]
    public List<PassiveEffectSO> alwaysOnPassives = new List<PassiveEffectSO>();

    [Header("Starting Food Passives (from OnHitPassiveSO)")]
    [Tooltip("Food passives the character starts with. " +
             "These fire a temporary buff every time the character takes damage.")]
    public List<OnHitPassiveSO> startingFoodPassives = new List<OnHitPassiveSO>();

    // Live list of food passive entries — private so nothing edits it directly
    private readonly List<FoodPassiveEntry> activeFoodEntries = new List<FoodPassiveEntry>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        // Subscribe in Awake so we never miss a hit event
        if (stats != null)
            stats.OnDamaged += HandleDamaged;
    }

    private void Start()
    {
        // Apply permanent always-on passives (PassiveEffectSO)
        foreach (PassiveEffectSO p in alwaysOnPassives)
        {
            if (p != null && p.applyOnStart)
                ApplyAlwaysOnPassive(p);
        }

        // Apply starting food passives (OnHitPassiveSO) with no stat roll
        foreach (OnHitPassiveSO p in startingFoodPassives)
        {
            if (p != null)
                AddFoodPassive(p, null);
        }
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
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
    /// Applies a permanent always-on passive immediately.
    /// This is what PassiveEffectRunner.ApplyPassive used to do.
    /// Call this at runtime when the player picks up a perk or equipment.
    /// </summary>
    public void ApplyAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (stats == null)
        {
            Debug.LogWarning("PassiveManager: Missing StatsManager.");
            return;
        }

        if (passive.modifierToApply == null)
        {
            Debug.LogWarning($"PassiveManager: {passive.displayName} has no modifierToApply.");
            return;
        }

        RolledModifierInstance rolled = ModifierRoller.Roll(passive.modifierToApply);

        // Store the roll on the SO so it can be removed later if needed
        passive.ActiveRoll = rolled;

        stats.AddRolledModifier(rolled);
    }

    /// <summary>
    /// Removes a permanent always-on passive and un-applies its stats.
    /// Use this if the player loses a perk or unequips something.
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
    /// Adds a food passive and permanently applies its stat roll to the player.
    /// Called by PlayerConsume after the player eats food.
    ///
    /// Pass the rolledStats from ConsumeItem so the stats can be removed if upgraded.
    /// Pass null for rolledStats if the passive has no permanent stat component.
    ///
    /// Returns true if added or upgraded, false if blocked.
    /// </summary>
    public bool AddFoodPassive(OnHitPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
            return false;

        FoodPassiveEntry existing = FindEntryByFamily(newPassive);

        // No match from the same family — add it fresh
        if (existing == null)
        {
            ApplyFoodEntry(newPassive, rolledStats);
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
            // Higher rarity — remove old passive and stats, apply new ones
            Debug.Log($"PassiveManager: Upgrading {existing.passive.displayName} → {newPassive.displayName}");
            RemoveFoodEntry(existing);
            ApplyFoodEntry(newPassive, rolledStats);
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
    /// Removes a food passive and un-applies its permanent stat roll.
    /// </summary>
    public void RemoveFoodPassive(OnHitPassiveSO passive)
    {
        if (passive == null)
            return;

        FoodPassiveEntry entry = FindEntry(passive);
        if (entry != null)
            RemoveFoodEntry(entry);
    }

    /// <summary>
    /// Returns true if this exact food passive is currently active.
    /// </summary>
    public bool HasFoodPassive(OnHitPassiveSO passive)
    {
        return FindEntry(passive) != null;
    }

    /// <summary>
    /// Returns true if any food passive from this family is currently active.
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
    // Lets you do: foreach (var p in passiveManager) { ... }

    public IEnumerator<OnHitPassiveSO> GetEnumerator()
    {
        foreach (FoodPassiveEntry e in activeFoodEntries)
            yield return e.passive;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // ------------------------------------------------------------------ Internals

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

    // ------------------------------------------------------------------ On-hit handler

    private void HandleDamaged(float finalDamage)
    {
        if (stats == null)
            return;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null || e.passive.buffTemplate == null)
                continue;

            // Roll and apply a fresh temporary buff every time the player is hit
            RolledModifierInstance onHitRoll = ModifierRoller.Roll(e.passive.buffTemplate);
            onHitRoll.durationSeconds = e.passive.buffDurationSeconds;
            stats.AddRolledModifier(onHitRoll);
        }
    }
}