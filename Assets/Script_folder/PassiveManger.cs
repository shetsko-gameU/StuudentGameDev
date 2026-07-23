using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ALL passives on the player.
/// Handles three types:
///
///   PassiveEffectSO    — permanent always-on stat boosts (starting perks, equipment).
///
///   OnHitPassiveSO     — food buffs that fire a TEMPORARY effect every time the player is hit.
///                        Also tracks a permanent stat roll from eating the food.
///
///   FoodStatPassiveSO  — food that gives a BASE STAT BOOST only, no on-hit effect.
///                        "+10 MaxHealth until you eat a better version."
///                        Higher rarity of same family replaces lower rarity.
/// </summary>
public class PassiveManager : MonoBehaviour, IEnumerable<OnHitPassiveSO>
{
    private class FoodPassiveEntry
    {
        public OnHitPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

    // Holds a FoodStatPassiveSO and its rolled stat instance together.
    // The stat instance is the key — it lets us remove the EXACT values when upgrading.
    private class StatBoostEntry
    {
        public FoodStatPassiveSO passive;
        public RolledModifierInstance statRoll;
    }

    // ------------------------------------------------------------------ Inspector

    [Header("References")]
    public StatsManager stats;

    [Header("Always-On Passives")]
    [Tooltip("Permanent stat effects applied once on Start. Drag PassiveEffectSO assets here.")]
    public List<PassiveEffectSO> alwaysOnPassives = new List<PassiveEffectSO>();

    [Header("Starting Food Passives")]
    [Tooltip("OnHitPassiveSOs the character starts with.")]
    public List<OnHitPassiveSO> startingFoodPassives = new List<OnHitPassiveSO>();

    // ------------------------------------------------------------------ Debug (read-only in Inspector)

    [Header("─── Active Passives (Read Only) ───────────────────────")]
    [SerializeField] private List<string> debugAlwaysOnPassives = new List<string>();
    [SerializeField] private List<string> debugFoodPassives = new List<string>();
    [SerializeField] private List<string> debugStatBoosts = new List<string>();

    // ------------------------------------------------------------------ Runtime data

    private readonly List<FoodPassiveEntry> activeFoodEntries = new List<FoodPassiveEntry>();
    private readonly List<StatBoostEntry> activeStatBoosts = new List<StatBoostEntry>();
    private bool subscribed = false;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (stats == null)
            Debug.LogError($"PassiveManager on '{name}': No StatsManager found.");
    }

    private void Start()
    {
        Subscribe();

        foreach (PassiveEffectSO p in alwaysOnPassives)
            if (p != null && p.applyOnStart) ApplyAlwaysOnPassive(p);

        foreach (OnHitPassiveSO p in startingFoodPassives)
            if (p != null) AddFoodPassive(p, null);
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribed || stats == null) return;
        stats.OnDamaged += HandlePlayerDamaged;
        subscribed = true;
        Debug.Log($"PassiveManager on '{name}': Subscribed to OnDamaged.");
    }

    private void Unsubscribe()
    {
        if (!subscribed || stats == null) return;
        stats.OnDamaged -= HandlePlayerDamaged;
        subscribed = false;
    }

    // ------------------------------------------------------------------ Always-on passives (PassiveEffectSO)

    public void ApplyAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null || passive.modifierToApply == null)
            return;

        if (stats == null)
        {
            Debug.LogWarning($"PassiveManager: Cannot apply '{passive.displayName}' — StatsManager missing.");
            return;
        }

        RolledModifierInstance rolled = ModifierRoller.Roll(passive.modifierToApply);
        passive.ActiveRoll = rolled;
        stats.AddRolledModifier(rolled);

        RefreshDebugLists();
        Debug.Log($"PassiveManager: Applied always-on passive '{passive.displayName}'");
    }

    public void RemoveAlwaysOnPassive(PassiveEffectSO passive)
    {
        if (passive == null) return;

        if (passive.ActiveRoll != null && stats != null)
        {
            stats.RemoveRolledInstance(passive.ActiveRoll);
            passive.ActiveRoll = null;
        }

        RefreshDebugLists();
    }

    // ------------------------------------------------------------------ Food passives (OnHitPassiveSO)

    public bool AddFoodPassive(OnHitPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
        {
            Debug.LogWarning("PassiveManager.AddFoodPassive: passive was null.");
            return false;
        }

        FoodPassiveEntry existing = FindFoodEntryByFamily(newPassive);

        if (existing == null)
        {
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugLists();
            Debug.Log($"PassiveManager: Added food passive '{newPassive.displayName}'");
            return true;
        }

        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have '{newPassive.displayName}', ignoring.");
            return false;
        }

        int existingRarity = GetFoodRarityValue(existing.passive);
        int newRarity = GetFoodRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            Debug.Log($"PassiveManager: Upgrading '{existing.passive.displayName}' → '{newPassive.displayName}'");
            RemoveFoodEntry(existing);
            ApplyFoodEntry(newPassive, rolledStats);
            RefreshDebugLists();
            return true;
        }

        Debug.Log($"PassiveManager: Already have equal or higher rarity of '{newPassive.displayName}', ignoring.");
        return false;
    }

    public void RemoveFoodPassive(OnHitPassiveSO passive)
    {
        if (passive == null) return;
        FoodPassiveEntry entry = FindFoodEntry(passive);
        if (entry != null) { RemoveFoodEntry(entry); RefreshDebugLists(); }
    }

    public bool HasFoodPassive(OnHitPassiveSO passive) => FindFoodEntry(passive) != null;
    public int FoodPassiveCount => activeFoodEntries.Count;

    // ------------------------------------------------------------------ Stat boost passives (FoodStatPassiveSO)

    /// <summary>
    /// Adds a stat boost from food and applies it to StatsManager.
    /// Handles family/rarity replacement — higher rarity removes old stats and applies new ones.
    /// Returns true if added or upgraded.
    /// </summary>
    public bool AddStatBoostPassive(FoodStatPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
        {
            Debug.LogWarning("PassiveManager.AddStatBoostPassive: passive was null.");
            return false;
        }

        StatBoostEntry existing = FindStatBoostEntryByFamily(newPassive);

        // No match from the same family — add it fresh
        if (existing == null)
        {
            ApplyStatBoostEntry(newPassive, rolledStats);
            RefreshDebugLists();
            Debug.Log($"PassiveManager: Added stat boost '{newPassive.displayName}'");
            return true;
        }

        // Exact same passive already active — do nothing
        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have '{newPassive.displayName}', ignoring.");
            return false;
        }

        // Rarity lives on statTemplate — Common=0, Rare=1, Epic=2, Legendary=3
        int existingRarity = GetStatBoostRarityValue(existing.passive);
        int newRarity = GetStatBoostRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            // Higher rarity — remove old stats first so they don't stack on top
            Debug.Log($"PassiveManager: Upgrading stat boost '{existing.passive.displayName}' → '{newPassive.displayName}'");
            RemoveStatBoostEntry(existing);
            ApplyStatBoostEntry(newPassive, rolledStats);
            RefreshDebugLists();
            return true;
        }

        Debug.Log($"PassiveManager: Already have equal or higher rarity of '{newPassive.displayName}', ignoring.");
        return false;
    }

    /// <summary>
    /// Removes a stat boost and un-applies its stats from StatsManager.
    /// </summary>
    public void RemoveStatBoostPassive(FoodStatPassiveSO passive)
    {
        if (passive == null) return;
        StatBoostEntry entry = FindStatBoostEntry(passive);
        if (entry != null) { RemoveStatBoostEntry(entry); RefreshDebugLists(); }
    }

    public bool HasStatBoostPassive(FoodStatPassiveSO passive) => FindStatBoostEntry(passive) != null;
    public int StatBoostCount => activeStatBoosts.Count;

    public bool HasStatBoostFamily(string family)
    {
        if (string.IsNullOrEmpty(family)) return false;
        foreach (StatBoostEntry e in activeStatBoosts)
            if (e.passive != null && e.passive.passiveFamily == family) return true;
        return false;
    }

    // ------------------------------------------------------------------ IEnumerable

    public IEnumerator<OnHitPassiveSO> GetEnumerator()
    {
        foreach (FoodPassiveEntry e in activeFoodEntries)
            yield return e.passive;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ------------------------------------------------------------------ Food passive internals

    private void ApplyFoodEntry(OnHitPassiveSO passive, RolledModifierInstance rolledStats)
    {
        activeFoodEntries.Add(new FoodPassiveEntry
        {
            passive = passive,
            permanentStatRoll = rolledStats
        });

        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveFoodEntry(FoodPassiveEntry entry)
    {
        if (entry.permanentStatRoll != null && stats != null)
            stats.RemoveRolledInstance(entry.permanentStatRoll);

        activeFoodEntries.Remove(entry);
    }

    private FoodPassiveEntry FindFoodEntry(OnHitPassiveSO passive)
    {
        foreach (FoodPassiveEntry e in activeFoodEntries)
            if (e.passive == passive) return e;
        return null;
    }

    private FoodPassiveEntry FindFoodEntryByFamily(OnHitPassiveSO passive)
    {
        if (string.IsNullOrEmpty(passive.passiveFamily)) return null;
        foreach (FoodPassiveEntry e in activeFoodEntries)
            if (e.passive != null && e.passive.passiveFamily == passive.passiveFamily) return e;
        return null;
    }

    private int GetFoodRarityValue(OnHitPassiveSO passive)
    {
        return passive.buffTemplate != null ? (int)passive.buffTemplate.rarity : 0;
    }

    // ------------------------------------------------------------------ Stat boost internals

    private void ApplyStatBoostEntry(FoodStatPassiveSO passive, RolledModifierInstance rolledStats)
    {
        activeStatBoosts.Add(new StatBoostEntry
        {
            passive = passive,
            statRoll = rolledStats
        });

        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveStatBoostEntry(StatBoostEntry entry)
    {
        // Remove the EXACT rolled instance — not all modifiers from this source
        if (entry.statRoll != null && stats != null)
            stats.RemoveRolledInstance(entry.statRoll);

        activeStatBoosts.Remove(entry);
    }

    private StatBoostEntry FindStatBoostEntry(FoodStatPassiveSO passive)
    {
        foreach (StatBoostEntry e in activeStatBoosts)
            if (e.passive == passive) return e;
        return null;
    }

    private StatBoostEntry FindStatBoostEntryByFamily(FoodStatPassiveSO passive)
    {
        if (string.IsNullOrEmpty(passive.passiveFamily)) return null;
        foreach (StatBoostEntry e in activeStatBoosts)
            if (e.passive != null && e.passive.passiveFamily == passive.passiveFamily) return e;
        return null;
    }

    private int GetStatBoostRarityValue(FoodStatPassiveSO passive)
    {
        return passive.statTemplate != null ? (int)passive.statTemplate.rarity : 0;
    }

    // ------------------------------------------------------------------ Debug lists

    private void RefreshDebugLists()
    {
        debugAlwaysOnPassives.Clear();
        foreach (PassiveEffectSO p in alwaysOnPassives)
            if (p != null && p.ActiveRoll != null)
                debugAlwaysOnPassives.Add(p.displayName);

        debugFoodPassives.Clear();
        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null) continue;
            string rarity = e.passive.buffTemplate != null
                ? e.passive.buffTemplate.rarity.ToString() : "No Template";
            debugFoodPassives.Add($"{e.passive.displayName}  [{rarity}]");
        }

        debugStatBoosts.Clear();
        foreach (StatBoostEntry e in activeStatBoosts)
        {
            if (e.passive == null) continue;
            string rarity = e.passive.statTemplate != null
                ? e.passive.statTemplate.rarity.ToString() : "No Template";
            debugStatBoosts.Add($"{e.passive.displayName}  [{rarity}]");
        }
    }

    // ------------------------------------------------------------------ On-hit handler

    /// <summary>Fires when the PLAYER takes damage — applies on-hit food buffs and spawns.</summary>
    private void HandlePlayerDamaged(float finalDamage)
    {
        if (stats == null) return;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
            if (e.passive == null) continue;

            // Apply the temporary stat buff if one is set
            if (e.passive.buffTemplate != null)
            {
                RolledModifierInstance onHitRoll = ModifierRoller.Roll(e.passive.buffTemplate);
                onHitRoll.durationSeconds = e.passive.buffDurationSeconds;
                stats.AddRolledModifier(onHitRoll);
            }

            // Spawn the entity if one is set — spawns at the player's position
            if (e.passive.SpawnEntity != null)
            {
                Instantiate(e.passive.SpawnEntity, new Vector3(transform.position.x,transform.position.y+2,transform.position.z), transform.rotation);
            }
        }
    }
}