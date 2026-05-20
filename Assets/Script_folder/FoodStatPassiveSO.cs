using UnityEngine;

/// <summary>
/// Attach this to food that gives a flat stat boost when eaten.
/// The boost lasts until the player eats a higher rarity version of the same food.
///
/// Examples:
///   Apple_Common  →  +5 MaxHealth  (replaces nothing)
///   Apple_Rare    →  +8 MaxHealth  (replaces Common version)
///   Apple_Epic    →  +12 MaxHealth (replaces Rare version)
///
/// The actual stat values come from the StatsModifierSO you assign to statTemplate.
/// Rarity is read from that same SO — you don't set it here.
/// </summary>
[CreateAssetMenu(menuName = "Game/Food/Food Stat Passive")]
public class FoodStatPassiveSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Food Stat Passive";

    [Tooltip("Passives with the same Family ID are treated as the same food at different rarities. " +
             "Example: 'apple_hp' on Common, Rare, Epic, Legendary versions. " +
             "If the player already has a lower rarity version, eating a higher one replaces it. " +
             "Leave blank if this passive has no rarity variants.")]
    public string passiveFamily = "";

    [Header("Stat Boost")]
    [Tooltip("The StatsModifierSO that defines what stats this food gives and at what rarity. " +
             "The rarity on this SO is used for upgrade comparisons.")]
    public StatsModifierSO statTemplate;
}