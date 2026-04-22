using UnityEngine;

[CreateAssetMenu(menuName = "Game/Food/Food Passive (On Hit Buff)")]
public class OnHitPassiveSO : ScriptableObject
{
    public string displayName = "Food Passive";

    [Header("When hit, apply this StatsModifierSO")]
    public StatsModifierSO buffTemplate;

    [Min(0.01f)]
    public float buffDurationSeconds = 3f;

    public bool preventDuplicates = true;
}