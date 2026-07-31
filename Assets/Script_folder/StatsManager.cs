using System;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [Header("Source (ScriptableObject)")]
    [SerializeField] private BaseStatsSO baseStats;

    [Header("Runtime Health")]
    [SerializeField] public float currentHealth;

    // Base copies (never change after load)
<<<<<<< HEAD
    private float baseMaxHealth, baseAttack, baseDefense, baseMoveSpeed, baseAttackSpeed, baseDodgeChance;
=======
    private float baseMaxHealth, baseAttack, baseDefense, baseMoveSpeed, baseAttackSpeed, baseDodgeChance, baseHealthSteal;
>>>>>>> ScriptBreanchfixs

    // Final runtime values (recalculated whenever a modifier is added or removed)
    public float MaxHealth { get; private set; }
    public float Attack { get; private set; }
    public float Defense { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    public float DodgeChance { get; private set; }
<<<<<<< HEAD
=======
    /// <summary>Fraction of damage dealt restored as health. 0.20 = heals 20% of each hit.</summary>
    public float HealthSteal { get; private set; }
>>>>>>> ScriptBreanchfixs

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDied;
    public event Action<float> OnDamaged;       // fires AFTER damage is applied and confirmed
<<<<<<< HEAD
=======

    /// <summary>
    /// Fires when ANY entity dies: (victim, killer). Killer is null when the damage
    /// source didn't pass an attacker into TakeDamage. Static so player-side systems
    /// (e.g. KillPassiveTrigger) can hear about enemy deaths without holding a
    /// reference to every enemy. Static events outlive scene loads — subscribers
    /// MUST unsubscribe in OnDisable/OnDestroy.
    /// </summary>
    public static event Action<StatsManager, StatsManager> OnAnyDied;
>>>>>>> ScriptBreanchfixs

    private class ActiveRolled
    {
        public RolledModifierInstance inst;
        public float timeRemaining; // <0 means permanent
        public int stacks = 1;
    }

    private readonly List<ActiveRolled> active = new();

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        LoadFromSO(baseStats, setHealthToFull: true);
    }

    private void Update()
    {
        bool changed = false;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var m = active[i];
            if (m.inst.durationSeconds <= 0f) continue;

            m.timeRemaining -= Time.deltaTime;
            if (m.timeRemaining <= 0f)
            {
                active.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            RecalculateFinalStats(keepHealthPercent: true);
    }

    // ------------------------------------------------------------------ Load

    public void LoadFromSO(BaseStatsSO stats, bool setHealthToFull)
    {
        if (stats == null)
        {
            Debug.LogError($"{name}: StatsManager missing BaseStatsSO");
            enabled = false;
            return;
        }

        baseStats = stats;

<<<<<<< HEAD
        baseMaxHealth = Mathf.Max(1f, stats.maxHealth);
        baseAttack = Mathf.Max(0f, stats.attack);
        baseDefense = Mathf.Max(0f, stats.defense);
        baseMoveSpeed = Mathf.Max(0f, stats.moveSpeed);
        baseAttackSpeed = Mathf.Max(0.01f, stats.attackSpeed);
        baseDodgeChance = Mathf.Clamp01(stats.dodgeChance);
=======
        baseMaxHealth    = Mathf.Max(1f,    stats.maxHealth);
        baseAttack       = Mathf.Max(0f,    stats.attack);
        baseDefense      = Mathf.Max(0f,    stats.defense);
        baseMoveSpeed    = Mathf.Max(0f,    stats.moveSpeed);
        baseAttackSpeed  = Mathf.Max(0.01f, stats.attackSpeed);
        baseDodgeChance  = Mathf.Clamp01(   stats.dodgeChance);
        baseHealthSteal  = Mathf.Clamp01(   stats.healthSteal);
>>>>>>> ScriptBreanchfixs

        active.Clear();
        RecalculateFinalStats(keepHealthPercent: false);

        currentHealth = setHealthToFull ? MaxHealth : Mathf.Clamp(currentHealth, 0f, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // ------------------------------------------------------------------ Modifiers

    public void AddRolledModifier(RolledModifierInstance inst)
    {
        if (inst == null || inst.source == null) return;

        bool canStack = AnyLineStackable(inst.source);
        var existing = active.Find(x => x.inst.source == inst.source);

        if (existing != null && canStack)
        {
            existing.stacks = ApplyStackCaps(existing.stacks + 1, inst.source);

            if (inst.durationSeconds > 0f)
                existing.timeRemaining = inst.durationSeconds;
        }
        else if (existing != null && !canStack)
        {
            // Same non-stacking source re-applied while still active (e.g. a debuff that
            // keeps landing on repeated hits). Replace with the fresh roll and reset the
            // clock instead of adding a duplicate entry — a duplicate would double the
            // effect every RecalculateFinalStats pass, not just extend its duration.
            existing.inst = inst;
            existing.timeRemaining = inst.durationSeconds > 0f ? inst.durationSeconds : -1f;
        }
        else
        {
            active.Add(new ActiveRolled
            {
                inst = inst,
                timeRemaining = inst.durationSeconds > 0f ? inst.durationSeconds : -1f,
                stacks = 1
            });
        }

        RecalculateFinalStats(keepHealthPercent: true);
    }

    /// <summary>
    /// Removes the modifier from a specific rolled instance (e.g. unequipping an item).
    /// </summary>
    public void RemoveRolledInstance(RolledModifierInstance inst)
    {
        if (inst == null) return;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].inst == inst)
            {
                active.RemoveAt(i);
                RecalculateFinalStats(keepHealthPercent: true);
                return;
            }
        }
    }

    /// <summary>
    /// Removes ALL modifiers that came from a given SO.
    /// </summary>
    public void RemoveAllFromSource(StatsModifierSO source)
    {
        if (source == null) return;

        bool changed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].inst != null && active[i].inst.source == source)
            {
                active.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            RecalculateFinalStats(keepHealthPercent: true);
    }

    // ------------------------------------------------------------------ Internals

    private bool AnyLineStackable(StatsModifierSO so)
    {
        foreach (var line in so.lines)
            if (line.canStack) return true;
        return false;
    }

    private int ApplyStackCaps(int stacks, StatsModifierSO so)
    {
        int cap = 0;
        foreach (var line in so.lines)
        {
            if (!line.canStack) continue;
            if (line.maxStacks > cap) cap = line.maxStacks;
        }
        return (cap <= 1) ? stacks : Mathf.Min(stacks, cap);
    }

    private void RecalculateFinalStats(bool keepHealthPercent)
    {
        float healthPct = MaxHealth > 0f ? currentHealth / MaxHealth : 1f;

<<<<<<< HEAD
        float maxHealthFlat = 0, attackFlat = 0, defenseFlat = 0, moveSpeedFlat = 0, attackSpeedFlat = 0, dodgeFlat = 0;
        float maxHealthPct = 0, attackPct = 0, defensePct = 0, moveSpeedPct = 0, attackSpeedPct = 0, dodgePct = 0;
=======
        float maxHealthFlat = 0, attackFlat = 0, defenseFlat = 0, moveSpeedFlat = 0, attackSpeedFlat = 0, dodgeFlat = 0, healthStealFlat = 0;
        float maxHealthPct  = 0, attackPct  = 0, defensePct  = 0, moveSpeedPct  = 0, attackSpeedPct  = 0, dodgePct  = 0, healthStealPct  = 0;
>>>>>>> ScriptBreanchfixs

        foreach (var a in active)
        {
            int stacks = Mathf.Max(1, a.stacks);

            foreach (var kvp in a.inst.stackableValues)
            {
                var (stat, mode) = kvp.Key;
                float applied = kvp.Value * stacks;
                ApplyStat(stat, mode, applied,
                    ref maxHealthFlat, ref maxHealthPct,
<<<<<<< HEAD
                    ref attackFlat, ref attackPct,
                    ref defenseFlat, ref defensePct,
                    ref moveSpeedFlat, ref moveSpeedPct,
                    ref attackSpeedFlat, ref attackSpeedPct,
                    ref dodgeFlat, ref dodgePct);
=======
                    ref attackFlat,    ref attackPct,
                    ref defenseFlat,   ref defensePct,
                    ref moveSpeedFlat, ref moveSpeedPct,
                    ref attackSpeedFlat, ref attackSpeedPct,
                    ref dodgeFlat,     ref dodgePct,
                    ref healthStealFlat, ref healthStealPct);
>>>>>>> ScriptBreanchfixs
            }

            foreach (var kvp in a.inst.nonStackableValues)
            {
                var (stat, mode) = kvp.Key;
                ApplyStat(stat, mode, kvp.Value,
                    ref maxHealthFlat, ref maxHealthPct,
<<<<<<< HEAD
                    ref attackFlat, ref attackPct,
                    ref defenseFlat, ref defensePct,
                    ref moveSpeedFlat, ref moveSpeedPct,
                    ref attackSpeedFlat, ref attackSpeedPct,
                    ref dodgeFlat, ref dodgePct);
            }
        }

        MaxHealth = Mathf.Max(1f, (baseMaxHealth + maxHealthFlat) * (1f + maxHealthPct));
        Attack = Mathf.Max(0f, (baseAttack + attackFlat) * (1f + attackPct));
        Defense = Mathf.Max(0f, (baseDefense + defenseFlat) * (1f + defensePct));
        MoveSpeed = Mathf.Max(0f, (baseMoveSpeed + moveSpeedFlat) * (1f + moveSpeedPct));
        AttackSpeed = Mathf.Max(0.01f, (baseAttackSpeed + attackSpeedFlat) * (1f + attackSpeedPct));
        DodgeChance = Mathf.Clamp((baseDodgeChance + dodgeFlat) * (1f + dodgePct), 0f, 0.75f);
=======
                    ref attackFlat,    ref attackPct,
                    ref defenseFlat,   ref defensePct,
                    ref moveSpeedFlat, ref moveSpeedPct,
                    ref attackSpeedFlat, ref attackSpeedPct,
                    ref dodgeFlat,     ref dodgePct,
                    ref healthStealFlat, ref healthStealPct);
            }
        }

        MaxHealth    = Mathf.Max(1f,    (baseMaxHealth   + maxHealthFlat)   * (1f + maxHealthPct));
        Attack       = Mathf.Max(0f,    (baseAttack      + attackFlat)      * (1f + attackPct));
        Defense      = Mathf.Max(0f,    (baseDefense     + defenseFlat)     * (1f + defensePct));
        MoveSpeed    = Mathf.Max(0f,    (baseMoveSpeed   + moveSpeedFlat)   * (1f + moveSpeedPct));
        AttackSpeed  = Mathf.Max(0.01f, (baseAttackSpeed + attackSpeedFlat) * (1f + attackSpeedPct));
        DodgeChance  = Mathf.Clamp(     (baseDodgeChance + dodgeFlat)      * (1f + dodgePct),      0f, 0.75f);
        HealthSteal  = Mathf.Clamp01(   (baseHealthSteal + healthStealFlat) * (1f + healthStealPct));
>>>>>>> ScriptBreanchfixs

        currentHealth = keepHealthPercent
            ? Mathf.Clamp(MaxHealth * healthPct, 0f, MaxHealth)
            : Mathf.Clamp(currentHealth, 0f, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private static void ApplyStat(
        StatType stat, ModifierMode mode, float value,
<<<<<<< HEAD
        ref float maxHealthFlat, ref float maxHealthPct,
        ref float attackFlat, ref float attackPct,
        ref float defenseFlat, ref float defensePct,
        ref float moveSpeedFlat, ref float moveSpeedPct,
        ref float attackSpeedFlat, ref float attackSpeedPct,
        ref float dodgeFlat, ref float dodgePct)
    {
        switch (stat)
        {
            case StatType.MaxHealth: AddTo(value, mode, ref maxHealthFlat, ref maxHealthPct); break;
            case StatType.Attack: AddTo(value, mode, ref attackFlat, ref attackPct); break;
            case StatType.Defense: AddTo(value, mode, ref defenseFlat, ref defensePct); break;
            case StatType.MoveSpeed: AddTo(value, mode, ref moveSpeedFlat, ref moveSpeedPct); break;
            case StatType.AttackSpeed: AddTo(value, mode, ref attackSpeedFlat, ref attackSpeedPct); break;
            case StatType.DodgeChance: AddTo(value, mode, ref dodgeFlat, ref dodgePct); break;
=======
        ref float maxHealthFlat,   ref float maxHealthPct,
        ref float attackFlat,      ref float attackPct,
        ref float defenseFlat,     ref float defensePct,
        ref float moveSpeedFlat,   ref float moveSpeedPct,
        ref float attackSpeedFlat, ref float attackSpeedPct,
        ref float dodgeFlat,       ref float dodgePct,
        ref float healthStealFlat, ref float healthStealPct)
    {
        switch (stat)
        {
            case StatType.MaxHealth:   AddTo(value, mode, ref maxHealthFlat,   ref maxHealthPct);   break;
            case StatType.Attack:      AddTo(value, mode, ref attackFlat,      ref attackPct);      break;
            case StatType.Defense:     AddTo(value, mode, ref defenseFlat,     ref defensePct);     break;
            case StatType.MoveSpeed:   AddTo(value, mode, ref moveSpeedFlat,   ref moveSpeedPct);   break;
            case StatType.AttackSpeed: AddTo(value, mode, ref attackSpeedFlat, ref attackSpeedPct); break;
            case StatType.DodgeChance: AddTo(value, mode, ref dodgeFlat,       ref dodgePct);       break;
            case StatType.HealthSteal: AddTo(value, mode, ref healthStealFlat, ref healthStealPct); break;
>>>>>>> ScriptBreanchfixs
        }
    }

    private static void AddTo(float value, ModifierMode mode, ref float flat, ref float pct)
    {
        if (mode == ModifierMode.Flat) flat += value;
        else pct += value;
    }

    // ------------------------------------------------------------------ Combat
<<<<<<< HEAD

    public float GetDamageRoll() => Attack;
=======
>>>>>>> ScriptBreanchfixs

    public float GetDamageRoll() => Attack;

<<<<<<< HEAD
        // Check dodge first � if dodged, nothing happens at all
        if (UnityEngine.Random.value < DodgeChance)
        {
            Debug.Log($"{name} dodged the attack!");
            return;
        }

        // Apply defense reduction
        float finalDamage = Mathf.Max(0f, incomingDamage - Defense);

        // Even if defense absorbs all damage, fire OnDamaged with 0
        // so PassiveManager still reacts and applies the on-hit food buff
        if (finalDamage <= 0f)
        {
            OnDamaged?.Invoke(0f);
            return;
        }

        // Subtract health FIRST � then fire events so listeners see the correct health value
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        // Fire OnDamaged AFTER health is updated � PassiveManager subscribes to this
=======
    /// <summary>
    /// Apply damage to this entity.
    /// Pass the attacker's StatsManager to trigger HealthSteal healing on their side.
    /// Returns true if the attack actually landed (not dodged) — false on a dodge or
    /// when already dead. Used by AttackHitbox to gate OnEnemyHit so on-hit effects
    /// (like enemy debuffs) can't fire off a swing that was evaded.
    /// </summary>
    public bool TakeDamage(float incomingDamage, StatsManager attacker = null)
    {
        if (IsDead) return false;

        // Check dodge first — if dodged, nothing happens at all
        if (UnityEngine.Random.value < DodgeChance)
        {
            Debug.Log($"{name} dodged the attack!");
            return false;
        }

        // Apply defense reduction
        float finalDamage = Mathf.Max(0f, incomingDamage - Defense);

        // Even if defense absorbs all damage, fire OnDamaged with 0 so PassiveManager
        // still reacts and applies the on-hit food buff — this still counts as a landed hit.
        if (finalDamage <= 0f)
        {
            OnDamaged?.Invoke(0f);
            return true;
        }

        // Subtract health FIRST — then fire events so listeners see the correct health value
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        // HealthSteal — heal the attacker based on actual damage dealt (after defense)
        if (attacker != null && attacker.HealthSteal > 0f)
            attacker.Heal(finalDamage * attacker.HealthSteal);

        // Fire OnDamaged AFTER health is updated — PassiveManager subscribes to this
>>>>>>> ScriptBreanchfixs
        OnDamaged?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDied?.Invoke();
            OnAnyDied?.Invoke(this, attacker);
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + Mathf.Max(0f, amount));
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}
