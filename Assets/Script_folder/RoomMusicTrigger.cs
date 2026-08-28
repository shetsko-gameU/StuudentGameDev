using UnityEngine;

/// <summary>
/// Add alongside (or near) EnemyManager, same setup as RoomExit.
///
/// Swaps to combat music the instant the player triggers EnemyManager (OnCombatStarted),
/// then crossfades back to exploration music once the room's last wave clears
/// (EnemyManager.OnAllWavesCleared) — the same event RoomExit uses to reveal the portal.
///
/// Setup:
///   1. Add this component to the same GameObject as EnemyManager (the object RoomExit
///      also lives on, if this room has one).
///   2. Leave enemyManager empty — it auto-finds on Awake.
///   3. Drag an AudioClip into combatMusic and/or explorationMusic — either can be left
///      empty to skip that half of the swap.
///   4. Adjust fadeSeconds to taste.
/// </summary>
public class RoomMusicTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public EnemyManager enemyManager;

    [Header("Music")]
    public AudioClip combatMusic;
    public AudioClip explorationMusic;
    public float fadeSeconds = 1.5f;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = GetComponent<EnemyManager>();

        if (enemyManager == null)
            Debug.LogError($"RoomMusicTrigger on '{name}': No EnemyManager found.");
    }

    private void OnEnable()
    {
        if (enemyManager == null) return;
        enemyManager.OnCombatStarted += HandleCombatStarted;
        enemyManager.OnAllWavesCleared += HandleWavesCleared;
    }

    private void OnDisable()
    {
        if (enemyManager == null) return;
        enemyManager.OnCombatStarted -= HandleCombatStarted;
        enemyManager.OnAllWavesCleared -= HandleWavesCleared;
    }

    private void HandleCombatStarted()
    {
        if (combatMusic != null)
            SoundManager.Instance.PlayMusic(combatMusic, fadeSeconds);
    }

    private void HandleWavesCleared()
    {
        if (explorationMusic != null)
            SoundManager.Instance.PlayMusic(explorationMusic, fadeSeconds);
    }
}
