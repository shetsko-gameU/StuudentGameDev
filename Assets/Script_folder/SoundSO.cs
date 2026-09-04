using UnityEngine;

/// <summary>
/// A single named sound "event" — e.g. "Sword Swing" or "Enemy Death" — that can bundle
/// several clip variations so repeats don't sound identical. Referenced by SFX/Music
/// trigger scripts (HitSFXTrigger, ComboSFXTrigger, etc.); never played directly.
///
/// Setup:
///   1. Project window → right-click → Create → Audio → Sound.
///   2. Drag one or more AudioClips into clips — one is picked at random each play.
///   3. Set volume, and optionally a pitchMin/pitchMax range for pitch variation
///      (leave both at 1 for no variation).
///   4. Drag this asset into any trigger script's SoundSO field (e.g. HitSFXTrigger.hitSound).
/// </summary>
[CreateAssetMenu(fileName = "New Sound", menuName = "Audio/Sound")]
public class SoundSO : ScriptableObject
{
    [Tooltip("One is picked at random each time this sound plays.")]
    public AudioClip[] clips;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Pitch is randomized within this range each play, to avoid identical repeats.")]
    public float pitchMin = 1f;
    public float pitchMax = 1f;

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0)
            return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public void ApplyTo(AudioSource source)
    {
        source.clip = GetClip();
        source.volume = volume;
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.loop = false;
    }
}
