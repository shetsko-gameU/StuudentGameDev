using System;
using System.Collections.Generic;
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

    /// <summary>Every currently tracked (currency, amount) pair — used to snapshot balances across a scene load.</summary>
    public IEnumerable<(CurrencySO currency, int amount)> GetAllBalances()
    {
        for (int i = 0; i < trackedTypes.Length; i++)
            if (trackedTypes[i] != null)
                yield return (trackedTypes[i], amounts[i]);
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
        // Grow both arrays by 1 — keep lengths matched (corrupt Inspector state can leave amounts shorter).
        int oldLen = trackedTypes != null ? trackedTypes.Length : 0;
        int amountLen = amounts != null ? amounts.Length : 0;
        int newLen = oldLen + 1;

        CurrencySO[] newTypes = new CurrencySO[newLen];
        int[] newAmounts = new int[newLen];

        for (int i = 0; i < oldLen; i++)
        {
            newTypes[i] = trackedTypes[i];
            newAmounts[i] = i < amountLen ? amounts[i] : 0;
        }

        newTypes[newLen - 1] = currency;
        newAmounts[newLen - 1] = 0;

        trackedTypes = newTypes;
        amounts = newAmounts;

        return newLen - 1;
    }

    private void OnValidate()
    {
        // Strip null currency slots and keep amounts parallel.
        if (trackedTypes == null)
        {
            trackedTypes = new CurrencySO[0];
            amounts = new int[0];
            return;
        }

        int valid = 0;
        for (int i = 0; i < trackedTypes.Length; i++)
            if (trackedTypes[i] != null) valid++;

        if (valid == trackedTypes.Length && amounts != null && amounts.Length == trackedTypes.Length)
            return;

        CurrencySO[] cleanedTypes = new CurrencySO[valid];
        int[] cleanedAmounts = new int[valid];
        int w = 0;
        for (int i = 0; i < trackedTypes.Length; i++)
        {
            if (trackedTypes[i] == null) continue;
            cleanedTypes[w] = trackedTypes[i];
            cleanedAmounts[w] = (amounts != null && i < amounts.Length) ? amounts[i] : 0;
            w++;
        }

        trackedTypes = cleanedTypes;
        amounts = cleanedAmounts;
    }
}
