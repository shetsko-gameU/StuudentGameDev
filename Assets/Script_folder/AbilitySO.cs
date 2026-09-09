using UnityEngine;

/// <summary>
/// Base class for an activated ability — equipped into one of AbilityRunner's two slots
/// and fired on input via AbilityRunner.UsePrimary/UseSecondary. Subclass this for each
/// concrete ability (DashAbilitySO, SnaghettiAbilitySO, RoastedSlimeAbilitySO,
/// DryadDelightsAbilitySO); override CanUse for extra gating (e.g. blocked while dead) and
/// Activate for the actual effect.
///
/// Setup: not used directly — create a concrete subclass asset instead, then drag it into
/// AbilityRunner.primaryAbility.ability or .secondaryAbility.ability (or grant it via a
/// UltFoodSO, for the secondary slot specifically).
/// </summary>
public abstract class AbilitySO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Ability";

    [Header("Cooldown")]
    [Min(0f)] public float cooldownSeconds = 1f;

    // Called once when ability is equipped/assigned
    public virtual void OnEquipped(GameObject user)
    {
    }

    // If this returns false, ability won't fire (even if off cooldown)
    public virtual bool CanUse(GameObject user)
    {
        return true;
    }

    // This is the actual ability action
    public abstract void Activate(GameObject user);
}
