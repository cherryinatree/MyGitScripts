using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(DecalProjector))]
public class DecalMouthAnimator : MonoBehaviour
{
    [Header("Decal")]
    public DecalProjector projector;
    [Tooltip("Template HDRP/Decal material. Will be cloned at runtime.")]
    public Material decalTemplate;
    [Tooltip("Mouth textures in order (e.g., Closed, A/I, E, O, U, etc.).")]
    public List<Texture2D> frames = new List<Texture2D>();

    [Header("Material Property")]
    [Tooltip("Texture property to swap on the decal material. HDRP Decal Base Map is usually _BaseColorMap.")]
    public string baseMapProperty = "_BaseColorMap";

    [Header("Audio (Optional)")]
    public AudioSource dialogueSource;
    [Tooltip("If true, when 'dialogueSource' starts playing, a matching sequence in 'library' will auto-play.")]
    public bool autoPlayWhenAudioStarts = true;
    [Tooltip("If a matching sequence has authoredLength > 0, stretch it to fill the AudioClip length.")]
    public bool stretchSequenceToClipLength = true;

    [Header("Idle")]
    [Tooltip("Frame to hold when not playing. -1 = leave whatever last frame is.")]
    public int idleFrameIndex = 0;

    [Header("Library (Optional)")]
    public List<NamedSequence> library = new List<NamedSequence>();

    [Serializable]
    public struct NamedSequence
    {
        public string id;               // optional custom key
        public AudioClip clip;          // optional direct match by clip
        public MouthSequence sequence;  // required for playback
    }

    [Header("Events")]
    public UnityEvent onPlaybackStarted;
    public UnityEvent onPlaybackFinished;

    Material _matInstance;
    Coroutine _playCo;
    MouthSequence _currentSeq;
    AudioSource _currentAudio;
    float _seqScale = 1f;  // used when stretching to clip length
    bool _wasPlayingLastFrame = false;

    void Reset()
    {
        projector = GetComponent<DecalProjector>();
    }

    void Awake()
    {
        if (!projector) projector = GetComponent<DecalProjector>();
        if (projector == null)
        {
            Debug.LogError($"{name}: Missing DecalProjector.");
            enabled = false; return;
        }
        if (decalTemplate == null)
        {
            Debug.LogWarning($"{name}: No decalTemplate assigned. Mouth animation will try to use projector.material as-is.");
        }
        // Make a unique material instance so we don't mutate a shared one
        if (decalTemplate)
        {
            _matInstance = new Material(decalTemplate);
            projector.material = _matInstance;
        }
        else
        {
            // If no template, ensure we are not editing a shared material
            _matInstance = projector.material; // Unity gives an instanced material here for DecalProjector
        }

        // Initialize to idle if desired
        if (idleFrameIndex >= 0) SetFrameSafe(idleFrameIndex);
    }

    void OnDestroy()
    {
        if (_matInstance && decalTemplate && Application.isPlaying)
        {
            Destroy(_matInstance);
        }
#if UNITY_EDITOR
        else if (_matInstance && decalTemplate && !Application.isPlaying)
        {
            DestroyImmediate(_matInstance);
        }
#endif
    }

    void Update()
    {
        // Auto trigger when the dialogue AudioSource starts
        if (autoPlayWhenAudioStarts && dialogueSource)
        {
            bool isPlayingNow = dialogueSource.isPlaying && dialogueSource.clip != null;
            if (isPlayingNow && !_wasPlayingLastFrame)
            {
                // Find best match by clip
                var seq = FindSequenceByClip(dialogueSource.clip);
                if (seq != null) Play(seq, dialogueSource);
            }
            _wasPlayingLastFrame = isPlayingNow;
        }
    }

    /// <summary>Play by custom ID (matches a library entry with same 'id'). Optionally attach an AudioSource.</summary>
    public void PlayById(string id, AudioSource audio = null)
        => Play(FindSequenceById(id), audio);

    /// <summary>Play by AudioClip (looks up a library entry whose 'clip' matches).</summary>
    public void PlayByClip(AudioClip clip, AudioSource audio = null)
        => Play(FindSequenceByClip(clip), audio);

    /// <summary>Play a specific sequence. If an AudioSource is provided, timing follows audioSource.time.</summary>
    public void Play(MouthSequence sequence, AudioSource audio = null)
    {
        if (sequence == null)
        {
            Debug.LogWarning($"{name}: Play called with null sequence.");
            return;
        }

        Stop();

        _currentSeq = sequence;
        _currentAudio = audio;

        // Compute stretch factor if we're aligning to the audio
        _seqScale = 1f;
        if (_currentAudio && _currentAudio.clip && stretchSequenceToClipLength)
        {
            float seqLen = Mathf.Max(0.0001f, _currentSeq.EffectiveLength());
            if (seqLen > 0f)
            {
                _seqScale = _currentAudio.clip.length / seqLen;
            }
        }

        _playCo = StartCoroutine(Co_Play());
        onPlaybackStarted?.Invoke();
    }

    /// <summary>Stop current playback and return to idle frame (if configured).</summary>
    public void Stop()
    {
        if (_playCo != null) StopCoroutine(_playCo);
        _playCo = null;
        _currentSeq = null;
        _currentAudio = null;

        if (idleFrameIndex >= 0) SetFrameSafe(idleFrameIndex);
    }

    IEnumerator Co_Play()
    {
        // Simple Update-driven sampler to ensure robust behaviour with scrubbing/pauses
        while (true)
        {
            float t = 0f;
            bool audioMode = _currentAudio && _currentAudio.clip;

            if (audioMode)
            {
                // Follow audio time; exit when audio finishes
                if (!_currentAudio.isPlaying && _currentAudio.time <= 0f)
                {
                    // Audio hasn't actually started yet; wait a frame
                    yield return null;
                    continue;
                }

                t = _currentAudio.time;
            }
            else
            {
                // No audio: we advance time ourselves
                t += Time.deltaTime; // This local t gets reset each loop, so keep an external timer
            }

            // We need a persistent timer for non-audio playback
            // Use a closure variable:
            if (!audioMode)
            {
                // Store in a field across frames
                _nonAudioTimer += Time.deltaTime;
                t = _nonAudioTimer;
            }

            float seqT = t / Mathf.Max(0.0001f, _seqScale); // map audio time back into authored timeline
            int idx = _currentSeq.FrameAt(seqT);
            if (idx >= 0) SetFrameSafe(idx);

            // Decide when to finish
            float endTime = _currentSeq.EffectiveLength() * _seqScale;

            bool done = false;
            if (audioMode)
            {
                // Finish when audio really stops OR sequence end reached (whichever comes first)
                if (!_currentAudio.isPlaying || _currentAudio.time >= endTime - 0.001f)
                    done = true;
            }
            else
            {
                if (t >= endTime - 0.001f)
                    done = true;
            }

            if (done) break;
            yield return null;
        }

        _nonAudioTimer = 0f;
        onPlaybackFinished?.Invoke();
        // Fall back to idle after finishing
        if (idleFrameIndex >= 0) SetFrameSafe(idleFrameIndex);
        _playCo = null;
        _currentSeq = null;
        _currentAudio = null;
    }

    float _nonAudioTimer = 0f;

    void SetFrameSafe(int frameIndex)
    {
        if (_matInstance == null) return;
        if (frames == null || frameIndex < 0 || frameIndex >= frames.Count) return;

        var tex = frames[frameIndex];
        if (tex == null) return;

        if (!_matInstance.HasProperty(baseMapProperty))
        {
            Debug.LogWarning($"{name}: Material missing '{baseMapProperty}'. Check your Decal shader property name.");
            return;
        }
        _matInstance.SetTexture(baseMapProperty, tex);
        // Optional: if your decal shader expects a color alpha to show base map, ensure it's visible
        // Example: _BaseColor often controls tint/opacity; uncomment if needed:
        // if (_matInstance.HasProperty("_BaseColor")) _matInstance.SetColor("_BaseColor", Color.white);
    }

    MouthSequence FindSequenceById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < library.Count; i++)
            if (!string.IsNullOrEmpty(library[i].id) && library[i].id == id)
                return library[i].sequence;
        return null;
    }

    MouthSequence FindSequenceByClip(AudioClip clip)
    {
        if (clip == null) return null;
        // First try exact clip match
        for (int i = 0; i < library.Count; i++)
            if (library[i].clip == clip && library[i].sequence != null)
                return library[i].sequence;

        // Fallback: try by name match
        for (int i = 0; i < library.Count; i++)
            if (library[i].clip == null && library[i].sequence != null && library[i].id == clip.name)
                return library[i].sequence;

        return null;
    }
}
