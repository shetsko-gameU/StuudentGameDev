using UnityEngine;

/// <summary>
/// Catches the two ways a player leaves mid-stage without touching a portal: quitting, and the
/// app being backgrounded (where mobile/console platforms may never send a quit at all). Both
/// re-write the current stage's save so the run resumes with the inventory, currency and health
/// the player actually had when they stopped.
///
/// It refuses to invent a save, and only refreshes one that already points at the active scene —
/// see RunSaveSerializer.RefreshCurrentStage — so quitting in the hub or main menu changes
/// nothing.
///
/// No setup: it creates itself after the first scene load and survives every later one.
/// </summary>
public class AutoSaveHooks : MonoBehaviour
{
    private static AutoSaveHooks instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (instance != null) return;

        GameObject host = new GameObject("AutoSaveHooks (Persistent)");
        instance = host.AddComponent<AutoSaveHooks>();
        DontDestroyOnLoad(host);
    }

    private void OnApplicationQuit()
    {
        RunSaveSerializer.RefreshCurrentStage();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            RunSaveSerializer.RefreshCurrentStage();
    }
}
