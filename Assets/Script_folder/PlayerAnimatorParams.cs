using UnityEngine;

/// <summary>
/// The single source of truth for PLAYER Animator parameter names. The enemy equivalent is
/// EnemyAnimatorParams; the two are deliberately kept separate because the player and NPCs
/// run separate state machines with different needs.
///
/// Ownership rules - these matter, because scattered animator writes are what made the old
/// animation wiring impossible to reason about:
///
///   * PlayerStateMachine is the ONLY thing that writes Speed, Grounded and Dead.
///   * ComboRunner keeps ownership of its own per-hit attack triggers. Those trigger names
///     come from ComboSO data (each ComboHitData carries its own animatorTrigger), which is
///     the whole point of the data-driven combo system, so they cannot live here as
///     constants. PlayerStateMachine reads ComboRunner's state instead of writing it.
///   * PlayerDeathHandler no longer fires its own death trigger - it sets Dead through the
///     state machine, so death cannot be half-applied by two systems at once.
///
/// Setup (per player Animator Controller):
///   1. Add Speed (float), Grounded (bool) and Dead (bool).
///   2. Add whatever trigger names your ComboSO hits reference - "Attack" on Player, and
///      "WizardAttack" on Player_Wizard.
///   3. Any parameter listed here that the controller does not have is simply skipped at
///      runtime rather than logged as an error, so a partially set up controller still runs.
/// </summary>
public static class PlayerAnimatorParams
{
    /// <summary>Float. The player's current horizontal speed in units/second, for the
    /// idle-to-run blend.</summary>
    public const string Speed = "Speed";

    /// <summary>Bool. False while PlayerMove's ledge-fall system has the player airborne.</summary>
    public const string Grounded = "Grounded";

    /// <summary>Bool. Latched true on death and never cleared.</summary>
    public const string Dead = "Dead";

    public static readonly int SpeedHash = Animator.StringToHash(Speed);
    public static readonly int GroundedHash = Animator.StringToHash(Grounded);
    public static readonly int DeadHash = Animator.StringToHash(Dead);
}
