using UnityEngine;

/// <summary>
/// Two independent ability slots (primary/secondary), each with its own cooldown timer.
/// Delegates the actual effect to whatever AbilitySO is equipped — this script only
/// handles cooldown gating and calling CanUse/Activate.
///
/// Setup:
///   1. Add to the player alongside StatsManager/PassiveManager.
///   2. Drag an AbilitySO asset (e.g. DashAbilitySO) into primaryAbility.ability and,
///      optionally, secondaryAbility.ability.
///   3. Wire UsePrimary/UseSecondary to Input System actions (see Player.prefab's
///      PlayerInput action events for the existing Dash/Ult bindings).
///   4. The secondary slot can also be filled at runtime by PassiveManager.AddUltAbility
///      when a UltFoodSO is eaten — no extra wiring needed for that path.
/// </summary>
public class AbilityRunner : MonoBehaviour
{
    [System.Serializable]
    public class AbilitySlot
    {
        public AbilitySO ability;
        [HideInInspector] public float nextReadyTime;
    }

    [Header("Equipped Abilities")]
    public AbilitySlot primaryAbility = new AbilitySlot();
    public AbilitySlot secondaryAbility = new AbilitySlot();

    private void Start()
    {
        if (primaryAbility.ability != null)
        {
            primaryAbility.ability.OnEquipped(gameObject);
        }

        if (secondaryAbility.ability != null)
        {
            secondaryAbility.ability.OnEquipped(gameObject);
        }
    }

    // Call these from Input System (or from Update for testing)
    public void UsePrimary()
    {
        TryUse(primaryAbility);
    }

    public void UseSecondary()
    {
        TryUse(secondaryAbility);
    }

    private void TryUse(AbilitySlot slot)
    {
        if (slot == null || slot.ability == null)
        {
            return;
        }

        if (Time.time < slot.nextReadyTime)
        {
            return; // still on cooldown
        }

        if (!slot.ability.CanUse(gameObject))
        {
            return;
        }

        slot.ability.Activate(gameObject);
        slot.nextReadyTime = Time.time + slot.ability.cooldownSeconds;
    }
}