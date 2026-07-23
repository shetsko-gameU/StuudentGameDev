using UnityEngine;

[CreateAssetMenu(menuName = "Game/Passives/Passive Effect")]
public class PassiveEffectSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Passive";

    [Header("What it does")]
    public StatsModifierSO modifierToApply;

    [Tooltip("If true, PassiveManager applies this automatically on Start. " +
             "If false, you must call passiveManager.ApplyAlwaysOnPassive(this) manually.")]
    public bool applyOnStart = true;

    // Stored at runtime when this passive is applied.
    // Not serialized — resets each play session which is correct behaviour.
    // PassiveManager uses this to remove the exact stat roll if the passive is taken away.
    [System.NonSerialized]
    public RolledModifierInstance ActiveRoll;
}