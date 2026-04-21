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
            Debug.LogError($"{name}: StatsManager missing UnitStatsSO");
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

    // ---------- Add a rolled modifier ----------
    public void AddRolledModifier(RolledModifierInstance inst)
    {
       // if (inst == null || inst.source == null) return;

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
        Debug.Log("RecalculateFinalStats");

        float healthPct = this.MaxHealth > 0f ? currentHealth / this.MaxHealth : 1f;
             
        float mh = baseMaxHealth, atk = baseAttack, def = baseDefense, ms = baseMoveSpeed, aspd = baseAttackSpeed; 

        float dodge = baseDodgeChance;

        float MaxHealthFlat = 0, AttackFlat = 0, DefencseFlat = 0, MovmentSpeedFlat = 0, AttackSpeedFlat = 0, DodgeFlat = 0; 
        float MaxHealthPct = 0, AtkackPct = 0, DefencsePct = 0, MovmentSpeedPct = 0, AttackSpeedPct = 0, DodgePct = 0;  //Pct Mean percentage

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
                    case StatType.MaxHealth: Add(applied, mode, ref MaxHealthFlat, ref MaxHealthPct); break;
                    case StatType.Attack: Add(applied, mode, ref AttackFlat, ref AtkackPct); break;
                    case StatType.Defense: Add(applied, mode, ref DefencseFlat, ref DefencsePct); break;
                    case StatType.MoveSpeed: Add(applied, mode, ref MovmentSpeedFlat, ref MovmentSpeedPct); break;
                    case StatType.AttackSpeed: Add(applied, mode, ref AttackSpeedFlat, ref AttackSpeedPct); break;
                    case StatType.DodgeChance: Add(applied, mode, ref DodgeFlat, ref DodgePct); break;
                }
            }
        }

        this.MaxHealth = Mathf.Max(1f, (mh + MaxHealthFlat) * (1f + MaxHealthPct));
        Attack = Mathf.Max(0f, (atk + AttackFlat) * (1f + AtkackPct));
        Defense = Mathf.Max(0f, (def + DefencseFlat) * (1f + DefencsePct));
        MoveSpeed = Mathf.Max(0f, (ms + MovmentSpeedFlat) * (1f + MovmentSpeedPct));
        AttackSpeed = Mathf.Max(0.01f, (aspd + AttackSpeedFlat) * (1f + AttackSpeedPct));
        DodgeChance = Mathf.Clamp01((dodge + DodgeFlat) * (1f + DodgePct));

        currentHealth = keepHealthPercent ? Mathf.Clamp(this.MaxHealth * healthPct, 0f, this.MaxHealth)
                                          : Mathf.Clamp(currentHealth, 0f, this.MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, this.MaxHealth);

        DodgeChance = Mathf.Clamp(DodgeChance, 0f, 0.75f);

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

        if (UnityEngine.Random.value < DodgeChance)
        {
            // dodged, take 0 damage
            return;
        }


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

    public void RemoveAllFromSource(StatsModifierSO source)
    {
        if (source == null)
        {
            return;
        }

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
        {
            RecalculateFinalStats(keepHealthPercent: true);
        }
    }







}