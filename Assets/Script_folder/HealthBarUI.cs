using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a UI GameObject. Displays an entity's health as a fill bar.
/// Subscribes to StatsManager.OnHealthChanged and updates automatically.
///
/// Wiring in the Inspector:
///   stats      — drag the entity's StatsManager here (or leave empty to auto-find the Player)
///   slider     — Slider used as the fill bar (leave Min=0, Max=1, Whole Numbers off, Interactable off)
///   healthText — (optional) TextMeshProUGUI label showing "current / max"
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

    private void Awake()
    {
        if (stats == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                stats = player.GetComponent<StatsManager>();
        }

        if (stats == null)
            Debug.LogWarning($"HealthBarUI on '{name}': No StatsManager found.");
    }

    private void Start()
    {
        if (stats != null)
            UpdateDisplay(stats.CurrentHealth, stats.MaxHealth);
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(float current, float max)
    {
        if (slider != null)
            slider.value = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
