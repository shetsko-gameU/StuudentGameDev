using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ALL passives on the player.
<<<<<<< HEAD
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
=======
/// Handles five types:
///
///   PassiveEffectSO       — permanent always-on stat boosts (starting perks, equipment).
///
///   OnHitPassiveSO        — food buffs that fire a TEMPORARY effect on the PLAYER every
///                           time the player is hit. Also tracks a permanent stat roll
///                           from eating the food.
///
///   FoodStatPassiveSO     — food that gives a BASE STAT BOOST only, no on-hit effect.
///                           "+10 MaxHealth until you eat a better version."
///                           Higher rarity of same family replaces lower rarity.
///
///   KillPassiveSO         — fires on every confirmed kill (via KillPassiveTrigger).
///
///   DebuffOnHitPassiveSO  — applies a debuff to the ENEMY on every confirmed hit
///                           (via DebuffOnHitTrigger) — the mirror of OnHitPassiveSO.
///
///   UltFoodSO              — equips an activated ability (e.g. SnaghettiAbilitySO) into
///                           AbilityRunner.secondaryAbility. Different from the other five:
///                           targets AbilityRunner, not StatsManager, and only ONE can ever
///                           be equipped — a different ult family always replaces it, the
///                           same family only replaces on strictly higher rarity.
>>>>>>> ScriptBreanchfixs
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

<<<<<<< HEAD
=======
    // Holds a KillPassiveSO and the permanent stat roll granted by EATING the food
    // (separate from the per-kill buffTemplate rolls, which KillPassiveTrigger
    // rolls fresh on every kill and never stores here).
    private class KillPassiveEntry
    {
        public KillPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

    // Holds a DebuffOnHitPassiveSO and the permanent stat roll granted by EATING the food
    // (separate from the per-hit debuffTemplate rolls, which DebuffOnHitTrigger rolls
    // fresh on every confirmed hit and applies to the ENEMY, not tracked here).
    private class DebuffPassiveEntry
    {
        public DebuffOnHitPassiveSO passive;
        public RolledModifierInstance permanentStatRoll;
    }

>>>>>>> ScriptBreanchfixs
    // ------------------------------------------------------------------ Inspector

    [Header("References")]
    public StatsManager stats;

<<<<<<< HEAD
=======
    [Tooltip("Auto-found if on the same GameObject. Required for UltFoodSO to equip abilities.")]
    public AbilityRunner abilityRunner;

>>>>>>> ScriptBreanchfixs
    [Header("Always-On Passives")]
    [Tooltip("Permanent stat effects applied once on Start. Drag PassiveEffectSO assets here.")]
    public List<PassiveEffectSO> alwaysOnPassives = new List<PassiveEffectSO>();

    [Header("Starting Food Passives")]
    [Tooltip("OnHitPassiveSOs the character starts with.")]
    public List<OnHitPassiveSO> startingFoodPassives = new List<OnHitPassiveSO>();

<<<<<<< HEAD
=======
    [Header("Starting Kill Passives")]
    [Tooltip("KillPassiveSOs the character starts with.")]
    public List<KillPassiveSO> startingKillPassives = new List<KillPassiveSO>();

    [Header("Starting Debuff Passives")]
    [Tooltip("DebuffOnHitPassiveSOs the character starts with.")]
    public List<DebuffOnHitPassiveSO> startingDebuffPassives = new List<DebuffOnHitPassiveSO>();

    [Header("Starting Ult Ability")]
    [Tooltip("UltFoodSO the character starts with equipped, if any. Only one can ever be active.")]
    public UltFoodSO startingUltFood;

>>>>>>> ScriptBreanchfixs
    // ------------------------------------------------------------------ Debug (read-only in Inspector)

    [Header("─── Active Passives (Read Only) ───────────────────────")]
    [SerializeField] private List<string> debugAlwaysOnPassives = new List<string>();
    [SerializeField] private List<string> debugFoodPassives = new List<string>();
    [SerializeField] private List<string> debugStatBoosts = new List<string>();
<<<<<<< HEAD
=======
    [SerializeField] private List<string> debugKillPassives = new List<string>();
    [SerializeField] private List<string> debugDebuffPassives = new List<string>();
    [SerializeField] private string debugUltAbility = "(none)";
>>>>>>> ScriptBreanchfixs

    // ------------------------------------------------------------------ Runtime data

    private readonly List<FoodPassiveEntry> activeFoodEntries = new List<FoodPassiveEntry>();
    private readonly List<StatBoostEntry> activeStatBoosts = new List<StatBoostEntry>();
<<<<<<< HEAD
=======
    private readonly List<KillPassiveEntry> activeKillEntries = new List<KillPassiveEntry>();
    private readonly List<DebuffPassiveEntry> activeDebuffEntries = new List<DebuffPassiveEntry>();
    private UltFoodSO activeUltFood;
>>>>>>> ScriptBreanchfixs
    private bool subscribed = false;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (stats == null)
            Debug.LogError($"PassiveManager on '{name}': No StatsManager found.");
<<<<<<< HEAD
=======

        if (abilityRunner == null)
            abilityRunner = GetComponent<AbilityRunner>();

        if (abilityRunner == null)
            Debug.LogWarning($"PassiveManager on '{name}': No AbilityRunner found — ult food won't be able to equip.");
>>>>>>> ScriptBreanchfixs
    }

    private void Start()
    {
        Subscribe();

        foreach (PassiveEffectSO p in alwaysOnPassives)
            if (p != null && p.applyOnStart) ApplyAlwaysOnPassive(p);

        foreach (OnHitPassiveSO p in startingFoodPassives)
            if (p != null) AddFoodPassive(p, null);
<<<<<<< HEAD
=======

        foreach (KillPassiveSO p in startingKillPassives)
            if (p != null) AddKillPassive(p, null);

        foreach (DebuffOnHitPassiveSO p in startingDebuffPassives)
            if (p != null) AddDebuffPassive(p, null);

        if (startingUltFood != null)
            AddUltAbility(startingUltFood);
>>>>>>> ScriptBreanchfixs
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribed || stats == null) return;
<<<<<<< HEAD
        stats.OnDamaged += HandleDamaged;
=======
        stats.OnDamaged += HandlePlayerDamaged;
>>>>>>> ScriptBreanchfixs
        subscribed = true;
        Debug.Log($"PassiveManager on '{name}': Subscribed to OnDamaged.");
    }

    private void Unsubscribe()
    {
        if (!subscribed || stats == null) return;
<<<<<<< HEAD
        stats.OnDamaged -= HandleDamaged;
=======
        stats.OnDamaged -= HandlePlayerDamaged;
>>>>>>> ScriptBreanchfixs
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

<<<<<<< HEAD
=======
    // ------------------------------------------------------------------ Kill passives (KillPassiveSO)

    /// <summary>
    /// Adds a kill passive and applies the permanent stat roll granted by eating the food (if any).
    /// Handles family/rarity replacement — higher rarity removes the old eaten-stat roll first.
    /// The per-kill buffTemplate itself is rolled fresh on every kill by KillPassiveTrigger,
    /// not here — this only tracks which kill passives are currently active.
    /// Returns true if added or upgraded.
    /// </summary>
    public bool AddKillPassive(KillPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
        {
            Debug.LogWarning("PassiveManager.AddKillPassive: passive was null.");
            return false;
        }

        KillPassiveEntry existing = FindKillEntryByFamily(newPassive);

        if (existing == null)
        {
            ApplyKillEntry(newPassive, rolledStats);
            RefreshDebugLists();
            Debug.Log($"PassiveManager: Added kill passive '{newPassive.displayName}'");
            return true;
        }

        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have '{newPassive.displayName}', ignoring.");
            return false;
        }

        int existingRarity = GetKillRarityValue(existing.passive);
        int newRarity = GetKillRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            Debug.Log($"PassiveManager: Upgrading kill passive '{existing.passive.displayName}' → '{newPassive.displayName}'");
            RemoveKillEntry(existing);
            ApplyKillEntry(newPassive, rolledStats);
            RefreshDebugLists();
            return true;
        }

        Debug.Log($"PassiveManager: Already have equal or higher rarity of '{newPassive.displayName}', ignoring.");
        return false;
    }

    public void RemoveKillPassive(KillPassiveSO passive)
    {
        if (passive == null) return;
        KillPassiveEntry entry = FindKillEntry(passive);
        if (entry != null) { RemoveKillEntry(entry); RefreshDebugLists(); }
    }

    public bool HasKillPassive(KillPassiveSO passive) => FindKillEntry(passive) != null;
    public int KillPassiveCount => activeKillEntries.Count;

    /// <summary>Every currently active KillPassiveSO. Used by KillPassiveTrigger on each kill.</summary>
    public IEnumerable<KillPassiveSO> ActiveKillPassives()
    {
        foreach (KillPassiveEntry e in activeKillEntries)
            if (e.passive != null) yield return e.passive;
    }

    // ------------------------------------------------------------------ Debuff passives (DebuffOnHitPassiveSO)

    /// <summary>
    /// Adds a debuff-on-hit passive and applies the permanent stat roll granted by eating the
    /// food (if any). Handles family/rarity replacement — higher rarity removes the old
    /// eaten-stat roll first. The per-hit debuffTemplate itself is rolled fresh on every
    /// confirmed hit by DebuffOnHitTrigger and applied to the ENEMY, not here — this only
    /// tracks which debuff passives are currently active.
    /// Returns true if added or upgraded.
    /// </summary>
    public bool AddDebuffPassive(DebuffOnHitPassiveSO newPassive, RolledModifierInstance rolledStats)
    {
        if (newPassive == null)
        {
            Debug.LogWarning("PassiveManager.AddDebuffPassive: passive was null.");
            return false;
        }

        DebuffPassiveEntry existing = FindDebuffEntryByFamily(newPassive);

        if (existing == null)
        {
            ApplyDebuffEntry(newPassive, rolledStats);
            RefreshDebugLists();
            Debug.Log($"PassiveManager: Added debuff passive '{newPassive.displayName}'");
            return true;
        }

        if (existing.passive == newPassive)
        {
            Debug.Log($"PassiveManager: Already have '{newPassive.displayName}', ignoring.");
            return false;
        }

        int existingRarity = GetDebuffRarityValue(existing.passive);
        int newRarity = GetDebuffRarityValue(newPassive);

        if (newRarity > existingRarity)
        {
            Debug.Log($"PassiveManager: Upgrading debuff passive '{existing.passive.displayName}' → '{newPassive.displayName}'");
            RemoveDebuffEntry(existing);
            ApplyDebuffEntry(newPassive, rolledStats);
            RefreshDebugLists();
            return true;
        }

        Debug.Log($"PassiveManager: Already have equal or higher rarity of '{newPassive.displayName}', ignoring.");
        return false;
    }

    public void RemoveDebuffPassive(DebuffOnHitPassiveSO passive)
    {
        if (passive == null) return;
        DebuffPassiveEntry entry = FindDebuffEntry(passive);
        if (entry != null) { RemoveDebuffEntry(entry); RefreshDebugLists(); }
    }

    public bool HasDebuffPassive(DebuffOnHitPassiveSO passive) => FindDebuffEntry(passive) != null;
    public int DebuffPassiveCount => activeDebuffEntries.Count;

    /// <summary>Every currently active DebuffOnHitPassiveSO. Used by DebuffOnHitTrigger on each confirmed hit.</summary>
    public IEnumerable<DebuffOnHitPassiveSO> ActiveDebuffPassives()
    {
        foreach (DebuffPassiveEntry e in activeDebuffEntries)
            if (e.passive != null) yield return e.passive;
    }

    // ------------------------------------------------------------------ Ult ability (UltFoodSO)

    /// <summary>
    /// Equips an ultimate ability into AbilityRunner.secondaryAbility. Only one ult can ever
    /// be equipped: a food from a DIFFERENT ultFamily always replaces the current one; a food
    /// from the SAME ultFamily only replaces it if strictly higher rarity (no downgrading,
    /// no re-equipping the same rarity). Returns true if the ult was equipped or upgraded.
    /// </summary>
    public bool AddUltAbility(UltFoodSO newUlt)
    {
        if (newUlt == null || newUlt.ability == null)
        {
            Debug.LogWarning("PassiveManager.AddUltAbility: passive or its ability was null.");
            return false;
        }

        if (abilityRunner == null)
        {
            Debug.LogWarning($"PassiveManager: Cannot equip ult '{newUlt.displayName}' — AbilityRunner missing.");
            return false;
        }

        bool sameFamily = activeUltFood != null
                        && !string.IsNullOrEmpty(newUlt.ultFamily)
                        && activeUltFood.ultFamily == newUlt.ultFamily;

        if (sameFamily)
        {
            if (activeUltFood.ability == newUlt.ability)
            {
                Debug.Log($"PassiveManager: Already have '{newUlt.displayName}', ignoring.");
                return false;
            }

            if (newUlt.rarity <= activeUltFood.rarity)
            {
                Debug.Log($"PassiveManager: Already have equal or higher rarity of ult '{newUlt.displayName}', ignoring.");
                return false;
            }

            Debug.Log($"PassiveManager: Upgrading ult '{activeUltFood.displayName}' → '{newUlt.displayName}'");
        }
        else if (activeUltFood != null)
        {
            Debug.Log($"PassiveManager: Replacing ult '{activeUltFood.displayName}' → '{newUlt.displayName}'");
        }
        else
        {
            Debug.Log($"PassiveManager: Equipped ult '{newUlt.displayName}'");
        }

        activeUltFood = newUlt;
        abilityRunner.secondaryAbility.ability = newUlt.ability;
        newUlt.ability.OnEquipped(gameObject); // AbilityRunner only calls this itself on its own Start()

        RefreshDebugLists();
        return true;
    }

    public UltFoodSO ActiveUltFood => activeUltFood;
    public bool HasUltAbility => activeUltFood != null;

>>>>>>> ScriptBreanchfixs
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

<<<<<<< HEAD
=======
    // ------------------------------------------------------------------ Kill passive internals

    private void ApplyKillEntry(KillPassiveSO passive, RolledModifierInstance rolledStats)
    {
        activeKillEntries.Add(new KillPassiveEntry
        {
            passive = passive,
            permanentStatRoll = rolledStats
        });

        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveKillEntry(KillPassiveEntry entry)
    {
        if (entry.permanentStatRoll != null && stats != null)
            stats.RemoveRolledInstance(entry.permanentStatRoll);

        activeKillEntries.Remove(entry);
    }

    private KillPassiveEntry FindKillEntry(KillPassiveSO passive)
    {
        foreach (KillPassiveEntry e in activeKillEntries)
            if (e.passive == passive) return e;
        return null;
    }

    private KillPassiveEntry FindKillEntryByFamily(KillPassiveSO passive)
    {
        if (string.IsNullOrEmpty(passive.passiveFamily)) return null;
        foreach (KillPassiveEntry e in activeKillEntries)
            if (e.passive != null && e.passive.passiveFamily == passive.passiveFamily) return e;
        return null;
    }

    private int GetKillRarityValue(KillPassiveSO passive)
    {
        return passive.buffTemplate != null ? (int)passive.buffTemplate.rarity : 0;
    }

    // ------------------------------------------------------------------ Debuff passive internals

    private void ApplyDebuffEntry(DebuffOnHitPassiveSO passive, RolledModifierInstance rolledStats)
    {
        activeDebuffEntries.Add(new DebuffPassiveEntry
        {
            passive = passive,
            permanentStatRoll = rolledStats
        });

        if (rolledStats != null && stats != null)
            stats.AddRolledModifier(rolledStats);
    }

    private void RemoveDebuffEntry(DebuffPassiveEntry entry)
    {
        if (entry.permanentStatRoll != null && stats != null)
            stats.RemoveRolledInstance(entry.permanentStatRoll);

        activeDebuffEntries.Remove(entry);
    }

    private DebuffPassiveEntry FindDebuffEntry(DebuffOnHitPassiveSO passive)
    {
        foreach (DebuffPassiveEntry e in activeDebuffEntries)
            if (e.passive == passive) return e;
        return null;
    }

    private DebuffPassiveEntry FindDebuffEntryByFamily(DebuffOnHitPassiveSO passive)
    {
        if (string.IsNullOrEmpty(passive.passiveFamily)) return null;
        foreach (DebuffPassiveEntry e in activeDebuffEntries)
            if (e.passive != null && e.passive.passiveFamily == passive.passiveFamily) return e;
        return null;
    }

    private int GetDebuffRarityValue(DebuffOnHitPassiveSO passive)
    {
        return passive.debuffTemplate != null ? (int)passive.debuffTemplate.rarity : 0;
    }

>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
=======

        debugKillPassives.Clear();
        foreach (KillPassiveEntry e in activeKillEntries)
        {
            if (e.passive == null) continue;
            string rarity = e.passive.buffTemplate != null
                ? e.passive.buffTemplate.rarity.ToString() : "No Template";
            debugKillPassives.Add($"{e.passive.displayName}  [{rarity}]");
        }

        debugDebuffPassives.Clear();
        foreach (DebuffPassiveEntry e in activeDebuffEntries)
        {
            if (e.passive == null) continue;
            string rarity = e.passive.debuffTemplate != null
                ? e.passive.debuffTemplate.rarity.ToString() : "No Template";
            debugDebuffPassives.Add($"{e.passive.displayName}  [{rarity}]");
        }

        debugUltAbility = activeUltFood != null
            ? $"{activeUltFood.displayName}  [{activeUltFood.rarity}]"
            : "(none)";
>>>>>>> ScriptBreanchfixs
    }

    // ------------------------------------------------------------------ On-hit handler

<<<<<<< HEAD
    private void HandleDamaged(float finalDamage)
=======
    /// <summary>Fires when the PLAYER takes damage — applies on-hit food buffs and spawns.</summary>
    private void HandlePlayerDamaged(float finalDamage)
>>>>>>> ScriptBreanchfixs
    {
        if (stats == null) return;

        foreach (FoodPassiveEntry e in activeFoodEntries)
        {
<<<<<<< HEAD
            if (e.passive == null || e.passive.buffTemplate == null) continue;

            RolledModifierInstance onHitRoll = ModifierRoller.Roll(e.passive.buffTemplate);
            onHitRoll.durationSeconds = e.passive.buffDurationSeconds;
            stats.AddRolledModifier(onHitRoll);
=======
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
>>>>>>> ScriptBreanchfixs
        }
    }
}