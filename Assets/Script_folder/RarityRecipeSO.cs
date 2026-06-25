using UnityEngine;

/// <summary>
/// A crafting recipe that matches ingredients by craft family and resolves
/// the output rarity probabilistically:
///
///   Same rarity       → 100 % that rarity
///   1 tier apart      →  50 % higher /  50 % lower
///   2 tiers apart     →  25 % higher /  75 % lower
///   3 tiers apart     →  15 % higher /  85 % lower
///
/// Assign output SOs for each rarity tier below.
/// Leave a slot null if that tier cannot appear (safe — will fall back to Common).
/// </summary>
[CreateAssetMenu(menuName = "Game/Crafting/Rarity Recipe")]
public class RarityRecipeSO : ScriptableObject
{
    [Header("Ingredient families (must match StatsModifierSO.craftFamily)")]
    [Tooltip("Required. Any item whose craftFamily matches this string can fill the primary slot.")]
    public string primaryFamily;

    [Tooltip("Optional. Leave empty for single-ingredient recipes.")]
    public string secondaryFamily;

    [Header("Results — assign the SO for each rarity tier you support")]
    public StatsModifierSO commonResult;
    public StatsModifierSO rareResult;
    public StatsModifierSO epicResult;
    public StatsModifierSO legendaryResult;

    // ------------------------------------------------------------------ Matching

    public bool Matches(StatsModifierSO prim, StatsModifierSO secon)
    {
        if (prim == null || string.IsNullOrEmpty(prim.craftFamily)) return false;
        if (prim.craftFamily != primaryFamily) return false;

        bool needsSecondary = !string.IsNullOrEmpty(secondaryFamily);

        if (!needsSecondary)
            return secon == null || string.IsNullOrEmpty(secon.craftFamily);

        if (secon == null || string.IsNullOrEmpty(secon.craftFamily)) return false;
        return secon.craftFamily == secondaryFamily;
    }

    // ------------------------------------------------------------------ Rarity roll

    /// <summary>
    /// Rolls the output rarity from two input rarities.
    /// Gap 0 → guaranteed; Gap 1 → 50 %; Gap 2 → 25 %; Gap 3 → 15 % for higher tier.
    /// </summary>
    public static Rarity RollOutputRarity(Rarity a, Rarity b)
    {
        Rarity higher = (Rarity)Mathf.Max((int)a, (int)b);
        Rarity lower  = (Rarity)Mathf.Min((int)a, (int)b);
        int gap = (int)higher - (int)lower;

        if (gap == 0) return higher;

        float higherChance = gap switch
        {
            1 => 0.50f,
            2 => 0.25f,
            3 => 0.15f,
            _ => 0.50f
        };

        return Random.value < higherChance ? higher : lower;
    }

    // ------------------------------------------------------------------ Result lookup

    /// <summary>Returns the result SO for the given rarity. Falls back to commonResult if null.</summary>
    public StatsModifierSO GetResult(Rarity rarity)
    {
        StatsModifierSO result = rarity switch
        {
            Rarity.Common    => commonResult,
            Rarity.Rare      => rareResult,
            Rarity.Epic      => epicResult,
            Rarity.Legendary => legendaryResult,
            _                => commonResult
        };

        // Graceful fallback — warn so the designer knows to fill in the slot
        if (result == null)
            Debug.LogWarning($"RarityRecipeSO '{name}': no result assigned for {rarity}, falling back to Common.");

        return result ?? commonResult;
    }
}
