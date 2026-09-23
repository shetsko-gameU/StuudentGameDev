using UnityEngine;

/// <summary>
/// Chase tuning for one enemy prefab, shown as a foldout on EnemyBase.
///
/// These values used to live as public fields on EnemyMove. That looked correct but could
/// never work: EnemyMove is a plain C# state object constructed in code, not a
/// MonoBehaviour, so Unity never serialized those fields and they all sat at 0 - meaning
/// every enemy had zero speed and zero acceleration no matter what the Inspector suggested.
/// Holding them here, on a [Serializable] class owned by the MonoBehaviour, is what makes
/// them genuinely designer-editable.
///
/// Setup: expand "Move Tuning" on EnemyBase and set these per enemy prefab. moveSpeed is a
/// base value - the StatsManager MoveSpeed stat multiplies it at runtime, so slows and
/// haste work.
/// </summary>
[System.Serializable]
public class EnemyMoveTuning
{
    [Tooltip("Base chase speed in units/second, before the StatsManager MoveSpeed multiplier. " +
             "Written onto NavMeshAgent.speed every frame.")]
    public float moveSpeed = 3.5f;

    [Tooltip("How hard the agent accelerates toward its chase speed. Low values feel floaty, " +
             "high values feel snappy and robotic.")]
    public float acceleration = 8f;

    [Tooltip("Turn rate in degrees/second. The agent rotates itself toward its path.")]
    public float angularSpeed = 360f;

    [Tooltip("How close the agent gets to its destination before it stops. Keep this below " +
             "the EnemyStrikeDistanceCheck radius or the enemy stops before it can attack.")]
    public float stoppingDistance = 1f;

    [Tooltip("Seconds between destination updates while chasing. The agent keeps following " +
             "its existing path in between, so this can be fairly coarse. 0.15-0.25 is " +
             "usually indistinguishable from repathing every frame and much cheaper.")]
    public float repathInterval = 0.2f;
}

/// <summary>
/// Attack timing for one enemy prefab, shown as a foldout on EnemyBase.
///
/// Same story as EnemyMoveTuning: these were hardcoded local fields inside EnemyAttack
/// (timeBetweenAttacks = 1f, timeToExitAfterAttack = 2f, distanceToCountExit = 3f), so
/// every enemy in the game attacked on exactly the same rhythm and no designer could
/// change it without editing code.
///
/// Setup: expand "Attack Tuning" on EnemyBase and set these per enemy prefab.
/// </summary>
[System.Serializable]
public class EnemyAttackTuning
{
    [Tooltip("Seconds between attack swings. The Attack animator trigger fires once per swing.")]
    public float timeBetweenAttacks = 1f;

    [Tooltip("How long the target must stay beyond Distance To Count Exit before this enemy " +
             "gives up attacking and returns to chasing.")]
    public float timeToExitAfterAttack = 2f;

    [Tooltip("Distance at which the target counts as having escaped, which starts the exit timer.")]
    public float distanceToCountExit = 3f;

    [Tooltip("Degrees/second this enemy circles its target between swings. Only used when " +
             "Does Attack Pivot is ticked on EnemyBase.")]
    public float pivotSpeed = 45f;
}
