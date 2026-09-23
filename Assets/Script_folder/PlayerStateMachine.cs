using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The states the player can be in. Ordered loosely by priority - see
/// PlayerStateMachine.EvaluateState.</summary>
public enum PlayerStateId
{
    Idle,
    Move,
    Attack,
    Dash,
    Consume,
    Death,
}

/// <summary>
/// The player's state machine. Deliberately a different shape from the NPC one
/// (EnemyStateMachine + EnemyState subclasses), because the two have different problems to
/// solve.
///
/// The NPC side needed states that DO things - nothing was driving enemy behaviour, so
/// EnemyIdle/EnemyMove/EnemyAttack/EnemyDeath own their logic outright.
///
/// The player side is the opposite. ComboRunner, AbilityRunner, PlayerConsume, PlayerMove
/// and PlayerDeathHandler already work and are already decoupled. Rewriting their logic
/// into state classes would be a large, risky change that deleted working code to arrive
/// back where it started. So this class coordinates rather than replaces: it derives one
/// authoritative state each frame from those systems, owns the animator parameters and the
/// transition rules between them, and gives everything else a single place to ask "what is
/// the player doing right now?".
///
/// What it actually owns:
///   * The Speed / Grounded / Dead animator parameters. Nothing else writes them.
///     (ComboRunner still owns its own per-hit attack triggers - see PlayerAnimatorParams
///     for why that exception exists.)
///   * Whether a dash is allowed to start, and the PlayerMove enable/disable around it.
///     That toggle used to live inside DashAbilitySO's coroutine while PlayerDeathHandler
///     also toggled the same flag, so dying mid-dash could re-enable movement on a corpse.
///   * The OnStateChanged event, so UI, SFX and VFX can react without polling.
///
/// Setup:
///   1. Add to the player prefab root, alongside StatsManager and PlayerMove.
///   2. Leave every reference empty - they are all auto-found in Awake.
///   3. Add Speed (float), Grounded (bool) and Dead (bool) to the player's Animator
///      Controller. Missing parameters are skipped silently, so this is safe to add before
///      the controller is ready.
/// </summary>
[RequireComponent(typeof(StatsManager))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    public StatsManager stats;
    public PlayerMove playerMove;
    public ComboRunner comboRunner;
    public PlayerConsume playerConsume;
    public Animator animator;

    [Header("Tuning")]
    [Tooltip("Horizontal speed above which the player counts as moving rather than idle. " +
             "Keep it small but non-zero - the NavMeshAgent's velocity rarely settles at exactly 0.")]
    public float moveSpeedThreshold = 0.1f;

    /// <summary>What the player is doing right now.</summary>
    public PlayerStateId CurrentState { get; private set; } = PlayerStateId.Idle;

    /// <summary>Fired as (previousState, newState) whenever the state actually changes.</summary>
    public event Action<PlayerStateId, PlayerStateId> OnStateChanged;

    /// <summary>True for the duration of a dash. Set by BeginDash/EndDash.</summary>
    public bool IsDashing { get; private set; }

    /// <summary>
    /// Which animator parameters the current controller actually has.
    ///
    /// Unity logs a warning every time you write a parameter a controller does not declare,
    /// and these are written every frame - so an incomplete controller would bury the
    /// console. Checking up front means a half-built controller degrades quietly instead.
    /// </summary>
    private readonly HashSet<string> availableParameters = new HashSet<string>();

    /// <summary>Set true once death has been applied, so the latch cannot be undone.</summary>
    private bool deathApplied;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<StatsManager>();
        if (playerMove == null) playerMove = GetComponent<PlayerMove>();
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (playerConsume == null) playerConsume = GetComponent<PlayerConsume>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        CacheAvailableParameters();
    }

    /// <summary>
    /// Points the state machine at a different Animator, for when the hub character swap replaces
    /// the player's mesh. The parameter list has to be re-read: each character's controller
    /// exposes its own set, and this class only writes parameters it knows exist.
    /// </summary>
    public void RebindAnimator(Animator newAnimator)
    {
        animator = newAnimator;
        CacheAvailableParameters();
    }

    private void CacheAvailableParameters()
    {
        availableParameters.Clear();

        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            availableParameters.Add(parameter.name);
    }

    private void Update()
    {
        PlayerStateId next = EvaluateState();

        if (next != CurrentState)
        {
            PlayerStateId previous = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(previous, next);
        }

        WriteAnimatorParameters();
    }

    /// <summary>
    /// Works out the single state that best describes the player this frame.
    ///
    /// Priority order matters and is the whole point of centralising this: death beats
    /// everything, a dash beats an attack (you can dash-cancel), consuming beats attacking,
    /// and movement is only reported when nothing more specific is happening. Previously
    /// each system answered this question for itself and they could disagree.
    /// </summary>
    private PlayerStateId EvaluateState()
    {
        if (stats != null && stats.IsDead) return PlayerStateId.Death;

        if (IsDashing) return PlayerStateId.Dash;

        if (playerConsume != null && playerConsume.IsConsuming) return PlayerStateId.Consume;

        if (comboRunner != null && comboRunner.enabled && comboRunner.IsAttacking) return PlayerStateId.Attack;

        return CurrentHorizontalSpeed() > moveSpeedThreshold
            ? PlayerStateId.Move
            : PlayerStateId.Idle;
    }

    /// <summary>
    /// The player's speed across the ground. Uses PlayerMove's gameplay velocity (what
    /// agent.Move actually drives). agent.velocity is unreliable with manual Move() and
    /// caused Walk to play once then never re-enter. Falls back to rb while ledge-falling.
    /// </summary>
    private float CurrentHorizontalSpeed()
    {
        if (playerMove == null) return 0f;

        if (!playerMove.IsFalling)
            return playerMove.HorizontalSpeed;

        if (playerMove.rb != null)
        {
            Vector3 velocity = playerMove.rb.linearVelocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            return float.IsFinite(speed) ? speed : 0f;
        }

        return playerMove.HorizontalSpeed;
    }

    private void WriteAnimatorParameters()
    {
        if (animator == null) return;

        SetFloatIfPresent(PlayerAnimatorParams.Speed, PlayerAnimatorParams.SpeedHash, CurrentHorizontalSpeed());

        bool grounded = playerMove == null || !playerMove.IsFalling;
        SetBoolIfPresent(PlayerAnimatorParams.Grounded, PlayerAnimatorParams.GroundedHash, grounded);

        // Latched: once dead, stays dead. Written once rather than every frame so the death
        // state cannot be re-entered.
        if (!deathApplied && CurrentState == PlayerStateId.Death)
        {
            deathApplied = true;
            SetBoolIfPresent(PlayerAnimatorParams.Dead, PlayerAnimatorParams.DeadHash, true);
        }
    }

    // ------------------------------------------------------------------ Dash ownership

    /// <summary>
    /// Whether a dash may start right now. DashAbilitySO asks this instead of deciding for
    /// itself, so dash rules live in one place.
    /// </summary>
    public bool CanEnterDash()
    {
        if (stats != null && stats.IsDead) return false;
        if (IsDashing) return false;

        // Dashing out of an attack or a bite of food is a design decision, not an accident -
        // flip these to allow dash-cancelling.
        if (CurrentState == PlayerStateId.Attack) return false;
        if (CurrentState == PlayerStateId.Consume) return false;

        return true;
    }

    /// <summary>
    /// Starts a dash and takes movement control away from PlayerMove for its duration.
    /// Always pair with EndDash - DashAbilitySO does this in a try/finally so an early exit
    /// cannot strand the player with movement disabled.
    /// </summary>
    public void BeginDash()
    {
        IsDashing = true;

        if (playerMove != null)
            playerMove.enabled = false;
    }

    /// <summary>
    /// Ends a dash and hands movement back - unless the player died mid-dash, in which case
    /// PlayerDeathHandler owns the disabled state and we must not undo it. This exact case
    /// used to re-enable movement on a corpse.
    /// </summary>
    public void EndDash()
    {
        IsDashing = false;

        bool isDead = stats != null && stats.IsDead;

        if (playerMove != null && !isDead)
            playerMove.enabled = true;
    }

    // ------------------------------------------------------------------ Death

    /// <summary>
    /// Applies the death animator state. Called by PlayerDeathHandler so that death is
    /// expressed through the same parameter contract as everything else, rather than through
    /// a separate trigger write that bypassed the state machine.
    /// </summary>
    public void ApplyDeath()
    {
        CurrentState = PlayerStateId.Death;

        if (!deathApplied)
        {
            deathApplied = true;
            SetBoolIfPresent(PlayerAnimatorParams.Dead, PlayerAnimatorParams.DeadHash, true);
        }
    }

    // ------------------------------------------------------------------ Guarded writes

    private void SetFloatIfPresent(string parameterName, int parameterHash, float value)
    {
        if (availableParameters.Contains(parameterName))
            animator.SetFloat(parameterHash, value);
    }

    private void SetBoolIfPresent(string parameterName, int parameterHash, bool value)
    {
        if (availableParameters.Contains(parameterName))
            animator.SetBool(parameterHash, value);
    }
}
