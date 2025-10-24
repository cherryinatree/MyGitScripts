using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Serializable]
    public class Track
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float trackVolume = 1f;
        public bool loop = true;
    }

    [Header("Library")]
    [Tooltip("Populate with your music tracks. Keys must be unique.")]
    public List<Track> tracks = new();

    [Header("Mixer (optional)")]
    [Tooltip("If using an AudioMixer, assign it and expose a float parameter (e.g., MusicVol).")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    [Tooltip("Name of the exposed mixer parameter for music volume, e.g., \"MusicVol\" (in dB).")]
    public string mixerVolumeParam = "MusicVol";

    [Header("Playback")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("Default seconds to crossfade when switching tracks.")]
    public float defaultFade = 1.2f;
    [Tooltip("Persist masterVolume & mute in PlayerPrefs.")]
    public bool savePreferences = true;

    [Header("Playlist")]
    public bool loopPlaylist = true;
    public bool shuffleOnStart = false;

    // --- Runtime ---
    private readonly Dictionary<string, Track> _map = new();
    private AudioSource _a, _b;
    private bool _usingA = true;
    private Coroutine _fadeCo;
    private List<Track> _playlist = new();
    private int _playlistIndex = -1;
    private bool _isMuted;

    public event Action<Track> OnTrackChanged;
    public event Action<bool> OnPausedChanged;

    const string PP_VOL = "MM_MusicVol";
    const string PP_MUTE = "MM_MusicMute";

    private void Awake()
    {
        // Singleton & don’t destroy
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build lookup
        _map.Clear();
        foreach (var t in tracks)
        {
            if (t != null && t.clip != null && !string.IsNullOrWhiteSpace(t.key))
                _map[t.key] = t;
        }

        // Dual sources for crossfades
        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        ConfigureSource(_a);
        ConfigureSource(_b);

        // Load saved prefs
        if (savePreferences)
        {
            if (PlayerPrefs.HasKey(PP_VOL)) masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PP_VOL, 1f));
            if (PlayerPrefs.HasKey(PP_MUTE)) _isMuted = PlayerPrefs.GetInt(PP_MUTE, 0) == 1;
        }
        ApplyVolumeToMixerOrSource();

        if (_isMuted) { _a.mute = true; _b.mute = true; }

        // Optional initial shuffle of the library into a playlist
        if (shuffleOnStart && tracks.Count > 0)
        {
            StartPlaylist(tracks, shuffle: true, autoPlay: false);
        }
    }

    private void OnValidate()
    {
        // Keep dictionary fresh in editor
        _map.Clear();
        foreach (var t in tracks)
        {
            if (t != null && t.clip != null && !string.IsNullOrWhiteSpace(t.key))
                _map[t.key] = t;
        }
        masterVolume = Mathf.Clamp01(masterVolume);
        ApplyVolumeToMixerOrSource();
    }

    private void ConfigureSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = true;
        src.volume = 0f;
        src.outputAudioMixerGroup = musicGroup;
        src.spatialBlend = 0f; // 2D
    }

    // ----------------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------------

    /// <summary>Play a track by key, crossfading from the current one.</summary>
    public void Play(string key, float fadeSeconds = -1f)
    {
        if (!_map.TryGetValue(key, out var track) || track.clip == null) return;
        Play(track, fadeSeconds);
    }

    /// <summary>Play a track object, crossfading from the current one.</summary>
    public void Play(Track track, float fadeSeconds = -1f)
    {
        if (track == null || track.clip == null) return;
        if (fadeSeconds < 0f) fadeSeconds = defaultFade;

        var active = _usingA ? _a : _b;
        var idle = _usingA ? _b : _a;

        // Prepare next
        idle.clip = track.clip;
        idle.loop = track.loop;
        idle.volume = 0f;
        idle.mute = _isMuted;
        idle.Play();

        // Fade
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(Crossfade(active, idle, fadeSeconds, masterVolume * track.trackVolume));

        _usingA = !_usingA;

        // Reset or create a single-item playlist to allow Next/Prev semantics
        _playlist.Clear();
        _playlist.Add(track);
        _playlistIndex = 0;

        OnTrackChanged?.Invoke(track);
    }

    /// <summary>Stop music (fade out current track).</summary>
    public void Stop(float fadeOutSeconds = -1f)
    {
        if (fadeOutSeconds < 0f) fadeOutSeconds = defaultFade * 0.75f;
        var active = _usingA ? _a : _b;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutThenStop(active, fadeOutSeconds));
    }

    /// <summary>Pause or resume current music.</summary>
    public void SetPaused(bool paused)
    {
        var a = _usingA ? _a : _b;
        var b = _usingA ? _b : _a;

        if (paused) { a.Pause(); b.Pause(); }
        else { a.UnPause(); b.UnPause(); }

        OnPausedChanged?.Invoke(paused);
    }

    /// <summary>Set master music volume [0..1].</summary>
    public void SetVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        if (savePreferences) PlayerPrefs.SetFloat(PP_VOL, masterVolume);
        ApplyVolumeToMixerOrSource();
        // Re-apply to active source target
        var active = _usingA ? _a : _b;
        var t = GetCurrentTrack();
        float target = t != null ? masterVolume * t.trackVolume : masterVolume;
        active.volume = _isMuted ? 0f : target;
    }

    /// <summary>Mute/unmute music.</summary>
    public void SetMuted(bool muted)
    {
        _isMuted = muted;
        if (savePreferences) PlayerPrefs.SetInt(PP_MUTE, muted ? 1 : 0);
        _a.mute = muted; _b.mute = muted;
    }

    /// <summary>Start a playlist from a set of tracks/keys.</summary>
    public void StartPlaylist(IEnumerable<Track> list, bool shuffle = true, bool autoPlay = true, float fadeSeconds = -1f)
    {
        _playlist = new List<Track>();
        foreach (var t in list) if (t != null && t.clip != null) _playlist.Add(t);
        if (_playlist.Count == 0) return;

        if (shuffle) FisherYates(_playlist);

        _playlistIndex = 0;

        if (autoPlay) Play(_playlist[_playlistIndex], fadeSeconds);
    }

    public void StartPlaylist(IEnumerable<string> keys, bool shuffle = true, bool autoPlay = true, float fadeSeconds = -1f)
    {
        var list = new List<Track>();
        foreach (var k in keys) if (_map.TryGetValue(k, out var t)) list.Add(t);
        StartPlaylist(list, shuffle, autoPlay, fadeSeconds);
    }

    /// <summary>Advance to the next track in the playlist (or wrap if looping).</summary>
    public void Next(float fadeSeconds = -1f)
    {
        if (_playlist.Count == 0) return;
        int next = _playlistIndex + 1;
        if (next >= _playlist.Count)
        {
            if (!loopPlaylist) return;
            next = 0;
        }
        _playlistIndex = next;
        Play(_playlist[_playlistIndex], fadeSeconds);
    }

    /// <summary>Go to previous track (wraps if looping).</summary>
    public void Previous(float fadeSeconds = -1f)
    {
        if (_playlist.Count == 0) return;
        int prev = _playlistIndex - 1;
        if (prev < 0)
        {
            if (!loopPlaylist) return;
            prev = _playlist.Count - 1;
        }
        _playlistIndex = prev;
        Play(_playlist[_playlistIndex], fadeSeconds);
    }

    /// <summary>Currently playing track (if any).</summary>
    public Track GetCurrentTrack()
    {
        var src = _usingA ? _a : _b;
        if (!src || !src.isPlaying) return null;

        // Best effort: return from playlist index if valid and matching clip; else a lookup by clip.
        if (_playlistIndex >= 0 && _playlistIndex < _playlist.Count && _playlist[_playlistIndex].clip == src.clip)
            return _playlist[_playlistIndex];

        foreach (var t in tracks) if (t != null && t.clip == src.clip) return t;
        return null;
    }

    // ----------------------------------------------------------------------
    // Internals
    // ----------------------------------------------------------------------

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds, float toTargetVolume)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        to.volume = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = seconds > 0f ? t / seconds : 1f;
            if (to) to.volume = _isMuted ? 0f : Mathf.Lerp(0f, toTargetVolume, k);
            if (from) from.volume = _isMuted ? 0f : Mathf.Lerp(fromStart, 0f, k);
            yield return null;
        }

        if (to) to.volume = _isMuted ? 0f : toTargetVolume;
        if (from)
        {
            from.volume = 0f;
            if (from.isPlaying) from.Stop();
            from.clip = null;
        }
    }

    private IEnumerator FadeOutThenStop(AudioSource src, float seconds)
    {
        if (!src || !src.isPlaying) yield break;
        float t = 0f;
        float start = src.volume;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = seconds > 0f ? t / seconds : 1f;
            src.volume = _isMuted ? 0f : Mathf.Lerp(start, 0f, k);
            yield return null;
        }
        src.Stop();
        src.clip = null;
        src.volume = 0f;
    }

    private void ApplyVolumeToMixerOrSource()
    {
        if (mixer && !string.IsNullOrEmpty(mixerVolumeParam))
        {
            mixer.SetFloat(mixerVolumeParam, LinearToDb(masterVolume));
        }
        else
        {
            // No mixer: set on active source; crossfade coroutine will keep things consistent.
            var active = _usingA ? _a : _b;
            if (active) active.volume = _isMuted ? 0f : masterVolume;
        }
    }

    private static float LinearToDb(float x) => (x <= 0.0001f) ? -80f : 20f * Mathf.Log10(x);

    private static void FisherYates<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    // ----------------------------------------------------------------------
    // Convenience: quick helpers you can call from UI buttons
    // ----------------------------------------------------------------------

    public void UI_SetVolume(float v) => SetVolume(v);
    public void UI_SetMuted(bool m) => SetMuted(m);
    public void UI_PlayByKey(string key) => Play(key);
    public void UI_Stop() => Stop();
    public void UI_Next() => Next();
    public void UI_Prev() => Previous();
}
