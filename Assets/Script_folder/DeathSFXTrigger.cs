using UnityEngine;

/// <summary>
/// Place once anywhere in the scene (e.g. alongside SoundManager or the Player) — no
/// per-entity setup needed.
///
/// Listens to the global StatsManager.OnAnyDied event and plays a positional death sound
/// at whoever died — playerDeathSound if the victim is tagged "Player", enemyDeathSound
/// otherwise.
///
/// Setup:
///   1. Add this component to any single GameObject in the scene — it doesn't need to be
///      on the Player or an enemy (e.g. the same object as SoundManager, or an empty
///      "SFXTriggers" GameObject). Only one instance is needed per scene.
///   2. Drag a SoundSO into playerDeathSound and/or enemyDeathSound.
/// </summary>
public class DeathSFXTrigger : MonoBehaviour
{
    [Header("Sounds")]
    public SoundSO playerDeathSound;
    public SoundSO enemyDeathSound;

    // OnAnyDied is STATIC — it outlives this object and the scene. Unsubscribing in
    // OnDisable is what keeps a destroyed object from being called back.
    private void OnEnable() => StatsManager.OnAnyDied += HandleAnyDied;
    private void OnDisable() => StatsManager.OnAnyDied -= HandleAnyDied;

    private void HandleAnyDied(StatsManager victim, StatsManager killer)
    {
        if (victim == null) return;

        SoundSO sound = victim.gameObject.CompareTag("Player") ? playerDeathSound : enemyDeathSound;
        if (sound == null) return;

        SoundManager.Instance.PlaySFXAtPoint(sound, victim.transform.position);
    }
}
