using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The enemy side of the damage pipeline - the mirror image of the player's AttackHitbox.
/// Put it on a child GameObject positioned where the attack actually connects (the claw,
/// the mouth, the body for a slam) with a trigger collider on it.
///
/// Before this existed, enemies could not damage the player at all. EnemyAttack fired an
/// animator trigger and nothing else; EnemyBase.attacks held a list of AttackBase
/// ScriptableObjects whose Execute() method was never called from anywhere; and the player
/// carried a legacy TakeDamage component that listened for objects tagged "EnemyAttack"
/// carrying a WizardProjectiles component - a player script - which no enemy ever used.
/// This routes enemy damage through the same StatsManager.TakeDamage call the player's
/// attacks use, so dodge, armour and death all behave identically in both directions.
///
/// Damage comes from the enemy's own StatsManager Attack stat, so stat modifiers on the
/// enemy (elite buffs, debuffs) are respected automatically.
///
/// Setup:
///   1. Add a child GameObject to the enemy where the attack lands. Add a collider and tick
///      Is Trigger (this script forces it on and starts it disabled).
///   2. Add this component. Set Player Layer to whichever layer the player is on.
///   3. Leave Attacker Stats empty - it auto-finds the enemy's StatsManager in a parent.
///   4. Add EnableHitbox / DisableHitbox animation events to the attack clip. If the
///      Animator is on a different GameObject than this one (it usually is - the Animator
///      sits on the nested model), put EnemyAnimationEventRelay on the Animator's object.
///   5. If the enemy has no attack animation yet, tick Use Swing Window and the hitbox will
///      open for Swing Window Seconds whenever EnemyAttack swings, with no animation events
///      needed. Turn it off once real animation events exist.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyHitbox : MonoBehaviour
{
    [Tooltip("Which layer the player is on. Only objects on this layer can be damaged.")]
    public LayerMask playerLayer;

    [Tooltip("This enemy's StatsManager, used for the damage roll. Leave empty to auto-find it in a parent.")]
    public StatsManager attackerStats;

    [Header("Damage")]
    [Tooltip("Multiplier applied to the enemy's Attack stat for this hitbox. Use it to make a " +
             "heavy slam hit harder than a light swipe on the same enemy.")]
    public float damageMultiplier = 1f;

    [Header("No-animation fallback")]
    [Tooltip("Open the hitbox on a timer when EnemyAttack swings, instead of waiting for animation " +
             "events. Lets enemies deal damage before their attack animations exist. Turn this off " +
             "once the attack clip has real EnableHitbox/DisableHitbox events.")]
    public bool useSwingWindow = true;

    [Tooltip("How long the hitbox stays open when Use Swing Window is on.")]
    public float swingWindowSeconds = 0.2f;

    private Collider hitboxCollider;

    /// <summary>
    /// Everything already hit during the current swing. Cleared when the hitbox opens.
    ///
    /// Without this, one swing can damage the player several times: a trigger that stays
    /// open across frames re-reports overlaps, and an enemy with more than one collider on
    /// the player hits once per collider. The player's AttackHitbox has this same
    /// multi-hit weakness; it is worth not reproducing it here.
    /// </summary>
    private readonly HashSet<StatsManager> alreadyHitThisSwing = new HashSet<StatsManager>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        if (attackerStats == null)
            attackerStats = GetComponentInParent<StatsManager>();

        if (attackerStats == null)
            Debug.LogError($"EnemyHitbox on '{name}': no StatsManager found in parents, so this " +
                           "hitbox has no damage value and will never hurt the player. Assign " +
                           "Attacker Stats or add a StatsManager to the enemy root.");

        if (playerLayer.value == 0)
            Debug.LogWarning($"EnemyHitbox on '{name}': Player Layer is empty, so nothing can be " +
                             "damaged. Set it to the player's layer.");
    }

    // ------------------------------------------------------------------ Animation Event methods

    /// <summary>Call at the frame the attack starts connecting.</summary>
    public void EnableHitbox()
    {
        alreadyHitThisSwing.Clear();
        hitboxCollider.enabled = true;
    }

    /// <summary>Call at the frame the attack stops connecting.</summary>
    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }

    // ------------------------------------------------------------------ Called by EnemyAttack

    /// <summary>
    /// Opens the hitbox for swingWindowSeconds. Called by EnemyAttack when useSwingWindow is
    /// on, so an enemy with no attack animation events can still land hits.
    /// </summary>
    public void FireSwingWindow()
    {
        if (!useSwingWindow) return;

        EnableHitbox();
        CancelInvoke(nameof(DisableHitbox));
        Invoke(nameof(DisableHitbox), swingWindowSeconds);
    }

    // ------------------------------------------------------------------ Collision

    private void OnTriggerEnter(Collider other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        StatsManager targetStats = other.GetComponent<StatsManager>()
                                ?? other.GetComponentInParent<StatsManager>();

        if (targetStats == null) return;
        if (!alreadyHitThisSwing.Add(targetStats)) return;   // already hit by this swing

        float damage = (attackerStats != null ? attackerStats.GetDamageRoll() : 0f) * damageMultiplier;

        targetStats.TakeDamage(damage, attackerStats);
    }
}
