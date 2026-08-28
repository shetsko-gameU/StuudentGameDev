using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add to the player alongside CurrencyTracker.
///
/// CurrencyTracker.OnCurrencyChanged fires on both Add and Spend with the same signature —
/// this compares each update against the last known total per currency so the pickup sound
/// only plays when a balance actually went up.
///
/// Setup:
///   1. Add this component to the Player, alongside CurrencyTracker.
///   2. Leave currencyTracker empty — it auto-finds on Awake.
///   3. Drag a SoundSO into pickupSound.
/// </summary>
public class CurrencyPickupSFXTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public CurrencyTracker currencyTracker;

    [Header("Sound")]
    public SoundSO pickupSound;

    private readonly Dictionary<CurrencySO, int> lastKnownAmounts = new Dictionary<CurrencySO, int>();

    private void Awake()
    {
        if (currencyTracker == null) currencyTracker = GetComponent<CurrencyTracker>();
        if (currencyTracker == null) Debug.LogError($"CurrencyPickupSFXTrigger on '{name}': CurrencyTracker missing.");
    }

    private void OnEnable()
    {
        if (currencyTracker != null)
            currencyTracker.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        if (currencyTracker != null)
            currencyTracker.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(CurrencySO currency, int newTotal)
    {
        lastKnownAmounts.TryGetValue(currency, out int previousTotal);
        lastKnownAmounts[currency] = newTotal;

        if (newTotal > previousTotal && pickupSound != null)
            SoundManager.Instance.PlaySFX(pickupSound);
    }
}
