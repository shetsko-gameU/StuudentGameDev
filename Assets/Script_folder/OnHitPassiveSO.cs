using UnityEngine;

[CreateAssetMenu(menuName = "Game/Food/Food Passive (On Hit Buff)")]
public class OnHitPassiveSO : ScriptableObject
{
    [Header("Info")]
    public string displayName = "Food Passive";

    [Tooltip("Passives with the same Family ID are treated as the same food at different rarities. " +
             "Example: 'fire_boost' on Common, Rare, Epic, and Legendary versions. " +
             "If the player already has a lower rarity version, eating a higher rarity one replaces it. " +
             "Leave blank if this passive has no rarity variants.")]
    public string passiveFamily = "";

    [Header("When hit, apply this buff")]
    public StatsModifierSO buffTemplate;

    [Min(0.01f)]
    public float buffDurationSeconds = 3f;
}