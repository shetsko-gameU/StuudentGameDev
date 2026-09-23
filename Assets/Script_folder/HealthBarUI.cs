using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a UI GameObject. Displays an entity's health as a fill bar.
/// Subscribes to StatsManager.OnHealthChanged and updates automatically.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The entity's StatsManager. Auto-found on the Player tag if left empty.")]
    public StatsManager stats;

    [Header("UI Elements")]
    [Tooltip("Slider used as the health fill bar (Min 0, Max 1).")]
    public Slider slider;

    [Tooltip("(Optional) TextMeshPro label showing current/max health.")]
    public TextMeshProUGUI healthText;

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
        if (stats != null)
            UpdateDisplay(stats.CurrentHealth, stats.MaxHealth);
        else
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
        if (stats != null)
            UpdateDisplay(stats.CurrentHealth, stats.MaxHealth);
    }

    private void TryBind(bool logIfMissing)
    {
        if (stats != null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            stats = player.GetComponent<StatsManager>();

        if (stats == null && logIfMissing)
            Debug.LogWarning($"HealthBarUI on '{name}': No StatsManager found.");
    }

    private void Subscribe()
    {
        if (subscribed || stats == null) return;
        stats.OnHealthChanged += UpdateDisplay;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || stats == null) return;
        stats.OnHealthChanged -= UpdateDisplay;
        subscribed = false;
    }

    private void UpdateDisplay(float current, float max)
    {
        if (slider != null)
            slider.value = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
