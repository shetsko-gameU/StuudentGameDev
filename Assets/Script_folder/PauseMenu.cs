using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Add to any GameObject in the gameplay scene (a dedicated "PauseManager" object works well).
///
/// Listens for Escape / Gamepad Start directly via the Input System's low-level
/// Keyboard/Gamepad classes, rather than through the InputSystem_Actions asset — a
/// pause toggle needs to work everywhere, so it isn't tied to the Player action map.
/// No new action/binding needs to be added to that asset for this to work.
///
/// Pausing sets Time.timeScale = 0 (freezes physics, WaitForSeconds, animation) and
/// disables the player's move/attack/ability scripts so input can't leak through
/// while the panel is up. UI still responds at timeScale 0 — the EventSystem runs
/// on real time, not scaled time.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References (auto-found on the \"Player\" tag if left empty)")]
    public StatsManager stats;
    public PlayerMove playerMove;
    public ComboRunner comboRunner;
    public AbilityRunner abilityRunner;

    [Header("Pause UI")]
    [Tooltip("Panel shown while paused. Hidden automatically on Awake.")]
    public GameObject pausePanel;

    [Tooltip("Optional. If assigned, the Settings button hides pausePanel and opens this instead.")]
    public SettingsMenu settingsMenu;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Scene to load for \"Return to Hub\". Must exist and be added to Build Settings.")]
    public string hubSceneName = "Hub";

    public bool IsPaused { get; private set; }

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (stats == null || playerMove == null || comboRunner == null || abilityRunner == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (stats == null) stats = player.GetComponent<StatsManager>();
                if (playerMove == null) playerMove = player.GetComponent<PlayerMove>();
                if (comboRunner == null) comboRunner = player.GetComponent<ComboRunner>();
                if (abilityRunner == null) abilityRunner = player.GetComponent<AbilityRunner>();
            }
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        // Don't let Escape open the pause menu over the death screen.
        if (stats != null && stats.IsDead) return;

        bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                          || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (pausePressed)
            TogglePause();
    }

    private void OnDisable()
    {
        // Safety net — never leave the whole game frozen if this object goes away while paused.
        if (IsPaused)
            Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ Pause / Resume

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        SetPlayerInputEnabled(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void SetPlayerInputEnabled(bool value)
    {
        // Don't resurrect controls for a dead player — PlayerDeathHandler owns that state.
        if (value && stats != null && stats.IsDead) return;

        if (playerMove != null) playerMove.enabled = value;
        if (comboRunner != null) comboRunner.enabled = value;
        if (abilityRunner != null) abilityRunner.enabled = value;
    }

    // ------------------------------------------------------------------ Pause menu buttons

    public void OnResume() => Resume();

    public void OnRestartRun()
    {
        Time.timeScale = 1f; // Time.timeScale persists across scene loads — must clear before loading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnReturnToHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }

    // ------------------------------------------------------------------ Settings sub-panel

    /// <summary>Hides the pause panel and opens Settings. Wire the Settings panel's own
    /// Back button to OnCloseSettings — NOT to SettingsMenu.Close() directly — so the
    /// pause panel reappears underneath it.</summary>
    public void OnOpenSettings()
    {
        if (settingsMenu == null) return;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        settingsMenu.Open();
    }

    public void OnCloseSettings()
    {
        if (settingsMenu != null)
            settingsMenu.Close();

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }
}
