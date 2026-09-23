using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The death screen. Sits on the DeathScreen prefab and wires its own buttons to the
/// player's PlayerDeathHandler at runtime.
///
/// PlayerDeathHandler has always had a deathScreenPanel field and the logic to show it, but
/// the field was empty on every prefab and no death screen existed anywhere in the project -
/// so dying left the player lying there with no way to restart. Rather than add a panel to
/// each scene by hand and re-wire three button OnClick entries every time (which is what
/// the old setup notes asked for, and is easy to forget in a new scene), the panel is a
/// prefab that PlayerDeathHandler spawns on death and this script connects up in code.
///
/// That means a new scene needs no death-screen setup at all: drop in the player prefab and
/// death works.
///
/// Setup:
///   1. Nothing, normally - Player.prefab and Player_Wizard.prefab already reference this
///      prefab through PlayerDeathHandler.deathScreenPrefab.
///   2. To restyle it, edit Assets/UI/DeathScreen.prefab directly. Keep the three button
///      references assigned; the labels and layout are free to change.
///   3. To use a bespoke per-scene panel instead, assign PlayerDeathHandler.deathScreenPanel
///      in that scene and the prefab spawn is skipped.
/// </summary>
public class DeathScreenController : MonoBehaviour
{
    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button hubButton;

    private void Awake()
    {
        EnsureEventSystemExists();
    }

    /// <summary>
    /// uGUI buttons are unclickable without an EventSystem in the scene, and several scenes
    /// in this project are gameplay-only and have none. Spawning one on demand means the
    /// death screen works everywhere instead of only in scenes that happen to have UI.
    /// </summary>
    private static void EnsureEventSystemExists()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;

        GameObject eventSystem = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        Debug.Log("DeathScreenController: no EventSystem in this scene, so one was created " +
                  "for the death screen's buttons.");
    }

    /// <summary>
    /// Points the three buttons at the dead player's handler. Called by PlayerDeathHandler
    /// straight after it spawns this prefab.
    ///
    /// Listeners are removed first so that re-initialising (or a scene that somehow spawns
    /// two of these) cannot stack duplicate handlers and load a scene twice.
    /// </summary>
    public void Initialize(PlayerDeathHandler handler)
    {
        if (handler == null)
        {
            Debug.LogError($"DeathScreenController on '{name}': initialised with no PlayerDeathHandler, " +
                           "so its buttons will do nothing.");
            return;
        }

        Bind(restartButton, handler.OnRestartRun, nameof(restartButton));
        Bind(mainMenuButton, handler.OnReturnToMainMenu, nameof(mainMenuButton));
        Bind(hubButton, handler.OnReturnToHub, nameof(hubButton));
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action, string fieldName)
    {
        if (button == null)
        {
            Debug.LogWarning($"DeathScreenController on '{name}': {fieldName} is not assigned.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
