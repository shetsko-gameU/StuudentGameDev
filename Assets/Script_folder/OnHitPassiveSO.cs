using UnityEngine;

/// <summary>
/// Food passive that fires a TEMPORARY buff on the PLAYER every time the player takes
/// damage (via PassiveManager.HandlePlayerDamaged), and optionally also on a combo's first
/// and/or last hit landing (via ComboPassiveTrigger). The mirror of DebuffOnHitPassiveSO,
/// which targets the enemy instead.
///
/// Setup:
///   1. Create via Assets → Create → Game → Food → Food Passive (On Hit Buff).
///   2. Assign buffTemplate (a StatsModifierSO — its own durationSeconds is ignored here;
///      buffDurationSeconds on this asset controls how long each proc's buff lasts) and,
///      optionally, SpawnEntity (a world object to spawn on trigger).
///   3. Tick triggerOnFirstHit/triggerOnLastHit if this should also fire on a confirmed
///      combo hit, not just on taking damage.
///   4. Link it to a food item through PlayerConsume's foodPassives list (or drop it in
///      PassiveManager.startingFoodPassives for testing without eating anything).
/// </summary>
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
}