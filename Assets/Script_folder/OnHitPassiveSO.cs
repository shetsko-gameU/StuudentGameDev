using UnityEngine;

[CreateAssetMenu(menuName = "Game/Food/Food Passive (On Hit Buff)")]
public class OnHitPassiveSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Food Passive";

    [Tooltip("Passives with the same Family ID are treated as the same food at different rarities. " +
             "Leave blank if this passive has no rarity variants.")]
    public string passiveFamily = "";

    [Header("When hit, apply this buff")]
    public StatsModifierSO buffTemplate;

    [Min(0.01f)]
    public float buffDurationSeconds = 3f;

    public GameObject SpawnEntity;

    [Header("Combo Triggers")]
    [Tooltip("If true and this passive is active in PassiveManager, " +
             "it also fires when the player lands the FIRST hit of a combo.")]
    public bool triggerOnFirstHit = false;

    [Tooltip("If true and this passive is active in PassiveManager, " +
             "it also fires when the player lands the LAST hit of a combo.")]
    public bool triggerOnLastHit = false;

    [Header("Kill Trigger")]
    [Tooltip("If true and this passive is active in PassiveManager, " +
             "it fires every time the player kills an enemy. " +
             "Unlike the on-hit/combo triggers, the buff uses the buffTemplate's OWN " +
             "durationSeconds (0 = permanent) instead of buffDurationSeconds — set " +
             "canStack + maxStacks on the template's stat line for per-kill stacking.")]
    public bool triggerOnKill = false;
}