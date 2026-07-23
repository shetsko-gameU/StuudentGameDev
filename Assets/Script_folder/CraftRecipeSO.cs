using UnityEngine;

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
