using UnityEngine;

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