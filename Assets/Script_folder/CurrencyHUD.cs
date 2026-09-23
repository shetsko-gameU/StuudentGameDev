using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a UI GameObject. Displays the balance of one CurrencySO type.
/// Subscribes to CurrencyTracker.OnCurrencyChanged and updates automatically.
/// </summary>
public class CurrencyHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's CurrencyTracker. Auto-found by tag if left empty.")]
    public CurrencyTracker tracker;

    [Tooltip("Which currency type to display.")]
    public CurrencySO currency;

    [Header("UI Elements")]
    [Tooltip("TextMeshPro label that shows the amount.")]
    public TextMeshProUGUI amountText;

    [Tooltip("(Optional) RawImage for the currency icon.")]
    public RawImage iconImage;

    private bool subscribed;

    private void Awake()
    {
        TryBind(logIfMissing: false);
    }

    private void OnEnable()
    {
        TryBind(logIfMissing: false);
        Subscribe();
    }

    private void Start()
    {
        TryBind(logIfMissing: true);
        Subscribe();

        if (iconImage != null && currency != null && currency.icon != null)
            iconImage.texture = currency.icon;

        Refresh();

        if (tracker == null || currency == null)
            StartCoroutine(RetryBindNextFrame());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private System.Collections.IEnumerator RetryBindNextFrame()
    {
        yield return null;
        TryBind(logIfMissing: true);
        Subscribe();
        Refresh();
    }

    private void TryBind(bool logIfMissing)
    {
        if (tracker == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                tracker = player.GetComponent<CurrencyTracker>();
        }

        if (logIfMissing)
        {
            if (tracker == null)
                Debug.LogWarning($"CurrencyHUD on '{name}': No CurrencyTracker found.");
            if (currency == null)
                Debug.LogWarning($"CurrencyHUD on '{name}': No CurrencySO assigned.");
        }
    }

    private void Subscribe()
    {
        if (subscribed || tracker == null) return;
        tracker.OnCurrencyChanged += HandleCurrencyChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || tracker == null) return;
        tracker.OnCurrencyChanged -= HandleCurrencyChanged;
        subscribed = false;
    }

    private void HandleCurrencyChanged(CurrencySO changed, int newTotal)
    {
        if (currency == null || changed != currency) return;
        if (amountText != null)
            amountText.text = newTotal.ToString();
    }

    private void Refresh()
    {
        if (amountText == null) return;
        int amount = (tracker != null && currency != null) ? tracker.GetAmount(currency) : 0;
        amountText.text = amount.ToString();
    }
}
