using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public struct NamedClip
    {
        public string key;
        public AudioClip clip;
    }

    [Header("Mixer (optional but recommended)")]
    [Tooltip("If using an AudioMixer, assign it and the exposed group references below.")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Music")]
    public List<NamedClip> musicLibrary = new();
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Tooltip("Seconds to crossfade to a new music track.")]
    public float defaultMusicFade = 1.0f;

    [Header("SFX")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("How many pooled SFX AudioSources to prewarm.")]
    public int sfxPoolSize = 16;
    [Tooltip("Optional parent for spawned SFX sources.")]
    public Transform sfxPoolParent;

    private readonly Dictionary<string, AudioClip> _musicMap = new();
    private AudioSource _musicA, _musicB;
    private bool _usingA = true;
    private Coroutine _musicFadeRoutine;

    private readonly Queue<AudioSource> _sfxFree = new();
    private readonly List<AudioSource> _sfxAll = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Music sources (two for crossfade)
        _musicA = gameObject.AddComponent<AudioSource>();
        _musicB = gameObject.AddComponent<AudioSource>();
        _musicA.loop = true; _musicB.loop = true;
        _musicA.playOnAwake = false; _musicB.playOnAwake = false;
        _musicA.outputAudioMixerGroup = musicGroup;
        _musicB.outputAudioMixerGroup = musicGroup;
        _musicA.volume = 0f; _musicB.volume = 0f;

        // Library map
        foreach (var nc in musicLibrary)
            if (nc.clip && !string.IsNullOrWhiteSpace(nc.key))
                _musicMap[nc.key] = nc.clip;

        // SFX pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            if (sfxPoolParent) go.transform.SetParent(sfxPoolParent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialize = true;
            src.spatialBlend = 1f;      // 3D by default
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.outputAudioMixerGroup = sfxGroup;
            _sfxAll.Add(src);
            _sfxFree.Enqueue(src);
        }

        ApplyMixerVolumes();
    }

    private void OnValidate()
    {
        if (musicVolume < 0f) musicVolume = 0f;
        if (sfxVolume < 0f) sfxVolume = 0f;
        if (sfxPoolSize < 1) sfxPoolSize = 1;
        ApplyMixerVolumes();
    }

    private void ApplyMixerVolumes()
    {
        // If you use an AudioMixer with exposed parameters named "MusicVol" and "SFXVol",
        // this will set them in decibels. Otherwise, we drive the sources directly.
        if (mixer)
        {
            if (musicGroup) mixer.SetFloat("MusicVol", LinearToDecibel(musicVolume));
            if (sfxGroup) mixer.SetFloat("SFXVol", LinearToDecibel(sfxVolume));
        }
        else
        {
            // Fallback if no mixer—music source volumes are controlled in the crossfade routine,
            // so no direct set here. SFX is handled per source at play time (multiplied).
        }
    }

    private static float LinearToDecibel(float lin) => lin > 0.0001f ? 20f * Mathf.Log10(lin) : -80f;

    // ---------- Music ----------
    public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, bool loop = true)
    {
        if (!clip) return;
        if (fadeSeconds < 0f) fadeSeconds = defaultMusicFade;

        var active = _usingA ? _musicA : _musicB;
        var idle = _usingA ? _musicB : _musicA;

        idle.clip = clip;
        idle.loop = loop;
        idle.volume = 0f;
        idle.Play();

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeRoutine(active, idle, fadeSeconds, musicVolume));

        _usingA = !_usingA;
    }

    public void PlayMusic(string key, float fadeSeconds = -1f, bool loop = true)
    {
        if (_musicMap.TryGetValue(key, out var clip))
            PlayMusic(clip, fadeSeconds, loop);
    }

    private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float duration, float targetVol)
    {
        float t = 0f;
        float fromStart = from.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? t / duration : 1f;
            to.volume = Mathf.Lerp(0f, targetVol, k);
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            yield return null;
        }
        to.volume = targetVol;
        from.volume = 0f;
        if (from.isPlaying) from.Stop();
    }

    // ---------- SFX ----------
    public void PlaySFX(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f, bool spatial = true)
    {
        if (!clip) return;
        var src = GetFreeSFXSource();
        if (!src) return;

        src.transform.position = worldPos;
        src.spatialBlend = spatial ? 1f : 0f;
        src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        // Multiply by master SFX volume if no mixer
        float finalVol = mixer ? volume : volume * sfxVolume;
        src.volume = Mathf.Clamp01(finalVol);

        src.clip = clip;
        src.Play();
        StartCoroutine(ReturnSFXWhenDone(src, clip.length / Mathf.Max(0.01f, src.pitch)));
    }

    public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        => PlaySFX(clip, Vector3.zero, volume, pitch, spatial: false);

    private AudioSource GetFreeSFXSource()
    {
        if (_sfxFree.Count == 0)
        {
            // Expand pool if needed
            var go = new GameObject($"SFX_{_sfxAll.Count}");
            if (sfxPoolParent) go.transform.SetParent(sfxPoolParent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialize = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.outputAudioMixerGroup = sfxGroup;
            _sfxAll.Add(src);
            return src;
        }
        return _sfxFree.Dequeue();
    }

    private IEnumerator ReturnSFXWhenDone(AudioSource src, float seconds)
    {
        yield return new WaitForSeconds(seconds + 0.02f);
        src.Stop();
        src.clip = null;
        _sfxFree.Enqueue(src);
    }

    // ---------- Public Volume Controls ----------
    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (mixer && musicGroup) mixer.SetFloat("MusicVol", LinearToDecibel(musicVolume));
        else
        {
            // Directly set current active music source volume target
            var active = _usingA ? _musicA : _musicB;
            active.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (mixer && sfxGroup) mixer.SetFloat("SFXVol", LinearToDecibel(sfxVolume));
    }
}
