using UnityEngine;

/// <summary>
/// Food passive that applies a debuff to the ENEMY when the player's weapon lands a hit
/// on them — the mirror image of OnHitPassiveSO (which buffs the PLAYER when the player
/// is hit). Fifth passive type, alongside PassiveEffectSO / OnHitPassiveSO /
/// FoodStatPassiveSO / KillPassiveSO, all owned by PassiveManager.
///
/// debuffTemplate is a normal StatsModifierSO — set NEGATIVE values on Attack/Defense/
/// MoveSpeed (or DodgeChance/HealthSteal) and a positive durationSeconds so it wears off;
/// durationSeconds = 0 would make it permanent, which is almost never what a debuff wants.
/// canStack should normally stay OFF — StatsManager now refreshes a non-stacking debuff's
/// timer on repeated hits instead of letting it stack in magnitude.
/// </summary>
[CreateAssetMenu(menuName = "Game/Food/Food Passive (Debuff On Hit)")]
public class DebuffOnHitPassiveSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Debuff Passive";

    [Tooltip("Passives with the same Family ID are treated as the same food at different rarities. " +
             "Leave blank if this passive has no rarity variants.")]
    public string passiveFamily = "";

    [Header("On confirmed hit, apply this debuff to the enemy")]
    [Range(0f, 1f)]
    [Tooltip("Chance to apply per hit. 1 = always, 0.25 = 25% of hits.")]
    public float procChance = 1f;

    [Tooltip("Rolled fresh on every proc and applied to the ENEMY'S StatsManager. " +
             "Use negative values + a positive durationSeconds for a temporary debuff.")]
    public StatsModifierSO debuffTemplate;

    [Tooltip("(Optional) Spawn a world entity on the enemy when this procs — e.g. a status marker/VFX.")]
    public GameObject SpawnEntity;
}
