using UnityEngine;
using UnityEngine.SceneManagement; // Lets us switch scenes

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

    // -----------------------------------------------
    // Called when player clicks "Start Game"
    // Loads your actual game scene
    // -----------------------------------------------
    public void OnStartGame()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene(ScriptTestScene);
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
