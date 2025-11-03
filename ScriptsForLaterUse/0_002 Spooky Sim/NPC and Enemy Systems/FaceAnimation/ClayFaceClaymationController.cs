// ClayFaceClaymationController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ClayMood { Neutral, Happy, Sad, Angry, Surprised, Thinking, Anxious }

public class ClayFaceClaymationController : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource voice;                  // Audio that drives lip-sync
    public ClaySwapGroup mouthGroup;           // Mouth frames (closed ? wide)
    [System.Serializable] public class MoodBinding { public ClayMood mood; public GameObject groupRoot; }
    public List<MoodBinding> eyeGroups = new(); // One root per mood (contains 1+ frames)
    public GameObject blinkObject;             // Closed-eyelid object

    [Header("Mouth (amplitude ? frame)")]
    [Range(4f, 24f)] public float mouthFPS = 12f;
    [Range(0.1f, 10f)] public float mouthGain = 2.2f;
    [Range(0f, 0.2f)] public float mouthFloor = 0.035f;
    [Range(0f, 0.5f)] public float mouthSmoothing = 0.12f;
    public bool closeWhenSilent = true;
    public float silentRelease = 0.15f; // time to settle to closed after clip ends

    [Header("Blink")]
    public Vector2 blinkEvery = new Vector2(2.5f, 6f);
    public float blinkDuration = 0.085f;

    [Header("Clay jitter (applied on each frame change)")]
    public Transform mouthJitterHandle;  // if null, uses mouthGroup.transform
    public float jitterPos = 0.0008f;    // meters
    public float jitterRot = 1.2f;       // degrees

    // internals
    float _tickTimer, _hold; float _env; float _silentT;
    bool _blinking; ClayMood _currentMood = ClayMood.Neutral;
    Dictionary<ClayMood, GameObject> _eyesByMood = new();

    Vector3 _mouthBasePos; Quaternion _mouthBaseRot;

    void Awake()
    {
        _hold = 1f / Mathf.Max(1f, mouthFPS);

        foreach (var b in eyeGroups)
            if (b != null && b.groupRoot) _eyesByMood[b.mood] = b.groupRoot;

        if (mouthJitterHandle == null && mouthGroup) mouthJitterHandle = mouthGroup.transform;
        if (mouthJitterHandle) { _mouthBasePos = mouthJitterHandle.localPosition; _mouthBaseRot = mouthJitterHandle.localRotation; }

        ApplyMood(_currentMood, true);
        ScheduleNextBlink();
    }

    void Update()
    {
        UpdateMouth(Time.deltaTime);
        UpdateBlink();
    }

    // ---------------- Mouth ----------------
    void UpdateMouth(float dt)
    {
        if (!mouthGroup || mouthGroup.Count == 0) return;

        // Envelope
        float target = 0f;
        if (voice && voice.isPlaying)
        {
            float[] buf = new float[256];
            voice.GetOutputData(buf, 0);
            float sum = 0f; for (int i = 0; i < buf.Length; i++) sum += Mathf.Abs(buf[i]);
            target = Mathf.Max(0f, (sum / buf.Length) - mouthFloor) * mouthGain;
            _silentT = 0f;
        }
        else
        {
            _silentT += dt;
            if (closeWhenSilent && _silentT >= silentRelease) target = 0f;
        }

        // Smooth
        float lerp = Mathf.Clamp01(dt / Mathf.Max(0.01f, mouthSmoothing));
        _env = Mathf.Lerp(_env, target, lerp);

        // Quantize to clay FPS
        _tickTimer += dt;
        if (_tickTimer >= _hold)
        {
            _tickTimer = 0f;
            int frames = Mathf.Max(1, mouthGroup.Count);
            int idx = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0, frames - 1, Mathf.Clamp01(_env))), 0, frames - 1);

            if (idx != mouthGroup.CurrentIndex)
            {
                mouthGroup.SetIndex(idx);
                ApplyMouthJitter();
            }
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
                Random.Range(-jitterRot, jitterRot) * 0.3f);
    }

    // ---------------- Eyes / Mood ----------------
    public void ApplyMood(ClayMood mood, bool immediate = false)
    {
        _currentMood = mood;
        // enable selected eye group, disable others
        foreach (var kv in _eyesByMood)
            if (kv.Value) kv.Value.SetActive(kv.Key == mood);

        if (immediate) _blinking = false;
    }

    // ---------------- Blink ----------------
    float _nextBlinkAt;
    void ScheduleNextBlink() => _nextBlinkAt = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);

    void UpdateBlink()
    {
        if (!blinkObject || _blinking) return;
        if (Time.time >= _nextBlinkAt) StartCoroutine(BlinkCo());
    }

    IEnumerator BlinkCo()
    {
        _blinking = true;

        // Hide current mood eyes, show blink
        var activeMoodGO = _eyesByMood.TryGetValue(_currentMood, out var go) ? go : null;
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
        voice.Stop(); voice.clip = clip; voice.volume = volume; voice.Play();
    }
}
