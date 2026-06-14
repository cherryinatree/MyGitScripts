using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Cherry/World/Elevator Grid Controller")]
public class ElevatorGridController : MonoBehaviour
{
    public enum Plane2D { XY, XZ, YZ }

    [Header("Plane")]
    [SerializeField] private Plane2D plane = Plane2D.XY;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool allowDiagonalManual = false;
    [Tooltip("If false, auto-call can still move the elevator even when Power is off.")]
    [SerializeField] private bool callsRequirePower = false;

    [Header("Stops")]
    [SerializeField] private List<Transform> stops = new List<Transform>();
    [SerializeField] private float arriveTolerance = 0.05f;
    [SerializeField] private bool snapToStopOnArrive = true;

    [Header("Bounds (choose one approach)")]
    [SerializeField] private Collider boundsCollider3D;
    [SerializeField] private Collider2D boundsCollider2D; // only meaningful in XY
    [SerializeField] private Vector2 minHV = new Vector2(-5, -5);
    [SerializeField] private Vector2 maxHV = new Vector2(5, 5);

    [Header("Keep Whole Platform Inside (optional)")]
    [SerializeField] private bool keepWholePlatformInside = true;
    [SerializeField] private Collider platformCollider3D;
    [SerializeField] private Collider2D platformCollider2D; // only meaningful in XY

    [Header("Physics (optional)")]
    [SerializeField] private Rigidbody rb3D;   // for XY/XZ/YZ (recommended)
    [SerializeField] private Rigidbody2D rb2D; // only for XY

    // Manual states (from switches)
    public bool PowerOn { get; private set; }
    private int _manualH; // -1 left, 0 neutral, +1 right
    private int _manualV; // -1 down, 0 neutral, +1 up

    // Auto travel (call switches)
    public bool IsAutoMoving { get; private set; }
    private Vector2 _autoTargetHV;

    // Bounds cached in HV space
    private Vector2 _bMin, _bMax;

    private const float EPS = 0.0005f;

    private void Awake()
    {
        if (rb3D == null) rb3D = GetComponent<Rigidbody>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();

        RecomputeBounds();

        if (plane != Plane2D.XY && rb2D != null)
            Debug.LogWarning($"{name}: Plane {plane} uses 3D movement. Rigidbody2D will be ignored.");
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);

        if (maxHV.x < minHV.x) maxHV.x = minHV.x;
        if (maxHV.y < minHV.y) maxHV.y = minHV.y;

        arriveTolerance = Mathf.Max(0.001f, arriveTolerance);
    }

    public void RecomputeBounds()
    {
        if (boundsCollider2D != null && plane == Plane2D.XY)
        {
            var b = boundsCollider2D.bounds;
            GetHVMinMaxFromAABB(b.min, b.max, out _bMin, out _bMax);
        }
        else if (boundsCollider3D != null)
        {
            var b = boundsCollider3D.bounds;
            GetHVMinMaxFromAABB(b.min, b.max, out _bMin, out _bMax);
        }
        else
        {
            _bMin = minHV;
            _bMax = maxHV;
        }

        if (keepWholePlatformInside)
        {
            Vector2 ext = GetPlatformHVExtents();
            _bMin += ext;
            _bMax -= ext;

            _bMax = new Vector2(
                Mathf.Max(_bMax.x, _bMin.x),
                Mathf.Max(_bMax.y, _bMin.y)
            );
        }
    }

    // -----------------------
    // Public API (Switches)
    // -----------------------

    public void SetPower(bool on)
    {
        PowerOn = on;

        if (!PowerOn)
        {
            // Power off stops manual motion
            _manualH = 0;
            _manualV = 0;
        }
    }

    /// <summary>h: -1 left, 0 neutral, +1 right</summary>
    public void SetManualHorizontal(int h)
    {
        _manualH = Mathf.Clamp(h, -1, 1);

        // touching manual cancels auto travel
        if (_manualH != 0) CancelAuto();
    }

    /// <summary>v: -1 down, 0 neutral, +1 up</summary>
    public void SetManualVertical(int v)
    {
        _manualV = Mathf.Clamp(v, -1, 1);

        // touching manual cancels auto travel
        if (_manualV != 0) CancelAuto();
    }

    public void CallStop(int stopIndex)
    {
        if (stopIndex < 0 || stopIndex >= stops.Count || stops[stopIndex] == null) return;
        CallWorldPosition(stops[stopIndex].position);
    }

    public void CallWorldPosition(Vector3 worldPos)
    {
        if (callsRequirePower && !PowerOn) return;

        _autoTargetHV = ClampHV(WorldToHV(worldPos));
        IsAutoMoving = true;
    }

    public void CancelAuto()
    {
        IsAutoMoving = false;
    }

    // -----------------------
    // Movement
    // -----------------------

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        if (IsAutoMoving)
        {
            TickAuto(dt);
            return;
        }

        if (!PowerOn) return;

        TickManual(dt);
    }

    private void TickManual(float dt)
    {
        Vector2 input = new Vector2(_manualH, _manualV);
        if (input.sqrMagnitude < EPS) return;

        if (!allowDiagonalManual)
        {
            // If both are non-zero, prefer vertical (feels "elevator-ish")
            if (Mathf.Abs(input.y) > EPS) input.x = 0f;
        }

        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector2 cur = WorldToHV(transform.position);
        Vector2 next = ClampHV(cur + input * speed * dt);

        if ((next - cur).sqrMagnitude < EPS) return;
        MoveToHV(next);
    }

    private void TickAuto(float dt)
    {
        Vector2 cur = WorldToHV(transform.position);
        Vector2 next = Vector2.MoveTowards(cur, _autoTargetHV, speed * dt);
        next = ClampHV(next);

        MoveToHV(next);

        bool arrived = (next - _autoTargetHV).sqrMagnitude <= arriveTolerance * arriveTolerance;
        if (!arrived) return;

        IsAutoMoving = false;

        if (snapToStopOnArrive)
            MoveToHV(_autoTargetHV);
    }

    // -----------------------
    // HV (Horizontal/Vertical) mapping
    // HV is always:
    //   H = Left/Right axis
    //   V = Up/Down axis
    //
    // XY: H=X, V=Y
    // XZ: H=X, V=Z
    // YZ: H=Z, V=Y
    // -----------------------

    private Vector2 WorldToHV(Vector3 w)
    {
        return plane switch
        {
            Plane2D.XY => new Vector2(w.x, w.y),
            Plane2D.XZ => new Vector2(w.x, w.z),
            Plane2D.YZ => new Vector2(w.z, w.y),
            _ => Vector2.zero
        };
    }

    private Vector3 HVToWorld(Vector2 hv, Vector3 currentWorld)
    {
        Vector3 w = currentWorld;

        switch (plane)
        {
            case Plane2D.XY:
                w.x = hv.x;
                w.y = hv.y;
                break;
            case Plane2D.XZ:
                w.x = hv.x;
                w.z = hv.y;
                break;
            case Plane2D.YZ:
                w.z = hv.x;
                w.y = hv.y;
                break;
        }

        return w;
    }

    private void MoveToHV(Vector2 hv)
    {
        Vector3 targetWorld = HVToWorld(hv, transform.position);

        if (plane == Plane2D.XY && rb2D != null)
        {
            rb2D.MovePosition(new Vector2(targetWorld.x, targetWorld.y));
            return;
        }

        if (rb3D != null) rb3D.MovePosition(targetWorld);
        else transform.position = targetWorld;
    }

    private Vector2 ClampHV(Vector2 hv)
    {
        return new Vector2(
            Mathf.Clamp(hv.x, _bMin.x, _bMax.x),
            Mathf.Clamp(hv.y, _bMin.y, _bMax.y)
        );
    }

    private void GetHVMinMaxFromAABB(Vector3 aabbMin, Vector3 aabbMax, out Vector2 hvMin, out Vector2 hvMax)
    {
        switch (plane)
        {
            case Plane2D.XY:
                hvMin = new Vector2(aabbMin.x, aabbMin.y);
                hvMax = new Vector2(aabbMax.x, aabbMax.y);
                break;

            case Plane2D.XZ:
                hvMin = new Vector2(aabbMin.x, aabbMin.z);
                hvMax = new Vector2(aabbMax.x, aabbMax.z);
                break;

            case Plane2D.YZ:
                hvMin = new Vector2(aabbMin.z, aabbMin.y);
                hvMax = new Vector2(aabbMax.z, aabbMax.y);
                break;

            default:
                hvMin = minHV;
                hvMax = maxHV;
                break;
        }
    }

    private Vector2 GetPlatformHVExtents()
    {
        if (!keepWholePlatformInside) return Vector2.zero;

        if (platformCollider2D != null && plane == Plane2D.XY)
        {
            var e = platformCollider2D.bounds.extents;
            return new Vector2(e.x, e.y);
        }

        if (platformCollider3D != null)
        {
            var e = platformCollider3D.bounds.extents;

            return plane switch
            {
                Plane2D.XY => new Vector2(e.x, e.y),
                Plane2D.XZ => new Vector2(e.x, e.z),
                Plane2D.YZ => new Vector2(e.z, e.y),
                _ => Vector2.zero
            };
        }

        return Vector2.zero;
    }
}
