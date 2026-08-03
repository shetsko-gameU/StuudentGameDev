using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a UI GameObject. Displays the balance of one CurrencySO type.
/// Subscribes to CurrencyTracker.OnCurrencyChanged and updates automatically.
///
/// Wiring in the Inspector:
///   tracker   — drag the player's CurrencyTracker here (or leave empty to auto-find)
///   currency  — the CurrencySO this display tracks
///   amountText — TextMeshProUGUI label that shows the number
///   iconImage  — (optional) RawImage that shows the currency icon
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

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (tracker == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                tracker = player.GetComponent<CurrencyTracker>();
        }

        if (tracker == null)
            Debug.LogWarning($"CurrencyHUD on '{name}': No CurrencyTracker found.");
    }

    private void Start()
    {
        // Set icon once on start
        if (iconImage != null && currency != null && currency.icon != null)
            iconImage.texture = currency.icon;

        Refresh();
    }

    private void OnEnable()
    {
        if (tracker != null)
            tracker.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    // ------------------------------------------------------------------ Handlers

    private void HandleCurrencyChanged(CurrencySO changed, int newTotal)
    {
        if (changed != currency) return;
        UpdateDisplay(newTotal);
    }

    // ------------------------------------------------------------------ Display

    private void Refresh()
    {
        int total = tracker != null ? tracker.GetAmount(currency) : 0;
        UpdateDisplay(total);
    }

    private void UpdateDisplay(int total)
    {
        if (amountText != null)
            amountText.text = total.ToString();
    }
}
