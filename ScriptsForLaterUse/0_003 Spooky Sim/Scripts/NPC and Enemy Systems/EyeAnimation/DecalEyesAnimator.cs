using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Animates eye decals using authored timelines (MouthSequence) and EyeSet mood data.
/// - Random auto-blink between min/max seconds
/// - Change mood with SetMood(id or asset)
/// - Play expressions by name (loop or one-shot), optionally suppressing blinks during playback
/// - Uses HDRP DecalProjector by swapping a texture property (default: _BaseColorMap)
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class DecalEyesAnimator : MonoBehaviour
{
    [Header("Decal")]
    public DecalProjector projector;
    [Tooltip("Template HDRP/Decal material (cloned at runtime).")]
    public Material decalTemplate;
    [Tooltip("Ordered list of eye frames (textures). Indices are referenced by EyeSet and MouthSequence steps.")]
    public List<Texture2D> frames = new List<Texture2D>();
    [Tooltip("Decal texture property to swap. HDRP Decal Base Map is usually _BaseColorMap.")]
    public string baseMapProperty = "_BaseColorMap";

    [Header("Library / Mood")]
    public List<EyeSet> sets = new List<EyeSet>();
    [Tooltip("Mood id to apply on Awake (must match EyeSet.id).")]
    public string startMoodId = "Neutral";

    [Header("Blinking")]
    [Min(0.05f)] public float blinkEveryMin = 2.5f;
    [Min(0.05f)] public float blinkEveryMax = 5.0f;
    [Range(0f, 1f)] public float doubleBlinkChance = 0.15f;
    [Tooltip("If true, random blink timer uses unscaled time (ignore Time.timeScale).")]
    public bool useUnscaledTimeForBlinks = false;

    [Header("Events")]
    public UnityEvent onBlinkStarted;
    public UnityEvent onBlinkFinished;
    public UnityEvent onExpressionStarted;
    public UnityEvent onExpressionFinished;

    Material _matInstance;
    EyeSet _currentSet;
    string _currentMoodId;
    string _activeExpressionName = null;

    Coroutine _blinkCo;
    Coroutine _exprCo;
    bool _isBlinking = false;
    bool _allowBlink = true; // controlled by expression's allowBlinkDuring

    void Reset()
    {
        projector = GetComponent<DecalProjector>();
    }

    void Awake()
    {
        if (!projector) projector = GetComponent<DecalProjector>();

        // Material instance per character
        if (decalTemplate)
        {
            _matInstance = new Material(decalTemplate);
            projector.material = _matInstance;
        }
        else
        {
            _matInstance = projector.material; // DecalProjector returns an instanced mat in play mode
        }

        // Start mood
        if (!string.IsNullOrEmpty(startMoodId))
            SetMood(startMoodId);
        else if (sets.Count > 0)
            SetMood(sets[0]);

        // Fallback to first frame if no set yet
        if (_currentSet == null && frames.Count > 0)
            SetFrameSafe(0);
    }

    void OnEnable()
    {
        StartBlinking();
    }

    void OnDisable()
    {
        StopBlinking();
        StopExpression();
    }

    void OnDestroy()
    {
        if (_matInstance && decalTemplate && Application.isPlaying)
            Destroy(_matInstance);
#if UNITY_EDITOR
        else if (_matInstance && decalTemplate && !Application.isPlaying)
            DestroyImmediate(_matInstance);
#endif
    }

    // ---------- Public API ----------

    public void SetMood(string id)
    {
        EyeSet found = null;
        for (int i = 0; i < sets.Count; i++)
            if (sets[i] && sets[i].id == id) { found = sets[i]; break; }

        if (found == null)
        {
            Debug.LogWarning($"{name}: Eye mood '{id}' not found.");
            return;
        }
        SetMood(found);
    }

    public void SetMood(EyeSet setAsset)
    {
        if (setAsset == null) return;
        _currentSet = setAsset;
        _currentMoodId = setAsset.id;
        _activeExpressionName = null;
        _allowBlink = true;
        StopExpression();
        // Show the mood's default open frame
        SetFrameSafe(_currentSet.openFrameIndex);
    }

    /// <summary>Play a named expression from the current mood. If it loops, it will continue until StopExpression() or mood change.</summary>
    public void PlayExpression(string expressionName)
    {
        if (_currentSet == null)
        {
            Debug.LogWarning($"{name}: No EyeSet/mood active. Cannot play expression.");
            return;
        }

        var seq = _currentSet.GetExpression(expressionName);
        if (seq == null)
        {
            Debug.LogWarning($"{name}: Expression '{expressionName}' not found in mood '{_currentSet.id}'.");
            return;
        }

        bool loop = _currentSet.ExpressionLoops(expressionName);
        bool allowBlinkDuring = _currentSet.ExpressionAllowsBlink(expressionName);

        StopExpression(); // stop any previous one

        _activeExpressionName = expressionName;
        _allowBlink = allowBlinkDuring;
        _exprCo = StartCoroutine(Co_PlaySequence(seq, loop, onFinish: () =>
        {
            _activeExpressionName = null;
            _allowBlink = true;
            onExpressionFinished?.Invoke();
            // Return to mood's default open when a non-looping expression completes
            if (_currentSet != null) SetFrameSafe(_currentSet.openFrameIndex);
        }));

        onExpressionStarted?.Invoke();
    }

    /// <summary>Stop any active expression and return to the mood's open eyes.</summary>
    public void StopExpression()
    {
        if (_exprCo != null) StopCoroutine(_exprCo);
        _exprCo = null;
        _activeExpressionName = null;
        _allowBlink = true;
        if (_currentSet != null) SetFrameSafe(_currentSet.openFrameIndex);
    }

    /// <summary>Trigger an immediate blink (respects allowBlink flag unless override is true).</summary>
    public void ForceBlink(bool overrideSuppression = false)
    {
        if (_isBlinking) return;
        if (!_allowBlink && !overrideSuppression) return;
        if (_currentSet == null || _currentSet.blinkSequence == null) return;

        StartCoroutine(Co_BlinkOnce());
    }

    public void StartBlinking()
    {
        if (_blinkCo != null) return;
        _blinkCo = StartCoroutine(Co_BlinkLoop());
    }

    public void StopBlinking()
    {
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = null;
    }

    // ---------- Internals ----------

    IEnumerator Co_BlinkLoop()
    {
        var wfsScaled = new WaitForSeconds(0.1f); // reused for tiny delays
        while (enabled)
        {
            float wait = Random.Range(Mathf.Min(blinkEveryMin, blinkEveryMax),
                                      Mathf.Max(blinkEveryMin, blinkEveryMax));

            if (useUnscaledTimeForBlinks)
                yield return WaitForSecondsUnscaled(wait);
            else
                yield return new WaitForSeconds(wait);

            if (!_allowBlink || _isBlinking || _currentSet == null || _currentSet.blinkSequence == null)
                continue;

            yield return Co_BlinkOnce();

            // Small chance of a quick double blink
            if (Random.value < doubleBlinkChance && _allowBlink && !_isBlinking)
            {
                if (useUnscaledTimeForBlinks)
                    yield return WaitForSecondsUnscaled(Random.Range(0.05f, 0.2f));
                else
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));

                if (_allowBlink && !_isBlinking)
                    yield return Co_BlinkOnce();
            }
        }
    }

    IEnumerator Co_BlinkOnce()
    {
        if (_currentSet == null || _currentSet.blinkSequence == null) yield break;

        _isBlinking = true;
        onBlinkStarted?.Invoke();

        float t = 0f;
        float length = _currentSet.blinkSequence.EffectiveLength();
        if (length <= 0f) length = 0.12f; // fallback to a quick blink if not authored

        while (t < length)
        {
            float seqT = t;
            int idx = _currentSet.blinkSequence.FrameAt(seqT);
            if (idx >= 0) SetFrameSafe(idx);

            yield return null;
            t += Time.deltaTime;
        }

        // Finalize on the open frame for safety
        SetFrameSafe(_currentSet.openFrameIndex);

        _isBlinking = false;
        onBlinkFinished?.Invoke();
    }

    IEnumerator Co_PlaySequence(MouthSequence sequence, bool loop, System.Action onFinish)
    {
        if (sequence == null) yield break;

        do
        {
            float t = 0f;
            float length = Mathf.Max(0.0001f, sequence.EffectiveLength());
            while (t < length)
            {
                // If blink is running, let it override the frame this frame
                if (!_isBlinking)
                {
                    int idx = sequence.FrameAt(t);
                    if (idx >= 0) SetFrameSafe(idx);
                }

                yield return null;
                t += Time.deltaTime;
            }
        }
        while (loop);

        onFinish?.Invoke();
    }

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

        // Ensure visibility if your decal shader tints by _BaseColor alpha (common for HDRP Decal)
        if (_matInstance.HasProperty("_BaseColor"))
        {
            var c = _matInstance.GetColor("_BaseColor");
            if (c.a <= 0f) _matInstance.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1f));
        }
    }

    // Utility: unscaled wait (without allocations every call)
    static IEnumerator WaitForSecondsUnscaled(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }
}
