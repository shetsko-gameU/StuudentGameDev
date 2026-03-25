using System;
using System.Collections.Generic;
using UnityEngine;

public class StatManager : MonoBehaviour
{
    [Header("Source (ScriptableObject)")]
    [SerializeField] private BaseStatsSO baseStats;

    [Header("Runtime Health")]
    [SerializeField] public float currentHealth;

    // Base copies (never change after load)
    private float baseMaxHealth, baseAttack, baseDefense, baseMoveSpeed, baseAttackSpeed;

    // Final runtime values (after modifiers)
    public float MaxHealth { get; private set; }
    public float Attack { get; private set; }
    public float Defense { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDied;

    private class ActiveRolled
    {
        public RolledModifierInstance inst;
        public float timeRemaining; // <=0 means permanent
        public int stacks = 1;
    }

    private readonly List<ActiveRolled> active = new();

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

    public void LoadFromSO(BaseStatsSO stats, bool setHealthToFull)
    {
        if (stats == null)
        {
            Debug.LogError($"{name}: StatManager missing UnitStatsSO");
            enabled = false;
            return;
        }

        baseStats = stats;

        baseMaxHealth = Mathf.Max(1f, stats.maxHealth);
        baseAttack = Mathf.Max(0f, stats.attack);
        baseDefense = Mathf.Max(0f, stats.defense);
        baseMoveSpeed = Mathf.Max(0f, stats.moveSpeed);
        baseAttackSpeed = Mathf.Max(0.01f, stats.attackSpeed);
       

        active.Clear();
        RecalculateFinalStats(keepHealthPercent: false);

        currentHealth = setHealthToFull ? MaxHealth : Mathf.Clamp(currentHealth, 0f, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // ---------- Add a rolled modifier ----------
    public void AddRolledModifier(RolledModifierInstance inst)
    {
        if (inst == null || inst.source == null) return;

        // Stack behavior: stack if ANY line can stack in the source
        bool canStack = AnyLineStackable(inst.source);

        var existing = active.Find(x => x.inst.source == inst.source);
        if (existing != null && canStack)
        {
            existing.stacks += 1;
            existing.stacks = ApplyStackCaps(existing.stacks, inst.source);

            // refresh timer if timed
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

        if (cap <= 1) return stacks;
        return Mathf.Min(stacks, cap);
    }

    private void RecalculateFinalStats(bool keepHealthPercent)
    {
        float healthPct = MaxHealth > 0f ? currentHealth / MaxHealth : 1f;

        float mh = baseMaxHealth, atk = baseAttack, def = baseDefense, ms = baseMoveSpeed, aspd = baseAttackSpeed;

        float mhFlat = 0, atkFlat = 0, defFlat = 0, msFlat = 0, aspdFlat = 0;
        float mhPct = 0, atkPct = 0, defPct = 0, msPct = 0, aspdPct = 0;

        foreach (var a in active)
        {
            int stacks = Mathf.Max(1, a.stacks);

            foreach (var kvp in a.inst.values)
            {
                var (stat, mode) = kvp.Key;
                float value = kvp.Value;

                // If the source line is not stackable, we still keep it at 1 stack.
                // Simple rule here: if ANY line can stack, we stack all rolled values.
                
                float applied = value * stacks;

                switch (stat)
                {
                    case StatType.MaxHealth: Add(applied, mode, ref mhFlat, ref mhPct); break;
                    case StatType.Attack: Add(applied, mode, ref atkFlat, ref atkPct); break;
                    case StatType.Defense: Add(applied, mode, ref defFlat, ref defPct); break;
                    case StatType.MoveSpeed: Add(applied, mode, ref msFlat, ref msPct); break;
                    case StatType.AttackSpeed: Add(applied, mode, ref aspdFlat, ref aspdPct); break;
                    
                }
            }
        }

        MaxHealth = Mathf.Max(1f, (mh + mhFlat) * (1f + mhPct));
        Attack = Mathf.Max(0f, (atk + atkFlat) * (1f + atkPct));
        Defense = Mathf.Max(0f, (def + defFlat) * (1f + defPct));
        MoveSpeed = Mathf.Max(0f, (ms + msFlat) * (1f + msPct));
        AttackSpeed = Mathf.Max(0.01f, (aspd + aspdFlat) * (1f + aspdPct));
        

        currentHealth = keepHealthPercent ? Mathf.Clamp(MaxHealth * healthPct, 0f, MaxHealth)
                                          : Mathf.Clamp(currentHealth, 0f, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private static void Add(float value, ModifierMode mode, ref float flat, ref float pct)
    {
        if (mode == ModifierMode.Flat) flat += value;
        else pct += value;
    }

    // ---------- Combat ----------
    public float GetDamageRoll()
    {
        float damage = Attack;
        
        return damage;
    }

    public void TakeDamage(float incomingDamage)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(0f, incomingDamage - Defense);
        if (finalDamage <= 0f) return;

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