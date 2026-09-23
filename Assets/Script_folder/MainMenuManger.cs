using UnityEngine;
using UnityEngine.SceneManagement; // Lets us switch scenes

/// <summary>
/// Main menu controller: Start Game loads the scene named in ScriptTestScene (despite the
/// stale field name - check its current value in the Inspector rather than assuming), and
/// Options follows the same open/close-settings pattern PauseMenu uses (see
/// OnCloseSettings's own doc comment for why the Settings panel's Back button must call
/// this, not SettingsMenu.Close() directly).
///
/// Setup:
///   1. Add to a GameObject in the main menu scene.
///   2. Set ScriptTestScene to whatever scene "Start Game" should load.
///   3. Assign mainPanel and settingsMenu.
///   4. Wire the Start/Options/Exit buttons to OnStartGame/OnOptions/OnExit, and the
///      Settings panel's own Back button to OnCloseSettings (not SettingsMenu.Close()).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // This is the name of your game scene � must match exactly
    [Header("Scene To Load")]
    public string ScriptTestScene = "ScriptTestScene";

    [Header("Menu UI")]
    [Tooltip("The main button panel (Start/Options/Exit). Hidden while Settings is open.")]
    public GameObject mainPanel;

    [Tooltip("Same SettingsMenu component pattern PauseMenu uses � volume + fullscreen.")]
    public SettingsMenu settingsMenu;

    [Tooltip("Continue button — hidden automatically when there's no run to resume.")]
    public GameObject continueButton;

    private void Start()
    {
        // Nothing to resume after a death or a win, so don't offer a button that would do nothing.
        if (continueButton != null)
            continueButton.SetActive(SaveSystem.HasRun);
    }

    // -----------------------------------------------
    // Called when player clicks "Start Game"
    // Loads your actual game scene
    // -----------------------------------------------
    public void OnStartGame()
    {
        Debug.Log("Starting game...");

        // A fresh run must not inherit a half-finished one: drop both the disk save and anything
        // still sitting in memory from a previous session in this same play session.
        SaveSystem.DeleteRun();
        RunStateManager.Instance.Clear();

        SceneManager.LoadScene(ScriptTestScene);
    }

    /// <summary>
    /// Resumes the saved run. Fills RunStateManager from disk and loads the saved stage;
    /// RunStatePlayerLink on that scene's Player then restores it exactly as it does after a
    /// portal, so there is no second restore path to keep in sync.
    /// </summary>
    public void OnContinue()
    {
        string sceneName;
        if (!RunSaveSerializer.TryLoadIntoRunState(out sceneName))
        {
            Debug.LogWarning("MainMenuManager: Continue was pressed but there's no readable run — " +
                             "starting a new one instead.");
            OnStartGame();
            return;
        }

        Debug.Log($"Continuing run at '{sceneName}'.");
        SceneManager.LoadScene(sceneName);
    }

    // -----------------------------------------------
    // Called when player clicks "Options"
    // Hides the main button panel and opens Settings.
    // -----------------------------------------------
    public void OnOptions()
    {
        if (settingsMenu == null) return;

        if (mainPanel != null)
            mainPanel.SetActive(false);

        settingsMenu.Open();
    }

    /// <summary>Wire the Settings panel's own Back button to this � NOT to
    /// SettingsMenu.Close() directly � so the main button panel reappears underneath it.</summary>
    public void OnCloseSettings()
    {
        if (settingsMenu != null)
            settingsMenu.Close();

        if (mainPanel != null)
            mainPanel.SetActive(true);
    }

    // -----------------------------------------------
    // Called when player clicks "Exit"
    // Closes the game completely
    // (Only works in a built game, not in the Editor)
    // -----------------------------------------------
    public void OnExit()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
