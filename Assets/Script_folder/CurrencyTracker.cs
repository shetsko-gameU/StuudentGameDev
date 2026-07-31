using System;
using UnityEngine;

/// <summary>
/// Attach to the player. Keeps a running total of each CurrencySO type collected.
/// Other systems (HUD, shops) subscribe to OnCurrencyChanged to react to updates.
///
/// Usage:
///   tracker.Add(currencySO, amount)   — add currency (e.g. from a pickup)
///   tracker.Spend(currencySO, amount) — returns true and deducts if player can afford it
///   tracker.GetAmount(currencySO)     — read current balance
/// </summary>
public class CurrencyTracker : MonoBehaviour
{
    /// <summary>Fires whenever any currency balance changes. (currencyType, newTotal)</summary>
    public event Action<CurrencySO, int> OnCurrencyChanged;

    // Parallel arrays — kept serializable so balances are visible in the Inspector
    [Header("Balances (Read Only)")]
    [SerializeField] private CurrencySO[] trackedTypes = new CurrencySO[0];
    [SerializeField] private int[]        amounts      = new int[0];

    // ------------------------------------------------------------------ Public API

    /// <summary>Add <paramref name="amount"/> of <paramref name="currency"/> to the tracker.</summary>
    public void Add(CurrencySO currency, int amount)
    {
        if (currency == null || amount <= 0) return;

        int index = IndexOf(currency);
        if (index == -1)
            index = AddNewEntry(currency);

        amounts[index] += amount;
        OnCurrencyChanged?.Invoke(currency, amounts[index]);
        Debug.Log($"CurrencyTracker: +{amount} {currency.displayName}  (total: {amounts[index]})");
    }

    /// <summary>
    /// Attempt to spend <paramref name="amount"/> of <paramref name="currency"/>.
    /// Returns true and deducts the amount if the player can afford it; otherwise returns false.
    /// </summary>
    public bool Spend(CurrencySO currency, int amount)
    {
        if (currency == null || amount <= 0) return false;

        int index = IndexOf(currency);
        if (index == -1 || amounts[index] < amount)
        {
            Debug.Log($"CurrencyTracker: Cannot afford {amount} {currency.displayName} " +
                      $"(have {(index == -1 ? 0 : amounts[index])})");
            return false;
        }

        amounts[index] -= amount;
        OnCurrencyChanged?.Invoke(currency, amounts[index]);
        Debug.Log($"CurrencyTracker: -{amount} {currency.displayName}  (total: {amounts[index]})");
        return true;
    }

    /// <summary>Returns the current balance for <paramref name="currency"/>, or 0 if never collected.</summary>
    public int GetAmount(CurrencySO currency)
    {
        if (currency == null) return 0;
        int index = IndexOf(currency);
        return index == -1 ? 0 : amounts[index];
    }

    // ------------------------------------------------------------------ Internals

    private int IndexOf(CurrencySO currency)
    {
        for (int i = 0; i < trackedTypes.Length; i++)
            if (trackedTypes[i] == currency) return i;
        return -1;
    }

    private int AddNewEntry(CurrencySO currency)
    {
        // Grow both arrays by 1
        int newLen = trackedTypes.Length + 1;

        CurrencySO[] newTypes   = new CurrencySO[newLen];
        int[]        newAmounts = new int[newLen];

        for (int i = 0; i < trackedTypes.Length; i++)
        {
            newTypes[i]   = trackedTypes[i];
            newAmounts[i] = amounts[i];
        }

        newTypes[newLen - 1]   = currency;
        newAmounts[newLen - 1] = 0;

        trackedTypes = newTypes;
        amounts      = newAmounts;

        return newLen - 1;
    }
}
