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
    private float baseMaxHealth, baseAttack, baseDefense, baseMoveSpeed, baseAttackSpeed, baseDodgeChance;

    // Final runtime values (after modifiers)
    public float MaxHealth { get; private set; }
    public float Attack { get; private set; }
    public float Defense { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    public float DodgeChance { get; private set; }

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDied;
    public event Action<float> OnDamaged;

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

        baseMaxHealth = Mathf.Max(1f, stats.maxHealth);
        baseAttack = Mathf.Max(0f, stats.attack);
        baseDefense = Mathf.Max(0f, stats.defense);
        baseMoveSpeed = Mathf.Max(0f, stats.moveSpeed);
        baseAttackSpeed = Mathf.Max(0.01f, stats.attackSpeed);
        baseDodgeChance = Mathf.Clamp01(stats.dodgeChance);

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

            // Refresh timer on stack
            if (inst.durationSeconds > 0f)
                existing.timeRemaining = inst.durationSeconds;
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

    /// <summary>Remove the modifier that was applied from a specific rolled instance (e.g. unequipping an item).</summary>
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

    /// <summary>Remove ALL modifiers that came from a given SO (e.g. clearing a buff source).</summary>
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

        // Accumulate flat and percent bonuses per stat
        float maxHealthFlat = 0, attackFlat = 0, defenseFlat = 0, moveSpeedFlat = 0, attackSpeedFlat = 0, dodgeFlat = 0;
        float maxHealthPct = 0, attackPct = 0, defensePct = 0, moveSpeedPct = 0, attackSpeedPct = 0, dodgePct = 0;

        foreach (var a in active)
        {
            int stacks = Mathf.Max(1, a.stacks);

            // --- Stackable values: multiply by stack count ---
            foreach (var kvp in a.inst.stackableValues)
            {
                var (stat, mode) = kvp.Key;
                float applied = kvp.Value * stacks;
                ApplyStat(stat, mode, applied,
                    ref maxHealthFlat, ref maxHealthPct,
                    ref attackFlat, ref attackPct,
                    ref defenseFlat, ref defensePct,
                    ref moveSpeedFlat, ref moveSpeedPct,
                    ref attackSpeedFlat, ref attackSpeedPct,
                    ref dodgeFlat, ref dodgePct);
            }

            // --- Non-stackable values: always applied once ---
            foreach (var kvp in a.inst.nonStackableValues)
            {
                var (stat, mode) = kvp.Key;
                ApplyStat(stat, mode, kvp.Value,
                    ref maxHealthFlat, ref maxHealthPct,
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

        currentHealth = keepHealthPercent
            ? Mathf.Clamp(MaxHealth * healthPct, 0f, MaxHealth)
            : Mathf.Clamp(currentHealth, 0f, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private static void ApplyStat(
        StatType stat, ModifierMode mode, float value,
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
        }
    }

    private static void AddTo(float value, ModifierMode mode, ref float flat, ref float pct)
    {
        if (mode == ModifierMode.Flat) flat += value;
        else pct += value;
    }

    // ------------------------------------------------------------------ Combat

    public float GetDamageRoll() => Attack;

    public void TakeDamage(float incomingDamage)
    {
        if (IsDead) return;

        if (UnityEngine.Random.value < DodgeChance)
            return; // dodged

        float finalDamage = Mathf.Max(0f, incomingDamage - Defense);
        if (finalDamage <= 0f) return;

        OnDamaged?.Invoke(finalDamage);
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + Mathf.Max(0f, amount));
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}