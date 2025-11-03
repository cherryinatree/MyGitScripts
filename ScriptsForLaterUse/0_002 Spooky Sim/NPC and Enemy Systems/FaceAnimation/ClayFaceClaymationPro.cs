using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Claymation face controller that swaps WHOLE GameObject frames
/// (e.g., meshes, quads, or Decal Projectors) to lip-sync speech.
/// Requirements:
/// - mouthGroup: ClaySwapGroup whose frames are ordered closed -> wide
/// - eyeGroups: one GameObject root per mood (children can be multiple frames if you like)
/// - blinkObject: a closed-eyelid mesh/quad shown briefly during blinks
public class ClayFaceClaymationPro : MonoBehaviour
{
    public enum ClayMood { Neutral, Happy, Sad, Angry, Surprised, Thinking, Anxious }

    [Header("Audio / Refs")]
    public AudioSource voice;
    public ClaySwapGroup mouthGroup;

    [System.Serializable] public class MoodBinding { public ClayMood mood; public GameObject groupRoot; }
    [Header("Eyes")]
    public List<MoodBinding> eyeGroups = new();
    public GameObject blinkObject;

    [Header("Mouth Analysis (Amplitude)")]
    [Range(0.01f, 0.05f)] public float windowSec = 0.02f;   // ~20ms analysis
    [Range(0.0f, 0.2f)] public float noiseGate = 0.02f;    // ignore room noise
    [Range(0.1f, 10f)] public float baseGain = 1.8f;      // coarse gain; AGC refines it
    [Range(0.1f, 4f)] public float gamma = 1.2f;         // >1 closes more, <1 opens more
    [Range(0.0f, 0.5f)] public float attackSec = 0.06f;    // faster open
    [Range(0.0f, 0.5f)] public float releaseSec = 0.12f;   // slower close
    public bool closeWhenSilent = true;
    [Range(0.0f, 0.5f)] public float silentCloseDelay = 0.10f;

    [Header("Adaptive Gain (AGC)")]
    [Range(0.2f, 1.0f)] public float agcTarget = 0.8f;     // target normalized peak
    [Range(0.05f, 1.0f)] public float agcSpeed = 0.4f;      // adaptation speed

    [Header("Clay Quantization / Stability")]
    [Range(4f, 24f)] public float clayFPS = 12f;        // max swap cadence
    [Range(0f, 0.2f)] public float minHoldSec = 0.06f;   // keep a frame at least this long
    [Range(0f, 0.5f)] public float visualDelaySec = 0.04f; // queue delay to sync with audio
    [Range(0f, 1f)] public float hysteresis = 0.15f;   // extra “openness” to step up

    [Header("Optional: Vowel Heuristics (Spectrum)")]
    public bool useVowelHeuristics = true;
    [Tooltip("Index in mouthGroup frames for a rounded O/U frame; -1 = disable")]
    public int frameO = -1;
    [Tooltip("Index in mouthGroup frames for a spread EE frame; -1 = disable")]
    public int frameEE = -1;
    [Range(0f, 1f)] public float vowelBias = 0.35f;
    [Range(256, 4096)] public int spectrumSize = 1024;

    [Header("Blink")]
    public Vector2 blinkEvery = new Vector2(2.5f, 6f);
    public float blinkDuration = 0.085f;

    [Header("Clay Jitter (on swap)")]
    public Transform mouthJitterHandle;        // if null, uses mouthGroup.transform
    public float jitterPos = 0.0008f;          // meters
    public float jitterRot = 1.2f;             // degrees

    // ----- internal state -----
    Dictionary<ClayMood, GameObject> _eyesByMood = new();
    ClayMood _currentMood = ClayMood.Neutral;

    float _env, _agcPeak = 0.2f, _silentT;
    float _tickHold, _tickTimer, _holdTimer;
    int _lastIdx = -1;

    Queue<int> _delayQ;
    float[] _timeBuf, _specBuf;

    // spectrum bands (bin edges)
    int _binLoEnd, _binMidEnd, _binHiEnd;
    float _binHz;

    Vector3 _mouthBasePos; Quaternion _mouthBaseRot;
    bool _blinking; float _nextBlinkAt;

    void Awake()
    {
        if (!mouthGroup) Debug.LogWarning($"{name}: ClayFaceClaymationPro needs a mouthGroup (ClaySwapGroup).");

        foreach (var b in eyeGroups) if (b != null && b.groupRoot) _eyesByMood[b.mood] = b.groupRoot;

        if (mouthJitterHandle == null && mouthGroup) mouthJitterHandle = mouthGroup.transform;
        if (mouthJitterHandle) { _mouthBasePos = mouthJitterHandle.localPosition; _mouthBaseRot = mouthJitterHandle.localRotation; }

        // buffers
        int sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        int tSamples = Mathf.Clamp(Mathf.RoundToInt(windowSec * sr), 64, 2048);
        _timeBuf = new float[tSamples];
        _specBuf = new float[Mathf.ClosestPowerOfTwo(Mathf.Clamp(spectrumSize, 256, 4096))];

        // spectrum calibration
        _binHz = (sr * 0.5f) / _specBuf.Length;
        _binLoEnd = Mathf.Clamp(Mathf.RoundToInt(500f / _binHz), 1, _specBuf.Length - 1);
        _binMidEnd = Mathf.Clamp(Mathf.RoundToInt(2000f / _binHz), 1, _specBuf.Length - 1);
        _binHiEnd = Mathf.Clamp(Mathf.RoundToInt(6000f / _binHz), 1, _specBuf.Length - 1);

        _tickHold = 1f / Mathf.Max(1f, clayFPS);
        _delayQ = new Queue<int>(64);

        ApplyMood(_currentMood, true);
        ScheduleNextBlink();
        if (blinkObject) blinkObject.SetActive(false);
    }

    void Update()
    {
        UpdateMouth(Time.deltaTime);
        UpdateBlink();
    }

    // ---------------- Mouth ----------------
    void UpdateMouth(float dt)
    {
        if (mouthGroup == null || mouthGroup.Count == 0) return;

        // 1) Measure RMS envelope
        float targetEnv = 0f;
        if (voice && voice.isPlaying)
        {
            voice.GetOutputData(_timeBuf, 0);
            targetEnv = Mathf.Max(0f, RMS(_timeBuf) - noiseGate);

            // AGC update toward peak
            _agcPeak = Mathf.Lerp(_agcPeak, Mathf.Max(_agcPeak, targetEnv + 1e-5f), agcSpeed * dt);
            float gain = baseGain * (_agcPeak > 1e-4f ? (agcTarget / _agcPeak) : 1f);

            // attack / release smoothing
            float openLerp = attackSec <= 1e-4f ? 1f : Mathf.Clamp01(dt / attackSec);
            float closeLerp = releaseSec <= 1e-4f ? 1f : Mathf.Clamp01(dt / releaseSec);
            float amplified = targetEnv * gain;
            _env = amplified > _env ? Mathf.Lerp(_env, amplified, openLerp)
                                    : Mathf.Lerp(_env, amplified, closeLerp);

            _silentT = 0f;
        }
        else
        {
            _silentT += dt;
            if (closeWhenSilent && _silentT >= silentCloseDelay)
                _env = Mathf.Lerp(_env, 0f, 0.25f);
        }

        // non-linear mapping
        float openness = Mathf.Pow(Mathf.Clamp01(_env), gamma);

        // 2) Base index from openness
        int desired = Mathf.RoundToInt(Mathf.Lerp(0, mouthGroup.Count - 1, openness));
        desired = Mathf.Clamp(desired, 0, mouthGroup.Count - 1);

        // 3) Optional vowel heuristics (O vs EE)
        if (useVowelHeuristics && voice && voice.isPlaying && (frameO >= 0 || frameEE >= 0))
        {
            voice.GetSpectrumData(_specBuf, 0, FFTWindow.BlackmanHarris);
            (float lo, float mid, float hi) = BandEnergies(_specBuf, _binLoEnd, _binMidEnd, _binHiEnd);
            float total = lo + mid + hi + 1e-6f;
            float loR = lo / total, hiR = hi / total;

            float biasO = (frameO >= 0) ? Mathf.Clamp01((loR - 0.25f) * 3.0f) : 0f;
            float biasEE = (frameEE >= 0) ? Mathf.Clamp01((hiR - 0.22f) * 3.2f) : 0f;

            if (biasO > 0f || biasEE > 0f)
            {
                int target = desired; float weight = 0f;
                if (biasO >= biasEE) { target = frameO; weight = biasO * vowelBias; }
                else { target = frameEE; weight = biasEE * vowelBias; }
                desired = Mathf.RoundToInt(Mathf.Lerp(desired, Mathf.Clamp(target, 0, mouthGroup.Count - 1), weight));
            }
        }

        // 4) Hysteresis + min hold + clay FPS tick
        _tickTimer += dt;
        _holdTimer += dt;

        int stable = ApplyHysteresis(desired, _lastIdx, hysteresis, mouthGroup.Count);

        bool canSwap = (_tickTimer >= _tickHold) && (_holdTimer >= minHoldSec);
        int queued = canSwap ? stable : (_lastIdx >= 0 ? _lastIdx : stable);

        // 5) Visual delay (queue)
        int delayFrames = Mathf.RoundToInt(visualDelaySec / Mathf.Max(0.001f, Time.deltaTime));
        if (delayFrames > 0)
        {
            _delayQ.Enqueue(queued);
            while (_delayQ.Count > delayFrames) queued = _delayQ.Dequeue();
        }

        // 6) Apply swap
        if (queued != _lastIdx)
        {
            mouthGroup.SetIndex(queued);
            _lastIdx = queued;
            _tickTimer = 0f;
            _holdTimer = 0f;
            ApplyMouthJitter();
        }
    }

    void ApplyMouthJitter()
    {
        if (!mouthJitterHandle) return;
        mouthJitterHandle.localPosition = _mouthBasePos + new Vector3(
            Random.Range(-jitterPos, jitterPos),
            Random.Range(-jitterPos, jitterPos),
            Random.Range(-jitterPos, jitterPos) * 0.25f);
        mouthJitterHandle.localRotation = _mouthBaseRot *
            Quaternion.Euler(
                Random.Range(-jitterRot, jitterRot),
                Random.Range(-jitterRot, jitterRot),
                Random.Range(-jitterRot, jitterRot) * 0.35f);
    }

    // ---------------- Eyes / Mood ----------------
    public void ApplyMood(ClayMood mood, bool immediate = false)
    {
        _currentMood = mood;
        foreach (var kv in _eyesByMood)
            if (kv.Value) kv.Value.SetActive(kv.Key == mood);
        if (immediate) _blinking = false;
    }

    // ---------------- Blink ----------------
    void ScheduleNextBlink() => _nextBlinkAt = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);

    void UpdateBlink()
    {
        if (!blinkObject || _blinking) return;
        if (Time.time >= _nextBlinkAt) StartCoroutine(BlinkCo());
    }

    IEnumerator BlinkCo()
    {
        _blinking = true;
        GameObject activeMoodGO = _eyesByMood.TryGetValue(_currentMood, out var go) ? go : null;
        if (activeMoodGO) activeMoodGO.SetActive(false);
        blinkObject.SetActive(true);

        yield return new WaitForSeconds(blinkDuration);

        blinkObject.SetActive(false);
        if (activeMoodGO) activeMoodGO.SetActive(true);
        _blinking = false;
        ScheduleNextBlink();
    }

    // ---------------- Public API ----------------
    public void Speak(AudioClip clip, float volume = 1f)
    {
        if (!voice || !clip) return;
        _agcPeak = 0.2f;       // reset AGC for new clip
        _silentT = 0f;
        voice.Stop(); voice.clip = clip; voice.volume = volume; voice.Play();
    }

    // ---------- helpers ----------
    static float RMS(float[] x)
    {
        double s = 0; for (int i = 0; i < x.Length; i++) s += x[i] * x[i];
        return Mathf.Sqrt((float)(s / Mathf.Max(1, x.Length)));
    }

    static (float lo, float mid, float hi) BandEnergies(float[] spec, int loEnd, int midEnd, int hiEnd)
    {
        float lo = 0, mid = 0, hi = 0;
        for (int i = 0; i < spec.Length; i++)
        {
            float v = spec[i];
            if (i < loEnd) lo += v;
            else if (i < midEnd) mid += v;
            else if (i < hiEnd) hi += v;
        }
        return (lo, mid, hi);
    }

    static int ApplyHysteresis(int desired, int last, float hysteresis, int count)
    {
        if (last < 0) return desired;
        if (desired == last) return desired;

        if (desired > last)
        {
            float need = last + 1 + hysteresis * (count - 1);
            return desired >= Mathf.CeilToInt(need) ? desired : last;
        }
        else
        {
            float need = last - 1 - hysteresis * (count - 1) * 0.35f;
            return desired <= Mathf.FloorToInt(need) ? desired : last;
        }
    }
}
