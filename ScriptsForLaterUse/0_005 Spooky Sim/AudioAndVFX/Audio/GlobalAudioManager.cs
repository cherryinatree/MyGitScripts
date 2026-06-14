using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [Header("Mixer Groups (optional but recommended)")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Pooling")]
    [SerializeField, Min(4)] private int initialPoolSize = 24;
    [SerializeField, Min(4)] private int maxPoolSize = 64;

    [Header("Global Limits")]
    [SerializeField, Min(0)] private int maxSimultaneousOneShots = 32;

    private readonly List<AudioSource> _free = new();
    private readonly List<ActiveOneShot> _activeOneShots = new();
    private readonly Dictionary<int, ActiveLoop> _loops = new();                 // handleId -> loop
    private readonly Dictionary<int, float> _lastPlayTime = new();              // soundDefId -> time
    private readonly Dictionary<int, int> _playingCountBySound = new();         // soundDefId -> count

    private int _nextHandleId = 1;

    private struct ActiveOneShot
    {
        public AudioSource src;
        public float endTime;
        public int soundDefId;
    }

    public struct LoopHandle
    {
        public int id;
        public bool IsValid => id != 0;
    }

    private class ActiveLoop
    {
        public AudioSource src;
        public Transform follow;
        public Vector3 offset;
        public int soundDefId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        WarmPool();
    }

    private void WarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            _free.Add(CreatePooledSource());
    }

    private AudioSource CreatePooledSource()
    {
        var go = new GameObject("AudioSrc");
        go.transform.SetParent(transform, false);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0f; // usually better for games unless you want doppler
        return src;
    }

    private AudioSource GetSource()
    {
        if (_free.Count > 0)
        {
            var s = _free[^1];
            _free.RemoveAt(_free.Count - 1);
            return s;
        }

        int total = _free.Count + _activeOneShots.Count + _loops.Count;
        if (total < maxPoolSize)
            return CreatePooledSource();

        // Pool exhausted: reuse the oldest one-shot if allowed
        if (_activeOneShots.Count > 0)
        {
            var oldest = _activeOneShots[0];
            _activeOneShots.RemoveAt(0);
            SafeStop(oldest.src);
            DecrementPlaying(oldest.soundDefId);
            return oldest.src;
        }

        return null; // all loops and no one-shots to steal
    }

    private void ReleaseSource(AudioSource src)
    {
        if (src == null) return;
        SafeStop(src);
        src.transform.SetParent(transform, false);
        _free.Add(src);
    }

    private static void SafeStop(AudioSource src)
    {
        src.Stop();
        src.clip = null;
        src.loop = false;
        src.outputAudioMixerGroup = null;
        src.transform.localPosition = Vector3.zero;
        src.transform.localRotation = Quaternion.identity;
    }

    private AudioMixerGroup ResolveGroup(SoundDefinition def)
    {
        if (def.outputOverride != null) return def.outputOverride;

        return def.category switch
        {
            SoundCategory.UI => uiGroup,
            SoundCategory.Ambience => ambienceGroup,
            SoundCategory.Music => musicGroup,
            _ => sfxGroup
        };
    }

    private bool CanPlay(SoundDefinition def)
    {
        if (def == null) return false;

        int id = def.GetInstanceID();

        // cooldown
        if (def.cooldown > 0f && _lastPlayTime.TryGetValue(id, out float last))
        {
            if (Time.unscaledTime - last < def.cooldown)
                return false;
        }

        // polyphony
        if (def.maxPolyphony > 0 && _playingCountBySound.TryGetValue(id, out int count))
        {
            if (count >= def.maxPolyphony)
                return false;
        }

        return true;
    }

    private void MarkPlayed(SoundDefinition def)
    {
        int id = def.GetInstanceID();
        _lastPlayTime[id] = Time.unscaledTime;
        if (!_playingCountBySound.ContainsKey(id)) _playingCountBySound[id] = 0;
        _playingCountBySound[id]++;
    }

    private void DecrementPlaying(int soundDefId)
    {
        if (_playingCountBySound.TryGetValue(soundDefId, out int count))
        {
            count--;
            if (count <= 0) _playingCountBySound.Remove(soundDefId);
            else _playingCountBySound[soundDefId] = count;
        }
    }

    private void Update()
    {
        // release finished one-shots
        float now = Time.unscaledTime;
        for (int i = _activeOneShots.Count - 1; i >= 0; i--)
        {
            var a = _activeOneShots[i];
            if (a.src == null || (!a.src.isPlaying && now >= a.endTime))
            {
                _activeOneShots.RemoveAt(i);
                DecrementPlaying(a.soundDefId);
                ReleaseSource(a.src);
            }
        }

        // update loop followers
        foreach (var kv in _loops)
        {
            var loop = kv.Value;
            if (loop?.src == null) continue;
            if (loop.follow != null)
                loop.src.transform.position = loop.follow.position + loop.offset;
        }
    }

    /// <summary>Play a one-shot (2D/3D controlled by SoundDefinition.spatialBlend).</summary>
    public void PlayOneShot(SoundDefinition def, Vector3 worldPos, Transform follow = null, Vector3 followOffset = default)
    {
        if (!CanPlay(def)) return;

        // global one-shot cap
        if (maxSimultaneousOneShots > 0 && _activeOneShots.Count >= maxSimultaneousOneShots)
            return;

        var clip = def.GetRandomClip();
        if (clip == null) return;

        var src = GetSource();
        if (src == null) return;

        ConfigureSource(src, def, clip, isLoop: false);
        src.transform.position = worldPos;

        MarkPlayed(def);

        // optionally follow for moving emitters (usually loops, but supported here)
        if (follow != null)
            src.transform.position = follow.position + followOffset;

        src.Play();

        // estimate end time (handles pitch)
        float pitchAbs = Mathf.Max(0.01f, Mathf.Abs(src.pitch));
        float duration = clip.length / pitchAbs;

        _activeOneShots.Add(new ActiveOneShot
        {
            src = src,
            endTime = Time.unscaledTime + duration + 0.05f,
            soundDefId = def.GetInstanceID()
        });
    }

    /// <summary>Convenience: play a 2D one-shot (ignores world position).</summary>
    public void PlayOneShot2D(SoundDefinition def)
    {
        if (def == null) return;
        // temporarily treat as 2D by overriding spatialBlend on the AudioSource
        PlayOneShotOverride(def, Vector3.zero, spatialBlendOverride: 0f);
    }

    private void PlayOneShotOverride(SoundDefinition def, Vector3 worldPos, float spatialBlendOverride)
    {
        if (!CanPlay(def)) return;

        if (maxSimultaneousOneShots > 0 && _activeOneShots.Count >= maxSimultaneousOneShots)
            return;

        var clip = def.GetRandomClip();
        if (clip == null) return;

        var src = GetSource();
        if (src == null) return;

        ConfigureSource(src, def, clip, isLoop: false);
        src.spatialBlend = spatialBlendOverride;
        src.transform.position = worldPos;

        MarkPlayed(def);

        src.Play();

        float pitchAbs = Mathf.Max(0.01f, Mathf.Abs(src.pitch));
        float duration = clip.length / pitchAbs;

        _activeOneShots.Add(new ActiveOneShot
        {
            src = src,
            endTime = Time.unscaledTime + duration + 0.05f,
            soundDefId = def.GetInstanceID()
        });
    }

    /// <summary>Start a loop that follows a transform. Returns a handle you can stop later.</summary>
    public LoopHandle StartLoop(SoundDefinition def, Transform follow, Vector3 offset = default, float fadeIn = 0f)
    {
        if (def == null) return default;
        if (!def.loop) { /* still allowed */ }

        if (!CanPlay(def)) return default;

        var clip = def.GetRandomClip();
        if (clip == null) return default;

        var src = GetSource();
        if (src == null) return default;

        ConfigureSource(src, def, clip, isLoop: true);
        src.transform.position = follow ? follow.position + offset : Vector3.zero;

        MarkPlayed(def);

        int handleId = _nextHandleId++;
        _loops[handleId] = new ActiveLoop
        {
            src = src,
            follow = follow,
            offset = offset,
            soundDefId = def.GetInstanceID()
        };

        if (fadeIn > 0f)
        {
            float targetVol = src.volume;
            src.volume = 0f;
            src.Play();
            StartCoroutine(FadeVolume(src, 0f, targetVol, fadeIn));
        }
        else
        {
            src.Play();
        }

        return new LoopHandle { id = handleId };
    }

    public void StopLoop(LoopHandle handle, float fadeOut = 0f)
    {
        if (!handle.IsValid) return;
        if (!_loops.TryGetValue(handle.id, out var loop) || loop?.src == null)
        {
            _loops.Remove(handle.id);
            return;
        }

        _loops.Remove(handle.id);
        DecrementPlaying(loop.soundDefId);

        if (fadeOut > 0f && gameObject.activeInHierarchy)
        {
            StartCoroutine(StopLoopWithFade(loop.src, fadeOut));
        }
        else
        {
            ReleaseSource(loop.src);
        }
    }

    public void StopAllLoops(float fadeOut = 0f)
    {
        var keys = new List<int>(_loops.Keys);
        foreach (var k in keys)
            StopLoop(new LoopHandle { id = k }, fadeOut);
    }

    private void ConfigureSource(AudioSource src, SoundDefinition def, AudioClip clip, bool isLoop)
    {
        src.clip = clip;
        src.loop = isLoop;

        float vJ = def.volumeJitter;
        float pJ = def.pitchJitter;

        src.volume = Mathf.Clamp01(def.volume + UnityEngine.Random.Range(-vJ, vJ));
        src.pitch = def.pitch + UnityEngine.Random.Range(-pJ, pJ);

        src.spatialBlend = def.spatialBlend;
        src.minDistance = Mathf.Max(0.01f, def.minDistance);
        src.maxDistance = Mathf.Max(src.minDistance, def.maxDistance);

        src.outputAudioMixerGroup = ResolveGroup(def);
        src.priority = 128; // tweak if you want; lower = higher priority in Unity
    }

    private System.Collections.IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        if (src == null) yield break;
        float t = 0f;
        while (t < time && src != null)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / time);
            src.volume = Mathf.Lerp(from, to, a);
            yield return null;
        }
        if (src != null) src.volume = to;
    }

    private System.Collections.IEnumerator StopLoopWithFade(AudioSource src, float fadeOut)
    {
        if (src == null) yield break;
        float start = src.volume;
        float t = 0f;
        while (t < fadeOut && src != null)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeOut);
            src.volume = Mathf.Lerp(start, 0f, a);
            yield return null;
        }
        if (src != null) ReleaseSource(src);
    }
}
