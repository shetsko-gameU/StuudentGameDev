using UnityEngine;

/// <summary>
/// Food that equips an activated ULTIMATE ability (e.g. SnaghettiAbilitySO) into the
/// player's ability slot when eaten — the food-acquisition wrapper around an AbilitySO,
/// the same role OnHitPassiveSO/KillPassiveSO/DebuffOnHitPassiveSO play for their effects.
/// Distinct from those: this targets AbilityRunner, not StatsManager, and only one can
/// ever be equipped at a time (see PassiveManager.AddUltAbility).
/// </summary>
[CreateAssetMenu(menuName = "Game/Food/Food Passive (Ult Ability)")]
public class UltFoodSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Ult Ability";

    [Tooltip("Identifies which ult line this is. Eating a food with the SAME ultFamily as " +
             "the currently-equipped ult only replaces it if this one is a strictly higher " +
             "rarity (no downgrading, no re-equipping the same rarity). Eating a food from a " +
             "DIFFERENT ultFamily always replaces the current ult — only one can be held.")]
    public string ultFamily = "";

    [Tooltip("Used only to compare against the currently-equipped ult of the same family.")]
    public Rarity rarity = Rarity.Common;

    [Tooltip("The activated ability this food grants — gets assigned into AbilityRunner.secondaryAbility.")]
    public AbilitySO ability;
}
