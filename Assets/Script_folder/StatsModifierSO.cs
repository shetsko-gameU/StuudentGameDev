using UnityEngine;

public enum StatType
{
    MaxHealth,
    Attack,
    Defense,
    MoveSpeed,
    AttackSpeed,
    DodgeChance,  // 0 to 1 (so 0.15 = 15%)
    HealthSteal,  // 0 to 1 — fraction of damage dealt restored as health (so 0.20 = 20% lifesteal)
}

public enum ModifierMode
{
    Flat,    // +5
    Percent  // +0.20 means +20% of base
}

public enum Rarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[System.Serializable]
public struct StatRollLine
{
    public StatType stat;
    public ModifierMode mode;

    [Tooltip("Rolled value will be between min and max.")]
    public float minValue;
    public float maxValue;

    [Tooltip("Round to nearest step. 1 = whole numbers, 0 = no rounding.")]
    public float step;

    [Tooltip("If true, this modifier can stack multiple times.")]
    public bool canStack;

    [Tooltip("Max stacks if canStack is true. Set 0 or 1 for no cap.")]
    public int maxStacks;
}

/// <summary>
/// A template for a stat modifier — a set of StatRollLine entries, each rolled independently
/// at runtime by ModifierRoller.Roll into a RolledModifierInstance. Used as the "what stats
/// does this grant" building block for passives (PassiveEffectSO.modifierToApply,
/// OnHitPassiveSO/KillPassiveSO/DebuffOnHitPassiveSO's buff/debuff templates), food stat
/// boosts (FoodStatPassiveSO.statTemplate), and crafted items (CraftRecipeSO/RarityRecipeSO
/// results — stats aren't rolled until the result is eaten).
///
/// Setup:
///   1. Create via Assets → Create → Game → Stats → Stats Modifier (Roguelite).
///   2. Add one or more StatRollLine entries under lines — each needs a stat, a mode (Flat
///      or Percent), a min/max roll range, and canStack/maxStacks if it should stack.
///   3. Set rarity (affects the roll via ModifierRoller's rarity multiplier) and, if this
///      SO is meant to be combined via crafting, a craftFamily string.
///   4. Assign this asset wherever a system asks for a StatsModifierSO — it's the shared
///      currency for "what does this thing actually do to stats."
/// </summary>
[CreateAssetMenu(fileName = "StatsModifier", menuName = "Game/Stats/Stats Modifier (Roguelite)")]
public class StatsModifierSO : ScriptableObject
{
    public string displayName;
    public Rarity rarity = Rarity.Common;

    [Tooltip("Items sharing the same craft family can be combined with a RarityRecipeSO. " +
             "Example: set 'apple' on Common/Rare/Epic/Legendary apple SOs so they can be combined.")]
    public string craftFamily = "";

    [Tooltip("0 = permanent. Otherwise expires after this many seconds.")]
    public float durationSeconds = 0f;

    [Tooltip("Stat lines that will roll when this modifier is created at runtime.")]
    public StatRollLine[] lines;

    
    public Texture Image;

    public string EffectDescription;
}
