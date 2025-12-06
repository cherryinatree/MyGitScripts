using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

/// Drop-in upgrade for better lip sync with Decal Projector mouth frames.
/// - Amplitude → openness (RMS, attack/release, AGC)
/// - Optional spectral heuristics to bias "O" (rounded) or "EE" (spread)
/// - Clay-style quantization, hysteresis, hold, and visual delay
public class DecalMouthTalk : MonoBehaviour
{
    [Header("Scene Refs")]
    public DecalProjector projector;
    public Material decalTemplate;
    public List<Texture2D> frames;           // ordered: closed → wide
    public AudioSource voice;

    [Header("Amplitude Mapping")]
    [Range(0.01f, 1.0f)] public float windowSec = 0.02f;     // ~20ms analysis
    [Range(0.0f, 0.2f)] public float noiseGate = 0.02f;      // ignore room noise
    [Range(0.1f, 10f)] public float baseGain = 1.8f;        // start gain (AGC will adapt)
    [Range(0.1f, 4f)] public float gamma = 1.2f;           // non-linear curve: >1 = more closed, <1 = more open
    [Range(0.0f, 0.5f)] public float attackSec = 0.06f;      // how fast mouth opens
    [Range(0.0f, 0.5f)] public float releaseSec = 0.12f;     // how fast mouth closes
    public bool closeWhenSilent = true;
    [Range(0.0f, 0.5f)] public float silentCloseDelay = 0.10f;

    [Header("Adaptive Gain Control (AGC)")]
    [Tooltip("Target normalized peak (post-AGC). 0.8 recommended.")]
    [Range(0.2f, 1.0f)] public float agcTarget = 0.8f;
    [Tooltip("How quickly AGC adapts; lower = steadier. 0.3–0.6 works well.")]
    [Range(0.05f, 1.0f)] public float agcSpeed = 0.4f;

    [Header("Clay Quantization / Stability")]
    [Range(4f, 24f)] public float clayFPS = 12f;
    [Range(0f, 0.2f)] public float minHoldSec = 0.06f;     // min time to keep a frame
    [Range(0f, 0.5f)] public float visualDelaySec = 0.04f; // delay to align with audio buffer
    [Range(0f, 1f)] public float hysteresis = 0.15f;     // extra openness needed to switch up; prevents flicker

    [Header("Optional: Vowel Heuristics (Spectrum)")]
    public bool useVowelHeuristics = true;
    [Tooltip("Index of an O/rounded frame in 'frames' (set -1 to disable).")]
    public int frameO = -1;
    [Tooltip("Index of an EE/spread frame in 'frames' (set -1 to disable).")]
    public int frameEE = -1;
    [Range(0f, 1f)] public float vowelBias = 0.35f;      // how strongly to lean O/EE
    [Range(256, 4096)] public int spectrumSize = 1024;      // power of two
    [Tooltip("Assumed analysis sample rate (fallback if system SR not known).")]
    public int assumedSampleRate = 48000;

    Material _mat; int _lastIdx = -1;
    float _env; float _agcPeak = 0.2f; float _silentT;
    float _tickHold; float _holdTimer;
    float _delayTimer;
    Queue<int> _delayQ;
    float[] _timeBuf; float[] _specBuf;

    // cached spectrum band bins
    int _binLoEnd, _binMidEnd, _binHiEnd;
    float _binHz; // hz per bin

    void Awake()
    {
        if (projector && decalTemplate) _mat = projector.material = new Material(decalTemplate);
        _tickHold = 1f / Mathf.Max(1f, clayFPS);
        _delayQ = new Queue<int>(Mathf.CeilToInt(0.5f / Mathf.Max(0.001f, Time.fixedDeltaTime)));

        // buffers
        int tSamples = Mathf.Clamp(NextPow2(Mathf.Max(64, Mathf.RoundToInt(windowSec * AudioSettings.outputSampleRate))), 64, 2048);
        _timeBuf = new float[tSamples];
        _specBuf = new float[Mathf.ClosestPowerOfTwo(spectrumSize)];

        // spectrum bin calibration
        int sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : assumedSampleRate;
        _binHz = (sr * 0.5f) / _specBuf.Length;
        _binLoEnd = Mathf.Clamp(Mathf.RoundToInt(500f / _binHz), 1, _specBuf.Length - 1);
        _binMidEnd = Mathf.Clamp(Mathf.RoundToInt(2000f / _binHz), 1, _specBuf.Length - 1);
        _binHiEnd = Mathf.Clamp(Mathf.RoundToInt(6000f / _binHz), 1, _specBuf.Length - 1);
    }

    void Update()
    {
        if (_mat == null || frames == null || frames.Count == 0) return;

        // --- 1) measure audio ---
        float targetEnv = 0f;
        if (voice && voice.isPlaying)
        {
            // envelope (RMS) from output buffer
            FillTimeBuffer(_timeBuf, voice);
            targetEnv = Mathf.Max(0f, RMS(_timeBuf) - noiseGate);

            // adaptive gain
            _agcPeak = Mathf.Lerp(_agcPeak, Mathf.Max(_agcPeak, targetEnv + 1e-5f), agcSpeed * Time.deltaTime);
            float gain = baseGain * (_agcPeak > 1e-4f ? (agcTarget / _agcPeak) : 1f);

            // smooth with attack/release
            float openLerp = attackSec <= 1e-4f ? 1f : Mathf.Clamp01(Time.deltaTime / attackSec);
            float closeLerp = releaseSec <= 1e-4f ? 1f : Mathf.Clamp01(Time.deltaTime / releaseSec);
            float amplified = targetEnv * gain;
            _env = amplified > _env ? Mathf.Lerp(_env, amplified, openLerp)
                                    : Mathf.Lerp(_env, amplified, closeLerp);

            _silentT = 0f;
        }
        else
        {
            _silentT += Time.deltaTime;
            if (closeWhenSilent && _silentT >= silentCloseDelay)
                _env = Mathf.Lerp(_env, 0f, 0.25f); // ease to closed while idle
        }

        // non-linear curve (gamma)
        float openness = Mathf.Pow(Mathf.Clamp01(_env), gamma);

        // --- 2) base index from openness ---
        int baseIdx = Mathf.RoundToInt(Mathf.Lerp(0, frames.Count - 1, openness));
        baseIdx = Mathf.Clamp(baseIdx, 0, frames.Count - 1);

        // --- 3) optional vowel heuristics (O vs EE) ---
        if (useVowelHeuristics && voice && voice.isPlaying && (frameO >= 0 || frameEE >= 0))
        {
            FillSpectrumBuffer(_specBuf, voice);
            (float lo, float mid, float hi) = BandEnergies(_specBuf, _binLoEnd, _binMidEnd, _binHiEnd);
            float total = lo + mid + hi + 1e-6f;
            float loR = lo / total, hiR = hi / total;

            // crude: more low-energy bias → rounded (O/U), more high-energy bias → spread (EE/SH)
            float biasO = (frameO >= 0) ? Mathf.Clamp01((loR - 0.25f) * 3.0f) : 0f;   // > ~25% low band
            float biasEE = (frameEE >= 0) ? Mathf.Clamp01((hiR - 0.22f) * 3.2f) : 0f;   // > ~22% high band

            if (biasO > 0f || biasEE > 0f)
            {
                // pull baseIdx toward the specialized frame
                int target = baseIdx;
                float weight = 0f;
                if (biasO >= biasEE) { target = frameO; weight = biasO * vowelBias; }
                else { target = frameEE; weight = biasEE * vowelBias; }
                baseIdx = Mathf.RoundToInt(Mathf.Lerp(baseIdx, Mathf.Clamp(target, 0, frames.Count - 1), weight));
            }
        }

        // --- 4) clay quantization + hysteresis/hold ---
        _holdTimer += Time.deltaTime;
        int desired = ApplyHysteresis(baseIdx, _lastIdx, hysteresis, frames.Count);
        if (_holdTimer < minHoldSec) desired = _lastIdx >= 0 ? _lastIdx : desired;
        int quantized = desired;

        // --- 5) visual delay (queue) ---
        int delayFrames = Mathf.RoundToInt(visualDelaySec / Mathf.Max(1e-4f, Time.deltaTime));
        if (delayFrames > 0)
        {
            _delayQ.Enqueue(quantized);
            while (_delayQ.Count > delayFrames) quantized = _delayQ.Dequeue();
        }

        // --- 6) apply swap if changed + reset hold timer ---
        if (quantized != _lastIdx)
        {
            _mat.SetTexture("_BaseColorMap", frames[quantized]);
            _lastIdx = quantized;
            _holdTimer = 0f;
        }
    }

    // Public API
    public void Speak(AudioClip clip, float volume = 1f)
    {
        if (!voice || !clip) return;
        // reset AGC for new clip
        _agcPeak = Mathf.Max(_agcPeak, 0.2f);
        _silentT = 0f;
        voice.Stop(); voice.clip = clip; voice.volume = volume; voice.Play();
    }

    // ---------- analysis helpers ----------
    static void FillTimeBuffer(float[] dst, AudioSource src)
    {
        // channel 0 is fine; data is post-mix of this source
        src.GetOutputData(dst, 0);
    }

    static float RMS(float[] x)
    {
        double s = 0; for (int i = 0; i < x.Length; i++) s += x[i] * x[i];
        return Mathf.Sqrt((float)(s / Mathf.Max(1, x.Length)));
    }

    static void FillSpectrumBuffer(float[] dst, AudioSource src)
    {
        // BlackmanHarris gives nicer peaks for speech analysis
        src.GetSpectrumData(dst, 0, FFTWindow.BlackmanHarris);
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

        // Require extra "openness" to move up a frame; easier to step down.
        if (desired > last)
        {
            float need = last + 1 + hysteresis * (count - 1);
            return desired >= Mathf.CeilToInt(need) ? desired : last;
        }
        else
        {
            // small buffer to avoid flicker when closing
            float need = last - 1 - hysteresis * (count - 1) * 0.35f;
            return desired <= Mathf.FloorToInt(need) ? desired : last;
        }
    }

    static int NextPow2(int v)
    {
        v--; v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16; v++;
        return v < 0 ? 1 : v;
    }
}
