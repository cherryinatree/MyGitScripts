using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class EyeSet
{
    public Texture2D neutral;
    public Texture2D happy;
    public Texture2D sad;
    public Texture2D angry;
    public Texture2D surprised;
    public Texture2D thinking;
    public Texture2D anxious;
    public Texture2D blink; // closed eyelids
}

public class AIFaceDecalController : MonoBehaviour
{
    public enum AIMood { Neutral, Happy, Sad, Angry, Surprised, Thinking, Anxious }
    [Header("Projectors (assign in Inspector)")]
    public DecalProjector mouthProj;
    public DecalProjector leftEyeProj;
    public DecalProjector rightEyeProj;
    [Tooltip("Optional – if assigned, pupils will saccade/track target.")]
    public DecalProjector leftPupilProj;
    public DecalProjector rightPupilProj;

    [Header("Materials (instances created at runtime)")]
    public Material mouthDecalMatTemplate;
    public Material eyeDecalMatTemplate;
    public Material pupilDecalMatTemplate;

    [Header("Assets")]
    [Tooltip("Ordered closed → wide. 5–8 frames recommended.")]
    public List<Texture2D> mouthFrames = new List<Texture2D>();
    public EyeSet eyes;
    public Texture2D pupilTex;

    [Header("Audio / Lip Sync")]
    public AudioSource voice;          // attach your audio source here
    [Range(0.1f, 10f)] public float mouthGain = 2.2f;
    [Range(0f, 0.2f)] public float mouthFloor = 0.035f;
    [Range(0f, 0.5f)] public float mouthSmoothing = 0.12f;

    [Header("Eyes / Behavior")]
    public AIMood currentMood = AIMood.Neutral;
    public Transform lookTarget;       // optional
    [Range(0f, 1f)] public float lookStrength = 0.7f;
    public float pupilRange = 0.015f;  // meters around each eye projector
    public Vector2 pupilDilation = new Vector2(0.9f, 1.15f); // min..max scale by mood

    [Header("Blink & Saccade")]
    public Vector2 blinkInterval = new Vector2(2.5f, 6.0f);
    public float blinkDuration = 0.085f;
    public Vector2 saccadeInterval = new Vector2(0.5f, 2.0f);
    public float saccadeSpeed = 10f;

    [Header("Claymation Feel")]
    public bool clayJitter = true;
    public float jitterPos = 0.0008f;
    public float jitterRot = 1.2f;

    // internals
    Material _mouthMat, _leftEyeMat, _rightEyeMat, _leftPupilMat, _rightPupilMat;
    float _mouthEnvSmoothed;
    float _nextBlinkAt;
    bool _isBlinking;
    Vector3 _lpHome, _rpHome;
    Vector3 _lpTarget, _rpTarget;

    void Awake()
    {
        // Create unique mat instances so swaps don't affect shared assets
        if (mouthProj && mouthDecalMatTemplate)
            _mouthMat = mouthProj.material = new Material(mouthDecalMatTemplate);
        if (leftEyeProj && eyeDecalMatTemplate)
            _leftEyeMat = leftEyeProj.material = new Material(eyeDecalMatTemplate);
        if (rightEyeProj && eyeDecalMatTemplate)
            _rightEyeMat = rightEyeProj.material = new Material(eyeDecalMatTemplate);

        if (leftPupilProj && pupilDecalMatTemplate)
            _leftPupilMat = leftPupilProj.material = new Material(pupilDecalMatTemplate);
        if (rightPupilProj && pupilDecalMatTemplate)
            _rightPupilMat = rightPupilProj.material = new Material(pupilDecalMatTemplate);

        if (_leftPupilMat && pupilTex) _leftPupilMat.SetTexture("_BaseColorMap", pupilTex);
        if (_rightPupilMat && pupilTex) _rightPupilMat.SetTexture("_BaseColorMap", pupilTex);

        // Cache pupil home offsets (local)
        if (leftPupilProj) _lpHome = leftPupilProj.transform.localPosition;
        if (rightPupilProj) _rpHome = rightPupilProj.transform.localPosition;

        ApplyMood(currentMood, immediate: true);
        ScheduleNextBlink();
        RandomizeSaccadeTargets(instant: true);
    }

    void Update()
    {
        UpdateMouthFromAudio();
        UpdateBlink(Time.deltaTime);
        UpdatePupils(Time.deltaTime);
        if (clayJitter) ApplyClayJitter();
    }

    // ----------------- Public API -----------------
    public void ApplyMood(AIMood mood, bool immediate = false)
    {
        currentMood = mood;

        var tex = EyesForMood(mood);
        if (_leftEyeMat) _leftEyeMat.SetTexture("_BaseColorMap", tex);
        if (_rightEyeMat) _rightEyeMat.SetTexture("_BaseColorMap", tex);

        float dil = 1f;
        switch (mood)
        {
            case AIMood.Happy:
            case AIMood.Surprised: dil = pupilDilation.y; break;
            case AIMood.Angry:
            case AIMood.Anxious: dil = pupilDilation.x; break;
            default: dil = 1f; break;
        }
        if (leftPupilProj) leftPupilProj.transform.localScale = Vector3.one * dil;
        if (rightPupilProj) rightPupilProj.transform.localScale = Vector3.one * dil;

        if (immediate) _isBlinking = false;
    }

    /// <summary>Play an audio clip and let the mouth lip-sync by amplitude.</summary>
    public void Speak(AudioClip clip, float volume = 1f)
    {
        if (!voice || clip == null) return;
        voice.Stop();
        voice.clip = clip;
        voice.volume = volume;
        voice.Play();
    }

    // ----------------- Mouth (amplitude drive) -----------------
    void UpdateMouthFromAudio()
    {
        if (_mouthMat == null || mouthFrames.Count == 0) return;

        int idx = 0;

        if (voice && voice.isPlaying)
        {
            float env = GetEnvelope(256);
            env = Mathf.Max(0f, env - mouthFloor) * mouthGain;

            // Smooth
            float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.01f, mouthSmoothing));
            _mouthEnvSmoothed = Mathf.Lerp(_mouthEnvSmoothed, env, t);

            idx = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0, mouthFrames.Count - 1,
                    Mathf.Clamp01(_mouthEnvSmoothed))), 0, mouthFrames.Count - 1);
        }

        // Swap only when changed (reduces material property churn)
        if (_mouthMat.GetTexture("_BaseColorMap") != mouthFrames[idx])
            _mouthMat.SetTexture("_BaseColorMap", mouthFrames[idx]);
    }

    float GetEnvelope(int samples)
    {
        float[] buf = new float[samples];
        voice.GetOutputData(buf, 0);
        float sum = 0f;
        for (int i = 0; i < buf.Length; i++) sum += Mathf.Abs(buf[i]);
        return sum / buf.Length;
    }

    // ----------------- Eyes: blinks -----------------
    void ScheduleNextBlink() => _nextBlinkAt = Time.time + Random.Range(blinkInterval.x, blinkInterval.y);

    void UpdateBlink(float dt)
    {
        if (_isBlinking || eyes == null || eyes.blink == null) return;
        if (Time.time >= _nextBlinkAt) StartCoroutine(BlinkCo());
    }

    IEnumerator BlinkCo()
    {
        _isBlinking = true;

        var leftBefore = _leftEyeMat ? _leftEyeMat.GetTexture("_BaseColorMap") : null;
        var rightBefore = _rightEyeMat ? _rightEyeMat.GetTexture("_BaseColorMap") : null;

        if (_leftEyeMat) _leftEyeMat.SetTexture("_BaseColorMap", eyes.blink);
        if (_rightEyeMat) _rightEyeMat.SetTexture("_BaseColorMap", eyes.blink);

        yield return new WaitForSeconds(blinkDuration);

        var tex = EyesForMood(currentMood);
        if (_leftEyeMat) _leftEyeMat.SetTexture("_BaseColorMap", tex);
        if (_rightEyeMat) _rightEyeMat.SetTexture("_BaseColorMap", tex);

        _isBlinking = false;
        ScheduleNextBlink();
    }

    // ----------------- Eyes: pupils / saccades / look -----------------
    void RandomizeSaccadeTargets(bool instant = false)
    {
        _lpTarget = Random.insideUnitCircle * pupilRange;
        _rpTarget = Random.insideUnitCircle * pupilRange;
        if (instant)
        {
            if (leftPupilProj) leftPupilProj.transform.localPosition = _lpHome + _lpTarget;
            if (rightPupilProj) rightPupilProj.transform.localPosition = _rpHome + _rpTarget;
        }
        StartCoroutine(SaccadeTimer());
    }

    IEnumerator SaccadeTimer()
    {
        yield return new WaitForSeconds(Random.Range(saccadeInterval.x, saccadeInterval.y));
        RandomizeSaccadeTargets();
    }

    void UpdatePupils(float dt)
    {
        if (!leftPupilProj && !rightPupilProj) return;

        Vector2 gaze = Vector2.zero;
        if (lookTarget)
        {
            // Compute local right/up directions from the face rig
            var fwd = transform.forward;
            var to = (lookTarget.position - transform.position).normalized;
            float x = Vector3.Dot(to, transform.right);
            float y = Vector3.Dot(to, transform.up);
            gaze = new Vector2(x, y) * pupilRange * lookStrength;
        }

        Vector3 lpGoal = _lpHome + (Vector3)Vector2.Lerp(_lpTarget, gaze, lookStrength);
        Vector3 rpGoal = _rpHome + (Vector3)Vector2.Lerp(_rpTarget, gaze, lookStrength);

        if (leftPupilProj)
            leftPupilProj.transform.localPosition =
                Vector3.Lerp(leftPupilProj.transform.localPosition, lpGoal, saccadeSpeed * dt);

        if (rightPupilProj)
            rightPupilProj.transform.localPosition =
                Vector3.Lerp(rightPupilProj.transform.localPosition, rpGoal, saccadeSpeed * dt);
    }

    // ----------------- Helpers -----------------
    Texture2D EyesForMood(AIMood m)
    {
        if (eyes == null) return null;
        return m switch
        {
            AIMood.Happy => eyes.happy ? eyes.happy : eyes.neutral,
            AIMood.Sad => eyes.sad ? eyes.sad : eyes.neutral,
            AIMood.Angry => eyes.angry ? eyes.angry : eyes.neutral,
            AIMood.Surprised => eyes.surprised ? eyes.surprised : eyes.neutral,
            AIMood.Thinking => eyes.thinking ? eyes.thinking : eyes.neutral,
            AIMood.Anxious => eyes.anxious ? eyes.anxious : eyes.neutral,
            _ => eyes.neutral
        };
    }

    void ApplyClayJitter()
    {
        // Tiny random offsets to projectors for stop-motion charm
        float jx = (Random.value - 0.5f) * 2f * jitterPos;
        float jy = (Random.value - 0.5f) * 2f * jitterPos;
        float jz = (Random.value - 0.5f) * 2f * jitterPos;

        Quaternion jRot = Quaternion.Euler(
            (Random.value - 0.5f) * 2f * jitterRot,
            (Random.value - 0.5f) * 2f * jitterRot,
            (Random.value - 0.5f) * 2f * jitterRot
        );

        if (mouthProj)
        {
            mouthProj.transform.localPosition += new Vector3(jx, jy, 0f);
            mouthProj.transform.localRotation = jRot * mouthProj.transform.localRotation;
        }
    }
}
