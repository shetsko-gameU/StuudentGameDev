using UnityEngine;

[CreateAssetMenu(menuName = "Game/Passives/Passive Effect")]
public class PassiveEffectSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Passive";

    [Header("What it does")]
    public StatsModifierSO modifierToApply;

    [Tooltip("If true, applies on Start/Awake. If false, you must call Apply() manually.")]
    public bool applyOnStart = true;
}