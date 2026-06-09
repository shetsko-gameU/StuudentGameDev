using UnityEngine;

/// <summary>
/// Add this to the face mesh alongside the Animator.
///
/// Animation Events can only call methods on scripts that are on the same
/// GameObject as the Animator. This relay script sits there and forwards
/// calls up to ComboRunner on the parent player root.
///
/// To add an animation event in Unity:
///   1. Open the Animation window (not Animator).
///   2. Select the attack clip.
///   3. Scrub to the frame where the weapon connects.
///   4. Click the Add Event button on the timeline.
///   5. In the Function dropdown pick EnableHitbox or DisableHitbox.
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    private ComboRunner comboRunner;
    private AttackHitbox hitbox;

    private void Awake()
    {
        // Look up to the parent for ComboRunner
        comboRunner = GetComponentInParent<ComboRunner>();
        hitbox = GetComponentInParent<AttackHitbox>();

        if (comboRunner == null)
            Debug.LogError($"AnimationEventRelay on '{name}': No ComboRunner found in parent.");

        if (hitbox == null)
            Debug.LogError($"AnimationEventRelay on '{name}': No AttackHitbox found in parent.");
    }

    // ------------------------------------------------------------------ Animation Event methods
    // These show up in the Animation Event dropdown because this script
    // is on the same GameObject as the Animator.

    /// <summary>
    /// Call this at the frame the weapon starts swinging.
    /// Enables the hitbox so it can hit enemies.
    /// </summary>
    public void EnableHitbox()
    {
        if (hitbox != null)
            hitbox.SetActive(true);
    }

    /// <summary>
    /// Call this at the frame the weapon swing ends.
    /// Disables the hitbox so it stops hitting enemies.
    /// </summary>
    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.SetActive(false);
    }
}