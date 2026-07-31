using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A minimal settings panel: master volume + fullscreen toggle, persisted via
/// PlayerPrefs so choices carry over between play sessions and scene loads.
///
/// Standalone and reusable — anything with a "Settings" button can call Open()/Close()
/// on this (the pause menu, the main menu's still-stubbed Options button, etc.).
/// Works fine while the game is paused (Time.timeScale = 0) since neither audio
/// volume nor fullscreen are affected by timeScale, and UI still responds at 0.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private const string VolumeKey = "Settings_MasterVolume";
    private const string FullscreenKey = "Settings_Fullscreen";

    [Header("Settings UI")]
    [Tooltip("Panel this component shows/hides. Hidden automatically on Awake.")]
    public GameObject settingsPanel;

    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private void Awake()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        ApplyVolume(savedVolume);
        ApplyFullscreen(savedFullscreen);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(savedVolume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    // ------------------------------------------------------------------ Panel open/close

    public void Open()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void Close()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ Handlers

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
    }

    private void OnFullscreenChanged(bool value)
    {
        ApplyFullscreen(value);
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
    }

    private void ApplyVolume(float value) => AudioListener.volume = Mathf.Clamp01(value);

    private void ApplyFullscreen(bool value) => Screen.fullScreen = value;
}
