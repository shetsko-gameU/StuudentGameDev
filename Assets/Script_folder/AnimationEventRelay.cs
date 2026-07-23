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
        comboRunner = GetComponentInParent<ComboRunner>();

        if (comboRunner == null)
            Debug.LogError($"AnimationEventRelay on '{name}': No ComboRunner found.");

        // The hitbox is on the Warrior Blade (a child), not a parent — can't use
        // GetComponentInParent. Pull the reference from ComboRunner instead.
        hitbox = comboRunner != null ? comboRunner.hitbox : null;

        if (hitbox == null)
            Debug.LogError($"AnimationEventRelay on '{name}': No AttackHitbox found — make sure ComboRunner has its Hitbox field assigned in the Inspector.");
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