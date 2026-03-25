using UnityEngine;

public enum StatType
{
    MaxHealth,
    Attack,
    Defense,
    MoveSpeed,
    AttackSpeed,
    
}

public enum ModifierMode
{
    Flat,       // +5
    Percent     // +0.20 means +20% of base
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

    [Tooltip("Rolled value will be between min and max (inclusive-ish).")]
    public float minValue;
    public float maxValue;

    [Tooltip("Flat: +value. Percent: +value (0.2 = +20%).")]
    public float value;

    [Tooltip("Round to nearest step. Examples: 1 for whole numbers, 0.1 for tenths. Set 0 for no rounding.")]
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

    [Tooltip("0 = permanent. Otherwise expires after duration seconds.")]
    public float durationSeconds = 0f;

    [Tooltip("Stat lines that will roll when this modifier is created at runtime.")]
    public StatRollLine[] lines;
}
