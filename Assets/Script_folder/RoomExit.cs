using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Add alongside (or near) EnemyManager. Listens for EnemyManager.OnAllWavesCleared and
/// reveals a portal/door object that starts hidden in the scene. Minimal first step toward
/// a Hades-style "clear the room, then move on" flow — not a full doors-with-rewards system.
///
/// EnemyManager is optional. With one assigned/found, the door stays hidden until all waves
/// clear. With none, RoomExit skips the gating entirely and acts as a plain always-open exit —
/// useful for rooms that don't have combat (e.g. a hub or a corridor).
///
/// Setup:
///   1. Build a portal/door GameObject in the scene (any visual + a trigger Collider).
///   2. Drag it into portalOrDoor here.
///   3. Add PortalTrigger.cs to that same portal GameObject and drag this RoomExit into it.
///   4. Set nextSceneName once you know what the next scene should be.
///   5. Only add an EnemyManager reference if this exit should be wave-gated.
/// </summary>
public class RoomExit : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. Auto-found if on the same GameObject. Leave unassigned (and don't add " +
             "an EnemyManager component) to make this a plain exit with no wave gating.")]
    public EnemyManager enemyManager;

    [Header("Portal / Door")]
    [Tooltip("The portal or door object. Hidden until waves clear when an EnemyManager is " +
             "present; revealed immediately if not. Build it inactive-friendly (visual + trigger Collider).")]
    public GameObject portalOrDoor;

    [Header("Next Scene")]
    [Tooltip("Scene to load when the player walks into the portal. Must be added to Build Settings.")]
    public string nextSceneName;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = GetComponent<EnemyManager>();

        if (enemyManager == null)
        {
            // Plain exit mode — no waves to wait for, so open the door immediately.
            Debug.Log($"RoomExit on '{name}': No EnemyManager found — acting as a plain exit (no wave gating).");
            if (portalOrDoor != null)
                portalOrDoor.SetActive(true);
            return;
        }

        if (portalOrDoor != null)
            portalOrDoor.SetActive(false);
        else
            Debug.LogWarning($"RoomExit on '{name}': No portalOrDoor assigned — nothing will appear when the waves clear.");
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

        Time.timeScale = 1f; // Time.timeScale persists across scene loads — must clear before loading
        SceneManager.LoadScene(nextSceneName);
    }
}
