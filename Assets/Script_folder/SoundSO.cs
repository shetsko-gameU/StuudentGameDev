using UnityEngine;

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
