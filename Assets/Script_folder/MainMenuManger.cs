using UnityEngine;
using UnityEngine.SceneManagement; // Lets us switch scenes

public class MainMenuManager : MonoBehaviour
{
    // This is the name of your game scene — must match exactly
    [Header("Scene To Load")]
    public string gameSceneName = "ScriptTestScene";

    // -----------------------------------------------
    // Called when player clicks "Start Game"
    // Loads your actual game scene
    // -----------------------------------------------
    public void OnStartGame()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene(gameSceneName);
    }

    // -----------------------------------------------
    // Called when player clicks "Options"
    // Right now just logs a message
    // You can hook up an options panel later
    // -----------------------------------------------
    public void OnOptions()
    {
        Debug.Log("Options button clicked");
        // TODO: show options panel
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