using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Replaces PlayerAttack. Add to the player alongside StatsManager.
///
/// How it works:
///   1. Player presses attack — OnAttack() is called from the Input System.
///   2. ComboRunner fires the current hit (animation + damage after hitCheckDelay).
///   3. A chain window opens — player has chainWindowSeconds to press again.
///   4. If they press in time → next hit fires, window resets.
///   5. If they don't press in time → combo resets to hit 0.
///   6. Input pressed during an active hit is buffered and fires as soon as the hit resolves.
///
/// Events you can subscribe to:
///   OnComboStarted          — first hit of a new combo began
///   OnHitLanded(int, float) — any hit landed (index, damage dealt)
///   OnComboFinished         — last hit of the combo landed
///   OnComboReset            — chain window expired without input
///
/// Checking first / last hit:
///   comboRunner.IsFirstHit  — true while the first hit is active
///   comboRunner.IsLastHit   — true while the last hit is active
/// </summary>
public class ComboRunner : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector

    [Header("References")]
    [Tooltip("The player's StatsManager. Auto-found if left empty.")]
    public StatsManager stats;

    [Tooltip("The AttackHitbox on the weapon/hand child object.")]
    public AttackHitbox hitbox;

    [Tooltip("The player's Animator.")]
    public Animator animator;

    [Header("Combo")]
    [Tooltip("The ComboSO asset that defines this combo's hits.")]
    public ComboSO combo;

    [Tooltip("If true: animation events on the face mesh control when the hitbox fires. " +
             "ComboRunner sets the damage but does NOT call FireHit. " +
             "If false: ComboRunner fires the hitbox itself after hitCheckDelay.")]
    public bool useAnimationEvents = false;

    // ------------------------------------------------------------------ Events

    /// <summary>Fires when the first hit of a new combo begins.</summary>
    public event Action OnComboStarted;

    /// <summary>Fires every time a hit actually lands. (hitIndex, damage)</summary>
    public event Action<int, float> OnHitLanded;

    /// <summary>Fires after the last hit of the combo lands.</summary>
    public event Action OnComboFinished;

    /// <summary>Fires when the chain window expires without input — combo dropped.</summary>
    public event Action OnComboReset;

    // ------------------------------------------------------------------ Properties

    /// <summary>True while the combo is on its first hit.</summary>
    public bool IsFirstHit => currentHitIndex == 0;

    /// <summary>True while the combo is on its last hit.</summary>
    public bool IsLastHit => combo != null && currentHitIndex == combo.hits.Length - 1;

    /// <summary>Which hit in the combo we are currently on (0 = first).</summary>
    public int CurrentHitIndex => currentHitIndex;

    // ------------------------------------------------------------------ Runtime state

    private int currentHitIndex = 0;
    private float chainTimer = 0f;
    private bool inChainWindow = false;
    private bool hitActive = false;
    private bool attackBuffered = false;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (stats == null)
            Debug.LogError($"ComboRunner on '{name}': No StatsManager found.");

        if (hitbox == null)
            Debug.LogError($"ComboRunner on '{name}': No AttackHitbox assigned.");

        if (combo == null)
            Debug.LogError($"ComboRunner on '{name}': No ComboSO assigned.");
    }

    private void Update()
    {
        // Count down the chain window
        if (inChainWindow)
        {
            chainTimer += Time.deltaTime;

            float windowDuration = combo.hits[currentHitIndex > 0 ? currentHitIndex - 1 : 0].chainWindowSeconds;

            if (chainTimer >= windowDuration)
            {
                // Player didn't press in time — reset the combo
                inChainWindow = false;
                currentHitIndex = 0;
                chainTimer = 0f;
                resetTriggers();
                OnComboReset?.Invoke();
                Debug.Log("ComboRunner: Chain window expired — combo reset.");
            }
        }

        // Fire buffered input as soon as the current hit resolves
        if (attackBuffered && !hitActive)
        {
            attackBuffered = false;
            TriggerCurrentHit();
        }
    }

    // ------------------------------------------------------------------ Input

    /// <summary>
    /// Wire this to the Attack action in the Input System (same as OnAttack on PlayerAttack).
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!hitActive)
        {
            TriggerCurrentHit();
        }
        else
        {
            // Hit is still active — buffer it so it fires as soon as possible
            attackBuffered = true;
        }
    }

    // ------------------------------------------------------------------ Core logic

    private void TriggerCurrentHit()
    {
        if (combo == null || combo.hits == null || combo.hits.Length == 0) return;

        ComboHitData hitData = combo.hits[currentHitIndex];
        bool isFirst = IsFirstHit;
        bool isLast = IsLastHit;
        int hitIndex = currentHitIndex;
        float damage = stats != null
            ? stats.Attack * hitData.damageMultiplier
            : hitData.damageMultiplier;

        // Tell the hitbox what damage to deal before the animation fires.
        // This way when AnimationEventRelay calls EnableHitbox the damage is already set.
        if (hitbox != null)
            hitbox.SetDamage(damage);

        // Fire animation trigger
        if (animator != null && !string.IsNullOrEmpty(hitData.animatorTrigger))
            animator.SetTrigger(hitData.animatorTrigger);

        // Fire combo started event on the first hit.
        // Guard: if this is also the last hit (single-hit combo) skip OnComboStarted
        // so ComboPassiveTrigger doesn't apply first-hit passives AND last-hit passives
        // in the same frame, which would double-apply any passive flagged for both.
        if (isFirst && !isLast)
        {
            OnComboStarted?.Invoke();
            Debug.Log($"ComboRunner: Combo started — '{hitData.displayName}'");
        }

        // Advance or reset the combo index
        if (isLast)
        {
            currentHitIndex = 0;
            inChainWindow = false;
            chainTimer = 0f;
            resetTriggers();
        }
        else
        {
            currentHitIndex++;
            inChainWindow = true;
            chainTimer = 0f;
        }

        // Fire the hit after the delay — this is where damage is dealt
        StartCoroutine(ResolveHit(hitData, hitIndex, damage, isFirst, isLast));
    }

    private IEnumerator ResolveHit(ComboHitData hitData, int hitIndex, float damage, bool isFirst, bool isLast)
    {
        hitActive = true;

        // Wait for the hit check delay (sync with swing animation)
        if (hitData.hitCheckDelay > 0f)
            yield return new WaitForSeconds(hitData.hitCheckDelay);

        // Deal damage via the hitbox — only if not using animation events.
        // When useAnimationEvents is true the AnimationEventRelay handles
        // enabling the hitbox at the right frame. Damage is already set
        // via SetDamage() above so OnTriggerEnter will use the correct value.
        if (!useAnimationEvents && hitbox != null)
            hitbox.FireHit(damage);

        // Fire events
        OnHitLanded?.Invoke(hitIndex, damage);

        if (isLast)
        {
            OnComboFinished?.Invoke();
            Debug.Log($"ComboRunner: Combo finished — last hit '{hitData.displayName}' dealt {damage} damage.");
        }
        else
        {
            Debug.Log($"ComboRunner: Hit {hitIndex} '{hitData.displayName}' dealt {damage} damage.");
        }

        hitActive = false;
    }
    public void resetTriggers()
    {
        foreach(ComboHitData comboHit in combo.hits)
        {
            animator.ResetTrigger(comboHit.animatorTrigger);
        }

        

    }
}