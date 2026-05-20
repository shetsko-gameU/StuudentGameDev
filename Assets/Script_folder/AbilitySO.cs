using UnityEngine;

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
