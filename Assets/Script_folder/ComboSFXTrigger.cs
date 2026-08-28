using UnityEngine;

/// <summary>
/// Add to the player alongside ComboRunner.
///
/// Plays a swing sound every time a hit lands (whether or not it connects with an enemy —
/// use HitSFXTrigger for impact-only sound), plus an optional finisher sound on the last
/// hit of a combo. Both fire on the last hit — comboFinishSound is meant to layer a
/// stinger on top of swingSound, not replace it.
/// </summary>
public class ComboSFXTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public ComboRunner comboRunner;

    [Header("Sounds")]
    public SoundSO swingSound;
    [Tooltip("Optional — layers on top of swingSound on the last hit of a combo.")]
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
        if (swingSound != null)
            SoundManager.Instance.PlaySFX(swingSound);
    }

    private void HandleComboFinished()
    {
        if (comboFinishSound != null)
            SoundManager.Instance.PlaySFX(comboFinishSound);
    }
}
