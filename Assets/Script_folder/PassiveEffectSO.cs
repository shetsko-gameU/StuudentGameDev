using UnityEngine;

/// <summary>
/// A permanent, always-on stat bonus — the first of PassiveManager's six passive types,
/// and the only one not tied to eating food (starting perks, equipment, etc.). Applied
/// once via PassiveManager.ApplyAlwaysOnPassive, either automatically on Start (if
/// applyOnStart) or manually.
///
/// Setup:
///   1. Create via Assets → Create → Game → Passives → Passive Effect.
///   2. Assign a StatsModifierSO to modifierToApply (its stat lines define the bonus).
///   3. Drag this asset into PassiveManager.alwaysOnPassives on the entity's prefab.
/// </summary>
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
    // Not serialized � resets each play session which is correct behaviour.
    // PassiveManager uses this to remove the exact stat roll if the passive is taken away.
    [System.NonSerialized]
    public RolledModifierInstance ActiveRoll;
}