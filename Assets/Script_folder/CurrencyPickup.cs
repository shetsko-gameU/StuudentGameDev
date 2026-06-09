using UnityEngine;

/// <summary>
/// Place on a pickup prefab alongside a trigger Collider.
/// When the player walks over it the amount is added to their CurrencyTracker
/// and the GameObject is destroyed.
///
/// Set up a prefab per currency type (e.g. "CoinPickup") and drag the
/// matching CurrencySO into the currency field.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CurrencyPickup : MonoBehaviour
{
    [Tooltip("Which currency type this pickup gives.")]
    public CurrencySO currency;

    [Min(1)]
    [Tooltip("How much currency this pickup is worth.")]
    public int amount = 1;

    private void Awake()
    {
        // Ensure the collider is a trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CurrencyTracker tracker = other.GetComponent<CurrencyTracker>()
                               ?? other.GetComponentInParent<CurrencyTracker>();

        if (tracker == null)
        {
            Debug.LogWarning($"CurrencyPickup: Player has no CurrencyTracker component.");
            return;
        }

        if (currency == null)
        {
            Debug.LogWarning($"CurrencyPickup on '{name}': No CurrencySO assigned.");
            return;
        }

        tracker.Add(currency, amount);
        Destroy(gameObject);
    }
}
