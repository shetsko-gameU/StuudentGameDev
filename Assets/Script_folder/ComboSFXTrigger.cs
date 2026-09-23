using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner.
///
/// Plays a swing sound every time a hit lands (whether or not it connects with an enemy —
/// use HitSFXTrigger for impact-only sound), plus an optional finisher sound on the last
/// hit of a combo. On the last hit, only comboFinishSound plays (when assigned) so finishers
/// do not double-blast with swingSound.
///
/// Setup:
///   1. Add this component to the Player, alongside ComboRunner.
///   2. Leave comboRunner empty — it auto-finds on Awake.
///   3. Drag a SoundSO into swingSound (plays on every non-finisher hit) and, optionally,
///      comboFinishSound (replaces swing on the last hit of a combo).
/// </summary>
public class ComboSFXTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public ComboRunner comboRunner;

    [Header("Sounds")]
    public SoundSO swingSound;
    [Tooltip("Optional — plays instead of swingSound on the last hit of a combo.")]
    public SoundSO comboFinishSound;

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (comboRunner == null) Debug.LogError($"ComboSFXTrigger on '{name}': ComboRunner missing.");
    }

    private void OnEnable()
    {
        if (comboRunner == null) return;
        comboRunner.OnHitLanded += HandleHitLanded;
        comboRunner.OnComboFinished += HandleComboFinished;
    }

    private void OnDisable()
    {
        if (comboRunner == null) return;
        comboRunner.OnHitLanded -= HandleHitLanded;
        comboRunner.OnComboFinished -= HandleComboFinished;
    }

    private void HandleHitLanded(int hitIndex, float damage)
    {
        // No phantom swings when combat hitbox is missing / unwired.
        if (comboRunner == null || comboRunner.hitbox == null) return;

        bool isLast = comboRunner.combo != null &&
                      comboRunner.combo.hits != null &&
                      hitIndex >= comboRunner.combo.hits.Length - 1;

        // Last hit: finish stinger only (avoid swing + finish double blast).
        if (isLast && comboFinishSound != null)
            return;

        if (swingSound != null)
            SoundManager.Instance.PlaySFX(swingSound);
    }

    private void HandleComboFinished()
    {
        if (comboRunner == null || comboRunner.hitbox == null) return;

        if (comboFinishSound != null)
            SoundManager.Instance.PlaySFX(comboFinishSound);
    }
}
