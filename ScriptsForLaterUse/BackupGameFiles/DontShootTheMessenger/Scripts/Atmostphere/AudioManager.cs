using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource distortionSource;

    [Header("Clips")]
    public AudioClip defaultAmbience;
    public AudioClip[] whispers;
    public AudioClip explosionClip;
    public AudioClip footsteps;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PlayAmbience(defaultAmbience);
    }

    public void PlayAmbience(AudioClip clip)
    {
        ambienceSource.clip = clip;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayWhisper()
    {
        if (whispers.Length > 0)
        {
            var clip = whispers[Random.Range(0, whispers.Length)];
            sfxSource.PlayOneShot(clip, 0.7f);
        }
    }

    public void StartDistortion()
    {
        if (!distortionSource.isPlaying)
            distortionSource.Play();
    }

    public void StopDistortion()
    {
        if (distortionSource.isPlaying)
            distortionSource.Stop();
    }
}
