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
