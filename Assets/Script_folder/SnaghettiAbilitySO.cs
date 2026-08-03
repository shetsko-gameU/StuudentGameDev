using UnityEngine;

/// <summary>
/// The Snake+Snake ultimate — activated (like DashAbilitySO), not a passive food buff.
/// On activation, rolls buffTemplate and applies it via StatsManager.AddRolledModifier.
///
/// Unlike Dash, this needs no coroutine — StatsManager's own Update() already ticks down
/// and removes the modifier when buffTemplate.durationSeconds runs out, so Activate() is a
/// single roll-and-apply. Cooldown between uses is handled by AbilityRunner via the
/// inherited cooldownSeconds field, same as every other ability.
/// </summary>
[CreateAssetMenu(menuName = "Game/Abilities/Snaghetti")]
public class SnaghettiAbilitySO : AbilitySO
{
    [Header("Buff")]
    [Tooltip("Rolled fresh on every activation. Give it Attack + MoveSpeed lines and a " +
             "positive durationSeconds so the ult wears off. Rarity on this SO scales the " +
             "rolled magnitude automatically (via ModifierRoller) — duration does NOT " +
             "auto-scale with rarity, set it by hand per tier if you want that.")]
    public StatsModifierSO buffTemplate;

    public override bool CanUse(GameObject user)
    {
        if (buffTemplate == null) return false;

        StatsManager stats = user.GetComponent<StatsManager>();
        if (stats != null && stats.IsDead) return false;

        return true;
    }

    public override void Activate(GameObject user)
    {
        StatsManager stats = user.GetComponent<StatsManager>();
        if (stats == null || buffTemplate == null) return;

        RolledModifierInstance roll = ModifierRoller.Roll(buffTemplate);
        stats.AddRolledModifier(roll);
    }
}
