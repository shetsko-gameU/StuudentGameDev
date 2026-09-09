using UnityEngine;

/// <summary>
/// An exact-match crafting recipe: a specific primary (+ optional specific secondary)
/// StatsModifierSO pair always produces the same result. Checked by CraftSystem before
/// RarityRecipeSO — an exact match here short-circuits before the family/rarity recipe is
/// even considered.
///
/// Setup:
///   1. Create via Assets, Create, Game, Crafting, Recipe (Primary+Secondary). Recipe
///      assets live in Assets/Player/RecipeSO/.
///   2. Assign primary (required), secondary (optional, leave null for a single-ingredient
///      recipe), and result.
///   3. Drag it into CraftSystem.recipes on the crafting UI's GameObject.
/// </summary>
[CreateAssetMenu(menuName = "Game/Crafting/Recipe (Primary+Secondary)")]
public class CraftRecipeSO : ScriptableObject
{
    [Header("Ingredients")]
    public StatsModifierSO primary;          // required
    public StatsModifierSO secondary;        // optional (can be null)

    [Header("Result")]
    public StatsModifierSO result;           // required

    public bool Matches(StatsModifierSO prim, StatsModifierSO secon)
    {
        if (prim == null || result == null) return false;
        if (prim != primary) return false;

        // if recipe does not require a secondary ingredient
        if (secondary == null) return secon == null;

        return secon == secondary;
    }
}
