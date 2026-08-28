using UnityEngine;

/// <summary>
/// Drop into any scene (main menu, hub, a level with no EnemyManager) to start its
/// ambient/theme track on load. For rooms that swap to combat music, use
/// RoomMusicTrigger instead — this is for the simple "just play this scene's track" case.
/// </summary>
public class SceneMusicPlayer : MonoBehaviour
{
    public AudioClip musicClip;
    public float fadeSeconds = 1.5f;

    private void Start()
    {
        if (musicClip != null)
            SoundManager.Instance.PlayMusic(musicClip, fadeSeconds);
    }
}
