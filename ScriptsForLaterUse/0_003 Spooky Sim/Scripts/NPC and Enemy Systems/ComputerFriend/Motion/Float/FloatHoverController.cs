using System.Collections;
using UnityEngine;

/// <summary>
/// Hovering floater with optional idle drift, sphere/box bounds, and micro-impulse API.
/// Other scripts can call ThinkFlick / AngryBounce / Nudge / NudgeLocal.
/// </summary>
[DisallowMultipleComponent]
public class FloatHoverController : MonoBehaviour
{
    public enum BoundsType { Auto, None, Box, Sphere }

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float hoverFrequency = 0.8f;
    [SerializeField] private bool randomizePhase = true;

    [Header("Idle Drift (Horizontal XZ)")]
    [SerializeField] private float driftSpeed = 0.4f;
    [SerializeField] private float driftTurnRate = 30f; // deg/sec

    [Header("Bounds")]
    [Tooltip("How to interpret bounds references below.")]
    [SerializeField] private BoundsType boundsType = BoundsType.Auto;

    [Tooltip("Box bounds (used if BoundsType is Box, or Auto with this set and sphere not set).")]
    [SerializeField] private BoxCollider boxBounds;

    [Tooltip("Shrink usable box by this padding in world units.")]
    [SerializeField] private Vector3 boxPadding = new Vector3(0.1f, 0.1f, 0.1f);

    [Tooltip("Sphere bounds (used if BoundsType is Sphere, or Auto with this set).")]
    [SerializeField] private SphereCollider sphereBounds;

    [Tooltip("Shrink usable sphere radius by this many world units.")]
    [SerializeField] private float spherePadding = 0.05f;

    [Header("Rebound")]
    [SerializeField, Range(0f, 1.5f)] private float reboundDamping = 1f;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;

    // Internal state
    private Vector3 centerPos;
    private Vector3 driftDirXZ;
    private float phase;
    private Vector3 additiveOffset;

    private static readonly AnimationCurve DefaultEaseOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        centerPos = transform.position;

        // initial drift dir on XZ
        Vector2 r = Random.insideUnitCircle.normalized;
        if (float.IsNaN(r.x)) r = Vector2.right;
        driftDirXZ = new Vector3(r.x, 0f, r.y);

        phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) Idle drift in XZ
        if (driftSpeed > 0f)
        {
            float maxTurnRad = driftTurnRate * Mathf.Deg2Rad * dt;
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            if (randomDir.sqrMagnitude < 1e-4f) randomDir = Vector3.right;
            randomDir.Normalize();
            driftDirXZ = Vector3.RotateTowards(driftDirXZ, randomDir, maxTurnRad, 0f);
            driftDirXZ.y = 0f;
            driftDirXZ.Normalize();

            centerPos += driftDirXZ * driftSpeed * dt;
        }

        // 2) Hover sine
        phase += hoverFrequency * dt * Mathf.PI * 2f;
        float hoverY = Mathf.Sin(phase) * hoverAmplitude;
        Vector3 hoverOffset = new Vector3(0f, hoverY, 0f);

        // 3) Candidate final
        Vector3 finalPos = centerPos + hoverOffset + additiveOffset;

        // 4) Bounds
        switch (ResolveBoundsType())
        {
            case BoundsType.Box:
                EnforceBox(ref centerPos, hoverOffset, ref finalPos);
                break;
            case BoundsType.Sphere:
                EnforceSphere(ref centerPos, hoverOffset, ref finalPos);
                break;
            case BoundsType.None:
            default:
                break;
        }

        // 5) Apply
        transform.position = finalPos;
    }

    // ---------- Bounds Helpers ----------

    private BoundsType ResolveBoundsType()
    {
        if (boundsType != BoundsType.Auto) return boundsType;
        if (sphereBounds != null) return BoundsType.Sphere;
        if (boxBounds != null) return BoundsType.Box;
        return BoundsType.None;
    }

    private void EnforceBox(ref Vector3 center, Vector3 hoverOffset, ref Vector3 finalPos)
    {
        if (boxBounds == null) return;

        Bounds b = boxBounds.bounds;
        b.Expand(-boxPadding);

        // Keep center inside contracted box
        Vector3 clampedCenter = center;
        bool hitX = false, hitY = false, hitZ = false;

        if (clampedCenter.x < b.min.x) { clampedCenter.x = b.min.x; hitX = true; }
        else if (clampedCenter.x > b.max.x) { clampedCenter.x = b.max.x; hitX = true; }

        if (clampedCenter.y < b.min.y) { clampedCenter.y = b.min.y; hitY = true; }
        else if (clampedCenter.y > b.max.y) { clampedCenter.y = b.max.y; hitY = true; }

        if (clampedCenter.z < b.min.z) { clampedCenter.z = b.min.z; hitZ = true; }
        else if (clampedCenter.z > b.max.z) { clampedCenter.z = b.max.z; hitZ = true; }

        if (hitX || hitY || hitZ)
        {
            if (hitX) driftDirXZ.x = -driftDirXZ.x * reboundDamping;
            if (hitZ) driftDirXZ.z = -driftDirXZ.z * reboundDamping;
            center = clampedCenter;
        }

        // Clamp final as well (for big impulses)
        Vector3 clampedFinal = center + hoverOffset + additiveOffset;
        bool finX = false, finZ = false;

        if (clampedFinal.x < b.min.x) { clampedFinal.x = b.min.x; finX = true; }
        else if (clampedFinal.x > b.max.x) { clampedFinal.x = b.max.x; finX = true; }

        if (clampedFinal.y < b.min.y) clampedFinal.y = b.min.y;
        else if (clampedFinal.y > b.max.y) clampedFinal.y = b.max.y;

        if (clampedFinal.z < b.min.z) { clampedFinal.z = b.min.z; finZ = true; }
        else if (clampedFinal.z > b.max.z) { clampedFinal.z = b.max.z; finZ = true; }

        if (finX) driftDirXZ.x = -Mathf.Sign(driftDirXZ.x) * Mathf.Abs(driftDirXZ.x) * reboundDamping;
        if (finZ) driftDirXZ.z = -Mathf.Sign(driftDirXZ.z) * Mathf.Abs(driftDirXZ.z) * reboundDamping;

        finalPos = clampedFinal;
    }

    private void EnforceSphere(ref Vector3 center, Vector3 hoverOffset, ref Vector3 finalPos)
    {
        if (sphereBounds == null) return;

        // World-space center & radius considering scale
        Vector3 sCenter = sphereBounds.transform.TransformPoint(sphereBounds.center);
        float maxScale = Mathf.Max(
            Mathf.Abs(sphereBounds.transform.lossyScale.x),
            Mathf.Abs(sphereBounds.transform.lossyScale.y),
            Mathf.Abs(sphereBounds.transform.lossyScale.z)
        );
        float r = Mathf.Max(0f, sphereBounds.radius * maxScale - Mathf.Max(0f, spherePadding));

        // Keep 'center' inside sphere
        Vector3 v = center - sCenter;
        float d = v.magnitude;
        if (d > r)
        {
            Vector3 n = v / d;                  // outward normal at contact
            center = sCenter + n * r;           // clamp center to sphere

            // Reflect horizontal drift across surface normal (projected onto XZ)
            Vector3 nXZ = new Vector3(n.x, 0f, n.z);
            if (nXZ.sqrMagnitude < 1e-6f)
            {
                driftDirXZ = -driftDirXZ * reboundDamping;
            }
            else
            {
                Vector3 reflected = Vector3.Reflect(driftDirXZ, nXZ.normalized) * reboundDamping;
                driftDirXZ = new Vector3(reflected.x, 0f, reflected.z).normalized;
            }
        }

        // Clamp final (center + hover + impulses) into sphere too
        Vector3 candidate = center + hoverOffset + additiveOffset;
        Vector3 w = candidate - sCenter;
        float wd = w.magnitude;
        if (wd > r)
        {
            Vector3 nFinal = w / wd;
            candidate = sCenter + nFinal * r;

            // Reflect drift again if final was outside (e.g., strong impulse)
            Vector3 nXZ = new Vector3(nFinal.x, 0f, nFinal.z);
            if (nXZ.sqrMagnitude < 1e-6f)
            {
                driftDirXZ = -driftDirXZ * reboundDamping;
            }
            else
            {
                Vector3 reflected = Vector3.Reflect(driftDirXZ, nXZ.normalized) * reboundDamping;
                driftDirXZ = new Vector3(reflected.x, 0f, reflected.z).normalized;
            }
        }

        finalPos = candidate;
    }

    // ---------- Public API: Micro Animations ----------

    public void Nudge(Vector3 worldDirection, float distance = 0.25f, float duration = 0.25f, AnimationCurve curve = null)
    {
        if (duration <= 0f) return;
        Vector3 offset = worldDirection.sqrMagnitude > 1e-6f ? worldDirection.normalized * distance : Vector3.zero;
        StartCoroutine(ImpulseRoutine(offset, duration, curve ?? DefaultEaseOut));
    }

    public void NudgeLocal(Vector3 localDirection, float distance = 0.25f, float duration = 0.25f, AnimationCurve curve = null)
    {
        Nudge(transform.TransformDirection(localDirection), distance, duration, curve);
    }

    public void ThinkFlick(float distance = 0.22f, float duration = 0.22f, AnimationCurve curve = null)
    {
        Vector3 dir = (Vector3.up + -transform.right).normalized;
        Nudge(dir, distance, duration, curve);
    }

    public void AngryBounce(float duration = 1.0f, float amplitude = 0.35f, int bounces = 8)
    {
        if (duration <= 0f || bounces <= 0 || amplitude <= 0f) return;
        StartCoroutine(AngryBounceRoutine(duration, amplitude, bounces));
    }

    public void SetBounds(BoxCollider newBox) => boxBounds = newBox;
    public void SetBounds(SphereCollider newSphere) => sphereBounds = newSphere;

    // ---------- Coroutines ----------

    private IEnumerator ImpulseRoutine(Vector3 offset, float duration, AnimationCurve curve)
    {
        float t = 0f;
        Vector3 prev = Vector3.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            Vector3 curr = offset * curve.Evaluate(u);
            additiveOffset += (curr - prev);
            prev = curr;
            yield return null;
        }
        additiveOffset -= prev;
    }

    private IEnumerator AngryBounceRoutine(float duration, float amplitude, int bounces)
    {
        Vector3 a = Random.onUnitSphere; a.y = 0f; if (a.sqrMagnitude < 1e-3f) a = Vector3.right; a.Normalize();
        Vector3 b = Vector3.Cross(Vector3.up, a).normalized;

        float t = 0f;
        Vector3 prev = Vector3.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float envelope = 1f - u * 0.85f;
            float ph = u * bounces * Mathf.PI * 2f;

            Vector3 curr = (Mathf.Sin(ph) * a + Mathf.Sin(ph * 1.7f) * b) * (amplitude * envelope);
            additiveOffset += (curr - prev);
            prev = curr;

            // Drift bias follows agitation slightly
            Vector3 want = new Vector3(curr.x, 0f, curr.z);
            if (want.sqrMagnitude > 1e-6f)
                driftDirXZ = Vector3.Slerp(driftDirXZ, want.normalized, 0.6f * Time.deltaTime);

            yield return null;
        }
        additiveOffset -= prev;
    }

    // ---------- Gizmos ----------

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        var resolved = ResolveBoundsType();

        if (resolved == BoundsType.Box && boxBounds != null)
        {
            Bounds b = boxBounds.bounds; b.Expand(-boxPadding);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
        else if (resolved == BoundsType.Sphere && sphereBounds != null)
        {
            Vector3 c = sphereBounds.transform.TransformPoint(sphereBounds.center);
            float maxScale = Mathf.Max(
                Mathf.Abs(sphereBounds.transform.lossyScale.x),
                Mathf.Abs(sphereBounds.transform.lossyScale.y),
                Mathf.Abs(sphereBounds.transform.lossyScale.z)
            );
            float r = Mathf.Max(0f, sphereBounds.radius * maxScale - Mathf.Max(0f, spherePadding));

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            // Draw approximate sphere with circles
            DrawWireSphereApprox(c, r, 24);
        }

        // Hover amplitude guide
        Vector3 pos = Application.isPlaying ? centerPos : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos + Vector3.up * hoverAmplitude, pos - Vector3.up * hoverAmplitude);
        Gizmos.DrawWireSphere(pos + Vector3.up * hoverAmplitude, 0.03f);
        Gizmos.DrawWireSphere(pos - Vector3.up * hoverAmplitude, 0.03f);
    }

    private void DrawWireSphereApprox(Vector3 c, float r, int segments)
    {
        // XY
        Vector3 prev = c + new Vector3(r, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
            Gizmos.DrawLine(prev, p); prev = p;
        }
        // XZ
        prev = c + new Vector3(r, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, p); prev = p;
        }
        // YZ
        prev = c + new Vector3(0, r, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(0, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, p); prev = p;
        }
    }
}
