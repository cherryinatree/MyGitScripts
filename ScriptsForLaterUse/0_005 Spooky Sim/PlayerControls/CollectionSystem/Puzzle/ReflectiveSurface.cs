using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Gameplay/Reflective Surface")]
[DisallowMultipleComponent]
public class ReflectiveSurface : MonoBehaviour
{
    public enum BeamMode
    {
        Mirror,          // reflect only
        GlassMirror,     // reflect + transmit
        Prism            // split
    }

    [Header("Optics Mode")]
    public BeamMode mode = BeamMode.Mirror;

    [Header("Mirror / Glass")]
    [Range(0f, 1f)] public float reflectionWeight = 1f;
    [Range(0f, 1f)] public float transmissionWeight = 1f;

    [Header("Prism")]
    [Range(2, 16)] public int prismSplitCount = 2;
    [Range(0f, 120f)] public float prismSpreadAngle = 25f;
    public Vector3 prismLocalFanAxis = Vector3.up;

    [Header("Ray Conversion (Base)")]
    [Tooltip("If set, this mirror converts ANY incoming ray into this ray.")]
    public BeamRayDefinition forcedRay;

    [Header("Coating (Temporary Override)")]
    [Tooltip("If set, coating forces this ray while coated.")]
    public BeamRayDefinition coatingForcedRay;

    [Min(0.1f)] public float coatingDurationSeconds = 6f;

    private float _coatedUntil = -1f;

    [Header("Rotation (Tap)")]
    public bool allowTapRotate = true;
    public Vector3 localRotationAxis = Vector3.up;
    [Min(0f)] public float stepDegrees = 15f;

    public bool animateRotation = true;
    [Min(0.01f)] public float rotateAnimSeconds = 0.08f;

    private bool _rotating;

    public bool IsCoated => Time.time < _coatedUntil;


    // =========================
    // VISUALS (Mode + Coating)
    // =========================
    [Header("Visuals (Optional)")]
    [Tooltip("Renderers to tint. If empty, will auto-find Renderer on this object + children.")]
    [SerializeField] private Renderer[] targetRenderers;

    [SerializeField] private bool autoFindRenderersIfEmpty = true;

    [Tooltip("Use emission so the mirror 'glows' a bit in its current mode.")]
    [SerializeField] private bool useEmission = true;

    [SerializeField, Min(0f)] private float emissionIntensity = 2f;

    [Header("Mode Colors")]
    [SerializeField] private Color mirrorModeColor = new Color(0.8f, 0.9f, 1f, 1f);      // Mirror
    [SerializeField] private Color glassModeColor = new Color(0.6f, 0.9f, 1f, 0.6f);     // GlassMirror
    [SerializeField] private Color prismModeColor = new Color(1f, 0.75f, 0.9f, 1f);      // Prism

    [Header("Ray Tint Overrides")]
    [Tooltip("If true, while coated the mirror color becomes the coatingForcedRay.color.")]
    [SerializeField] private bool tintToCoatingRayColor = true;

    [Tooltip("If true and not coated, but forcedRay is set, mirror tints to forcedRay.color.")]
    [SerializeField] private bool tintToForcedRayColor = false;

    private MaterialPropertyBlock _mpb;

    private BeamMode _cachedMode;
    private bool _cachedCoated;
    private BeamRayDefinition _cachedForcedRay;
    private BeamRayDefinition _cachedCoatingRay;

    // Common color property names across pipelines
    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");     // HDRP/URP Lit
    private static readonly int _ColorID = Shader.PropertyToID("_Color");         // Built-in Standard
    private static readonly int _EmissionColorID = Shader.PropertyToID("_EmissionColor"); // Built-in/URP
    private static readonly int _EmissiveColorID = Shader.PropertyToID("_EmissiveColor"); // HDRP

    private void EnsureVisualSetup()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if ((targetRenderers == null || targetRenderers.Length == 0) && autoFindRenderersIfEmpty)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }
    }

    private void Update()
    {
        // If you already have an Update() in this script, merge this check into it.
        bool coated = IsCoated;

        if (_cachedMode != mode ||
            _cachedCoated != coated ||
            _cachedForcedRay != forcedRay ||
            _cachedCoatingRay != coatingForcedRay)
        {
            _cachedMode = mode;
            _cachedCoated = coated;
            _cachedForcedRay = forcedRay;
            _cachedCoatingRay = coatingForcedRay;

            ApplyVisuals();
        }
    }

    private void Awake()
    {
        // If you already have Awake() in this script, merge these two lines into it.
        EnsureVisualSetup();
        ApplyVisuals();
    }

    private void OnValidate()
    {
        // Runs in editor when you change fields.
        EnsureVisualSetup();
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        EnsureVisualSetup();

        // Pick base color by mode
        Color c = mode switch
        {
            BeamMode.GlassMirror => glassModeColor,
            BeamMode.Prism => prismModeColor,
            _ => mirrorModeColor
        };

        // Optional ray-driven tinting
        if (IsCoated && tintToCoatingRayColor && coatingForcedRay != null)
            c = coatingForcedRay.color;
        else if (!IsCoated && tintToForcedRayColor && forcedRay != null)
            c = forcedRay.color;

        // Apply to all target renderers
        if (targetRenderers == null) return;

        Color emissive = useEmission ? (c * Mathf.Max(0f, emissionIntensity)) : Color.black;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);

            // Set both common base color properties
            _mpb.SetColor(_BaseColorID, c);
            _mpb.SetColor(_ColorID, c);

            // Set emission for URP/Built-in and HDRP
            if (useEmission)
            {
                _mpb.SetColor(_EmissionColorID, emissive);
                _mpb.SetColor(_EmissiveColorID, emissive);
            }
            else
            {
                _mpb.SetColor(_EmissionColorID, Color.black);
                _mpb.SetColor(_EmissiveColorID, Color.black);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    public void ApplyCoating(BeamRayDefinition overrideRay = null, float overrideSeconds = -1f)
    {
        ApplyVisuals();
        if (overrideRay != null) coatingForcedRay = overrideRay;
        float dur = overrideSeconds > 0f ? overrideSeconds : coatingDurationSeconds;
        _coatedUntil = Time.time + Mathf.Max(0.05f, dur);
    }

    public void RotateStep(int direction = 1)
    {
        if (!allowTapRotate) return;
        if (_rotating) return;

        float degrees = stepDegrees * Mathf.Sign(direction == 0 ? 1 : direction);

        if (!animateRotation || rotateAnimSeconds <= 0f)
        {
            transform.Rotate(localRotationAxis.normalized, degrees, Space.Self);
            return;
        }

        StartCoroutine(RotateRoutine(degrees));
    }

    private IEnumerator RotateRoutine(float degrees)
    {
        _rotating = true;

        Quaternion start = transform.localRotation;
        Quaternion end = start * Quaternion.AngleAxis(degrees, localRotationAxis.normalized);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateAnimSeconds;
            transform.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localRotation = end;
        _rotating = false;
    }

    // Backward-compatible signature (no ray typing)
    public bool GetOutgoingBeams(
        Vector3 incomingDir,
        Vector3 hitNormal,
        List<Vector3> outDirs,
        List<float> outWeights)
    {
        return GetOutgoingBeams(null, incomingDir, hitNormal, outDirs, outWeights, null);
    }

    // NEW: typed-ray overload
    public bool GetOutgoingBeams(
        BeamRayDefinition incomingRay,
        Vector3 incomingDir,
        Vector3 hitNormal,
        List<Vector3> outDirs,
        List<float> outWeights,
        List<BeamRayDefinition> outRays)
    {
        outDirs.Clear();
        outWeights.Clear();
        outRays?.Clear();

        if (incomingRay != null && !incomingRay.canBeReflected && mode == BeamMode.Mirror)
            return false;

        BeamRayDefinition outRay = ResolveOutputRay(incomingRay);

        incomingDir = incomingDir.normalized;
        hitNormal = hitNormal.normalized;

        if (mode == BeamMode.Mirror)
        {
            outDirs.Add(Vector3.Reflect(incomingDir, hitNormal).normalized);
            outWeights.Add(Mathf.Clamp01(reflectionWeight <= 0f ? 1f : reflectionWeight));
            outRays?.Add(outRay);
            return true;
        }

        if (mode == BeamMode.GlassMirror)
        {
            if (reflectionWeight > 0f)
            {
                outDirs.Add(Vector3.Reflect(incomingDir, hitNormal).normalized);
                outWeights.Add(Mathf.Clamp01(reflectionWeight));
                outRays?.Add(outRay);
            }

            if (transmissionWeight > 0f)
            {
                outDirs.Add(incomingDir); // straight-through
                outWeights.Add(Mathf.Clamp01(transmissionWeight));
                outRays?.Add(outRay);
            }

            return outDirs.Count > 0;
        }

        // Prism split
        int count = Mathf.Clamp(prismSplitCount, 2, 16);
        float total = Mathf.Clamp(prismSpreadAngle, 0f, 120f);
        float half = total * 0.5f;

        Vector3 axisWorld = transform.TransformDirection(prismLocalFanAxis.normalized);
        if (axisWorld.sqrMagnitude < 0.0001f) axisWorld = Vector3.up;

        float w = 1f / count;
        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (i / (float)(count - 1));
            float ang = Mathf.Lerp(-half, half, t);
            Vector3 d = (Quaternion.AngleAxis(ang, axisWorld) * incomingDir).normalized;

            outDirs.Add(d);
            outWeights.Add(w);
            outRays?.Add(outRay);
        }

        return true;
    }

    private BeamRayDefinition ResolveOutputRay(BeamRayDefinition incomingRay)
    {
        if (IsCoated && coatingForcedRay != null) return coatingForcedRay;
        if (forcedRay != null) return forcedRay;
        return incomingRay;
    }
}