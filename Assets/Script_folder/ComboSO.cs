using UnityEngine;

/// <summary>
/// One hit in a combo sequence.
/// </summary>
[System.Serializable]
public class ComboHitData
{
    [Tooltip("Label shown in debug logs and the Inspector.")]
    public string displayName = "Hit";

    [Tooltip("Multiplied against the player's Attack stat to get final damage. " +
             "1.0 = normal damage. 2.0 = double. 0.5 = half.")]
    public float damageMultiplier = 1f;

    [Tooltip("How long after this hit the player can press attack to continue the combo. " +
             "If they don't press in time the combo resets.")]
    public float chainWindowSeconds = 0.6f;

    [Tooltip("Delay in seconds between pressing attack and the hit actually registering. " +
             "Use this to sync the damage with the swing animation. 0 = instant.")]
    public float hitCheckDelay = 0.1f;

    [Tooltip("Animator trigger fired when this hit starts. " +
             "Each hit can use a different trigger so you can chain different animations.")]
    public string animatorTrigger = "Attack";
}

/// <summary>
/// Defines a full combo sequence as a reusable asset.
/// Create via: Right-click → Create → Game → Combat → Combo
///
/// Example:
///   BasicSwordCombo
///     [0] Slash    1.0x damage  chain window 0.6s
///     [1] Slash    1.0x damage  chain window 0.6s
///     [2] Finisher 2.0x damage  (last hit — no chain needed)
/// </summary>
[CreateAssetMenu(menuName = "Game/Combat/Combo")]
public class ComboSO : ScriptableObject
{
    [Tooltip("All hits in this combo in order. First = index 0, Last = final index.")]
    public ComboHitData[] hits;
}