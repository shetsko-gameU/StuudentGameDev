using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional UI binder for the boss-win congrats prefab. Mirrors DeathScreenController:
/// buttons call BossStageController hub/menu exits (which wipe run state).
/// </summary>
public class CongratsScreenController : MonoBehaviour
{
    [Header("Buttons (optional — auto-found by name if empty)")]
    public Button returnToHubButton;
    public Button returnToMainMenuButton;

    private BossStageController bossStage;

    private void Awake()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;

        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    public void Initialize(BossStageController controller)
    {
        bossStage = controller;
        AutoFindButtons();

        if (returnToHubButton != null)
        {
            returnToHubButton.onClick.RemoveAllListeners();
            returnToHubButton.onClick.AddListener(bossStage.OnReturnToHub);
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveAllListeners();
            returnToMainMenuButton.onClick.AddListener(bossStage.OnReturnToMainMenu);
        }
    }

    private void AutoFindButtons()
    {
        if (returnToHubButton != null && returnToMainMenuButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            string n = button.gameObject.name.ToLowerInvariant();
            if (returnToHubButton == null && (n.Contains("hub") || n.Contains("restart")))
                returnToHubButton = button;
            else if (returnToMainMenuButton == null && n.Contains("menu"))
                returnToMainMenuButton = button;
        }
    }
}
