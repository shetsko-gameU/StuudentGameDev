using UnityEngine;

/// <summary>
/// The single source of truth for NPC Animator parameter names. Every enemy Animator
/// Controller must expose exactly these four parameters, and only the enemy state machine
/// (EnemyIdle / EnemyMove / EnemyAttack / EnemyDeath) is allowed to write them.
///
/// This exists because the parameter names used to be scattered as raw strings across four
/// files, and none of them matched the controllers: scripts drove "Move", "isMoving",
/// "Attack" and "Death" while every enemy controller shipped with zero parameters at all,
/// so every single write silently did nothing. Referencing these constants instead means a
/// typo is a compile error rather than an animation that quietly never plays.
///
/// The Hashes are Animator.StringToHash values. Unity hashes the string on every SetBool /
/// SetTrigger call otherwise, and these run per-frame per-enemy.
///
/// Setup (per enemy Animator Controller):
///   1. Add the four parameters below, spelled exactly as the Name constants.
///   2. Speed (float) drives Idle to Walk. Attack and Hit are triggers. Dead is a bool.
///   3. Do not add transitions out of the Death state - EnemyDeath destroys the object.
/// </summary>
public static class EnemyAnimatorParams
{
    /// <summary>Float. Current horizontal NavMeshAgent speed in units/second. Drives the
    /// idle-to-walk blend. Written every frame by EnemyMove, and zeroed by EnemyIdle.</summary>
    public const string Speed = "Speed";

    /// <summary>Trigger. Fired once per swing by EnemyAttack. The attack clip carries the
    /// animation event that actually deals damage - see EnemyHitbox.</summary>
    public const string Attack = "Attack";

    /// <summary>Trigger. Fired when this enemy takes damage that was not dodged.</summary>
    public const string Hit = "Hit";

    /// <summary>Bool. Latched true by EnemyDeath and never cleared, since the GameObject is
    /// destroyed once the death clip finishes.</summary>
    public const string Dead = "Dead";

    public static readonly int SpeedHash = Animator.StringToHash(Speed);
    public static readonly int AttackHash = Animator.StringToHash(Attack);
    public static readonly int HitHash = Animator.StringToHash(Hit);
    public static readonly int DeadHash = Animator.StringToHash(Dead);
}
