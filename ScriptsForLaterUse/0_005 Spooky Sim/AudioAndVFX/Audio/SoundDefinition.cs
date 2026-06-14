using UnityEngine;
using UnityEngine.Audio;

public enum SoundCategory { SFX, UI, Ambience, Music }

[CreateAssetMenu(menuName = "CherryAudio/Sound Definition", fileName = "SD_")]
public class SoundDefinition : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Routing")]
    public SoundCategory category = SoundCategory.SFX;
    public AudioMixerGroup outputOverride;

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Adds +/- random range to volume.")]
    [Range(0f, 1f)] public float volumeJitter = 0.1f;

    [Tooltip("Base pitch.")]
    [Range(-3f, 3f)] public float pitch = 1f;
    [Tooltip("Adds +/- random range to pitch.")]
    [Range(0f, 3f)] public float pitchJitter = 0.1f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
    public float minDistance = 1f;
    public float maxDistance = 25f;

    [Header("Spam Control")]
    [Tooltip("Seconds between allowed plays (per SoundDefinition). 0 = no cooldown.")]
    public float cooldown = 0f;

    [Tooltip("Max concurrent instances of this sound. 0 = unlimited.")]
    public int maxPolyphony = 6;

    [Header("Loop Hint")]
    [Tooltip("If true, AudioManager will treat this as a loop when you call StartLoop.")]
    public bool loop = false;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
