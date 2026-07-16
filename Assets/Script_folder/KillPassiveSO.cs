using UnityEngine;

/// <summary>
/// Food passive that fires every time the PLAYER kills an enemy — a distinct type
/// from OnHitPassiveSO (which fires when the player is HIT). Sits alongside
/// PassiveEffectSO / OnHitPassiveSO / FoodStatPassiveSO as the fourth passive type,
/// all owned by PassiveManager.
///
/// Typical use: "gain a stack of +Attack for each enemy killed" — set buffTemplate's
/// stat line to canStack = true with a maxStacks cap, and leave durationSeconds at 0
/// so stacks last the rest of the run (KillPassiveTrigger does not override duration).
/// </summary>
[CreateAssetMenu(menuName = "Game/Food/Food Passive (On Kill)")]
public class KillPassiveSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Kill Passive";

    [Tooltip("Passives with the same Family ID are treated as the same food at different rarities. " +
             "Leave blank if this passive has no rarity variants.")]
    public string passiveFamily = "";

    [Header("On kill, apply this buff")]
    [Tooltip("Rolled fresh on every kill and added via StatsManager.AddRolledModifier. " +
             "Set canStack + maxStacks on its stat line(s) for per-kill stacking. " +
             "durationSeconds on this SO controls how long each roll lasts — 0 = permanent.")]
    public StatsModifierSO buffTemplate;

    [Tooltip("(Optional) Spawn a world entity on kill, same as OnHitPassiveSO.SpawnEntity.")]
    public GameObject SpawnEntity;
}
