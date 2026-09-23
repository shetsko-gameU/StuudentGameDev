using UnityEngine;

/// <summary>
/// Put this on whichever GameObject the enemy's Animator is on - on these prefabs that is
/// the nested model child, not the enemy root.
///
/// Animation Events can only call methods on components attached to the same GameObject as
/// the Animator. EnemyHitbox lives on the attack point instead, so this relay bridges the
/// two. It is the enemy counterpart to AnimationEventRelay on the player.
///
/// To add the events in Unity:
///   1. Open the Animation window (not the Animator window).
///   2. Select the enemy's attack clip.
///   3. Scrub to the frame where the attack connects.
///   4. Click Add Event on the timeline.
///   5. Pick EnableHitbox from the Function dropdown, then add a second event a few frames
///      later for DisableHitbox.
///
/// Once real events exist, untick Use Swing Window on the EnemyHitbox so the timer fallback
/// stops competing with them.
/// </summary>
public class EnemyAnimationEventRelay : MonoBehaviour
{
    private EnemyHitbox hitbox;

    private void Awake()
    {
        // Search the whole enemy, not just children: the Animator sits on the model child, so
        // the hitbox is just as likely to be a sibling of this object as a descendant.
        EnemyBase enemy = GetComponentInParent<EnemyBase>();

        hitbox = enemy != null
            ? enemy.GetComponentInChildren<EnemyHitbox>()
            : GetComponentInChildren<EnemyHitbox>();

        if (hitbox == null)
            Debug.LogError($"EnemyAnimationEventRelay on '{name}': no EnemyHitbox found on this " +
                           "enemy, so animation events cannot deal damage.");
    }

    /// <summary>Animation event. Call at the frame the attack starts connecting.</summary>
    public void EnableHitbox()
    {
        if (hitbox != null)
            hitbox.EnableHitbox();
    }

    /// <summary>Animation event. Call at the frame the attack stops connecting.</summary>
    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.DisableHitbox();
    }
}
