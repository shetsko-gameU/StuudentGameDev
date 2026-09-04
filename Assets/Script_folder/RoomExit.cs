using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Add alongside (or near) EnemyManager. Listens for EnemyManager.OnAllWavesCleared and
/// reveals a portal/door object that starts hidden in the scene. Minimal first step toward
/// a Hades-style "clear the room, then move on" flow — not a full doors-with-rewards system.
///
/// Setup:
///   1. Build a portal/door GameObject in the scene (any visual + a trigger Collider).
///   2. Drag it into portalOrDoor here — it gets SetActive(false) on Awake automatically.
///   3. Add PortalTrigger.cs to that same portal GameObject and drag this RoomExit into it.
///   4. Set nextSceneName once you know what the next scene should be.
/// </summary>
public class RoomExit : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject.")]
    public EnemyManager enemyManager;

    [Header("Portal / Door")]
    [Tooltip("The portal or door object to reveal once the last wave is cleared. " +
             "Hidden automatically on Awake — build it inactive-friendly (visual + trigger Collider).")]
    public GameObject portalOrDoor;

    [Header("Next Scene")]
    [Tooltip("Scene to load when the player walks into the portal. Must be added to Build Settings.")]
    public string nextSceneName;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = GetComponent<EnemyManager>();

        if (portalOrDoor == null)
        {
            Debug.LogWarning($"RoomExit on '{name}': No portalOrDoor assigned — nothing will appear when the waves clear.");
        }
        else if (enemyManager == null)
        {
            // No EnemyManager to ever fire OnAllWavesCleared — leave the portal open
            // instead of hiding it and never being able to reveal it again.
            Debug.LogError($"RoomExit on '{name}': No EnemyManager found — leaving portalOrDoor open.");
        }
        else
        {
            portalOrDoor.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (enemyManager != null)
            enemyManager.OnAllWavesCleared += HandleAllWavesCleared;
    }

    private void OnDisable()
    {
        if (enemyManager != null)
            enemyManager.OnAllWavesCleared -= HandleAllWavesCleared;
    }

    private void HandleAllWavesCleared()
    {
        if (portalOrDoor != null)
            portalOrDoor.SetActive(true);
    }

    /// <summary>Called by PortalTrigger when the player walks into the revealed portal.</summary>
    public void EnterPortal()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"RoomExit on '{name}': nextSceneName is not set — nothing to load.");
            return;
        }

        // Snapshot passives/inventory/currency before the scene unloads — RunStatePlayerLink
        // on the new scene's Player reapplies this once it's initialized. Purely in-memory,
        // separate from any future meta-progression save-to-disk system.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        RunStatePlayerLink runState = player != null ? player.GetComponent<RunStatePlayerLink>() : null;
        if (runState != null)
            runState.CaptureAndStore();
        else
            Debug.LogWarning($"RoomExit on '{name}': No RunStatePlayerLink found on the Player — " +
                              "passives/inventory/currency won't carry over to the next scene.");

        Time.timeScale = 1f; // Time.timeScale persists across scene loads — must clear before loading
        SceneManager.LoadScene(nextSceneName);
    }
}
