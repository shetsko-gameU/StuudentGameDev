using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lazily self-creating, scene-independent audio playback hub — same pattern as
/// RunStateManager. Nothing needs to place this in a scene; the first PlaySFX/PlayMusic
/// call creates and persists it via DontDestroyOnLoad.
///
/// Gameplay scripts should reference a SoundSO asset and call SoundManager.Instance rather
/// than holding their own AudioSource — keeps pooling and volume centralized in one place.
/// Fades run on unscaled time so music can fade while the game is paused (Time.timeScale = 0).
///
/// Setup:
///   Nothing to place by hand — SoundManager.Instance creates and persists itself
///   (DontDestroyOnLoad) the first time any trigger script calls PlaySFX/PlaySFXAtPoint/
///   PlayMusic. To tune sfxPoolSize/sfxVolume/musicVolume ahead of time instead of at
///   runtime, add a SoundManager component to a bootstrap GameObject yourself in your
///   first-loaded scene (e.g. MainMenu) — Awake()'s duplicate-guard makes it safe even if
///   Instance also gets accessed before that object's Awake runs.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SoundManager (Persistent)");
                instance = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("SFX")]
    [Tooltip("Number of simultaneous non-positional SFX voices.")]
    public int sfxPoolSize = 8;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Music")]
    [Range(0f, 1f)] public float musicVolume = 1f;

    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private int nextPoolIndex;

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private Coroutine musicFadeRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sfxPool.Add(source);
        }

        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceA.playOnAwake = false;
        musicSourceB.playOnAwake = false;
        musicSourceA.loop = true;
        musicSourceB.loop = true;
        activeMusicSource = musicSourceA;
    }

    // ---------------------------------------------------------------- SFX

    /// <summary>Non-positional SFX (UI clicks, pickups, ambience) — plays from the SoundManager itself.</summary>
    public void PlaySFX(SoundSO sound)
    {
        if (sound == null)
            return;

        AudioSource source = sfxPool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % sfxPool.Count;

        sound.ApplyTo(source);
        source.spatialBlend = 0f;
        source.volume *= sfxVolume;
        source.Play();
    }

    /// <summary>Positional SFX (hit impacts, deaths, footsteps) — plays at a world position, then cleans itself up.</summary>
    public void PlaySFXAtPoint(SoundSO sound, Vector3 position)
    {
        if (sound == null)
            return;

        AudioClip clip = sound.GetClip();
        if (clip == null)
            return;

        var go = new GameObject("SFX (One-Shot)");
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        sound.ApplyTo(source);
        source.spatialBlend = 1f;
        source.volume *= sfxVolume;
        source.Play();

        Destroy(go, clip.length / Mathf.Max(0.01f, source.pitch));
    }

    // ---------------------------------------------------------------- Music

    public void PlayMusic(AudioClip clip, float fadeSeconds = 1f)
    {
        if (clip == null || (activeMusicSource.clip == clip && activeMusicSource.isPlaying))
            return;

        AudioSource incoming = activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(CrossfadeMusic(activeMusicSource, incoming, fadeSeconds));
        activeMusicSource = incoming;
    }

    public void StopMusic(float fadeSeconds = 1f)
    {
        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(FadeOutMusic(activeMusicSource, fadeSeconds));
    }

    private IEnumerator CrossfadeMusic(AudioSource outgoing, AudioSource incoming, float fadeSeconds)
    {
        float t = 0f;
        float outgoingStart = outgoing.volume;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float ratio = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(t / fadeSeconds);
            outgoing.volume = Mathf.Lerp(outgoingStart, 0f, ratio);
            incoming.volume = Mathf.Lerp(0f, musicVolume, ratio);
            yield return null;
        }

        outgoing.Stop();
        incoming.volume = musicVolume;
    }

    private IEnumerator FadeOutMusic(AudioSource source, float fadeSeconds)
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float ratio = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(t / fadeSeconds);
            source.volume = Mathf.Lerp(startVolume, 0f, ratio);
            yield return null;
        }

        source.Stop();
    }
}
