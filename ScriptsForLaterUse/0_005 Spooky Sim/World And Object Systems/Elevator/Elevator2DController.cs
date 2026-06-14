using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Cherry/World/Elevator 2D Controller")]
public class Elevator2DController : MonoBehaviour
{
    public enum Plane2D { XY, XZ, YZ }
    public enum ControlMode { Step, Hold }
    public enum Dir { Up, Down, Left, Right }

    [Header("Mode")]
    [SerializeField] private Plane2D plane = Plane2D.XY;
    [SerializeField] private ControlMode controlMode = ControlMode.Step;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [Tooltip("In Step mode, how far one switch flip moves.")]
    [SerializeField] private float stepDistance = 3f;
    [Tooltip("If false, holding Up+Right won't move diagonally (a simple priority is used).")]
    [SerializeField] private bool allowDiagonal = false;
    [Tooltip("If true, Step mode can queue ONE extra step while moving.")]
    [SerializeField] private bool queueStepWhileMoving = true;

    [Header("Bounds (choose one approach)")]
    [Tooltip("If set, bounds come from this collider's world AABB projected onto your plane.")]
    [SerializeField] private Collider boundsCollider3D;
    [Tooltip("If set, bounds come from this collider's world AABB projected onto your plane (2D). Only meaningful for XY.")]
    [SerializeField] private Collider2D boundsCollider2D;

    [Tooltip("If no bounds collider is set, use these min/max plane coords.\nFor YZ: X=Y, Y=Z.")]
    [SerializeField] private Vector2 minPlane = new Vector2(-5, -5);
    [Tooltip("If no bounds collider is set, use these min/max plane coords.\nFor YZ: X=Y, Y=Z.")]
    [SerializeField] private Vector2 maxPlane = new Vector2(5, 5);

    [Header("Keep Whole Platform Inside (optional)")]
    [Tooltip("If true, the bounds shrink by half the platform size so the platform doesn't poke out.")]
    [SerializeField] private bool keepWholePlatformInside = true;
    [SerializeField] private Collider platformCollider3D;
    [Tooltip("Only meaningful for XY plane.")]
    [SerializeField] private Collider2D platformCollider2D;

    [Header("Physics (optional)")]
    [Tooltip("If assigned, movement uses Rigidbody.MovePosition (recommended for moving platforms).")]
    [SerializeField] private Rigidbody rb3D;
    [Tooltip("If assigned and Plane is XY, movement uses Rigidbody2D.MovePosition.")]
    [SerializeField] private Rigidbody2D rb2D;

    // --- runtime ---
    private Vector2 _boundsMin;
    private Vector2 _boundsMax;

    // Hold mode input toggles
    private bool _up, _down, _left, _right;

    // Step mode
    private bool _isStepping;
    private Vector2 _stepTarget;
    private Vector2 _queuedStepDir; // at most one queued step

    private const float EPS = 0.0005f;

    private void Awake()
    {
        RecomputeBounds();

        if (rb3D == null) rb3D = GetComponent<Rigidbody>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();

        // Safety: 2D rigidbodies only make sense for XY
        if (plane != Plane2D.XY && rb2D != null)
        {
            Debug.LogWarning($"{name}: Plane {plane} uses 3D movement. Rigidbody2D will be ignored.");
        }
    }

    private void OnValidate()
    {
        if (maxPlane.x < minPlane.x) maxPlane.x = minPlane.x;
        if (maxPlane.y < minPlane.y) maxPlane.y = minPlane.y;

        speed = Mathf.Max(0f, speed);
        stepDistance = Mathf.Max(0f, stepDistance);
    }

    public void RecomputeBounds()
    {
        // Bounds from collider if provided, otherwise inspector min/max
        if (boundsCollider2D != null && plane == Plane2D.XY)
        {
            var b = boundsCollider2D.bounds;
            GetPlaneMinMaxFromAABB(b.min, b.max, out _boundsMin, out _boundsMax);
        }
        else if (boundsCollider3D != null)
        {
            var b = boundsCollider3D.bounds;
            GetPlaneMinMaxFromAABB(b.min, b.max, out _boundsMin, out _boundsMax);
        }
        else
        {
            _boundsMin = minPlane;
            _boundsMax = maxPlane;
        }

        if (keepWholePlatformInside)
        {
            Vector2 extents = GetPlatformPlaneExtents();
            _boundsMin += extents;
            _boundsMax -= extents;

            _boundsMax = new Vector2(
                Mathf.Max(_boundsMax.x, _boundsMin.x),
                Mathf.Max(_boundsMax.y, _boundsMin.y)
            );
        }
    }

    // -------------------------
    // Public API for switches
    // -------------------------

    public void Pulse(Dir dir)
    {
        Vector2 d = DirToVec(dir);

        if (controlMode != ControlMode.Step)
        {
            StartStep(d);
            return;
        }

        if (_isStepping)
        {
            if (queueStepWhileMoving) _queuedStepDir = d;
            return;
        }

        StartStep(d);
    }

    public void SetHeld(Dir dir, bool isOn)
    {
        switch (dir)
        {
            case Dir.Up: _up = isOn; break;
            case Dir.Down: _down = isOn; break;
            case Dir.Left: _left = isOn; break;
            case Dir.Right: _right = isOn; break;
        }

        if (controlMode == ControlMode.Hold)
        {
            _isStepping = false;
            _queuedStepDir = Vector2.zero;
        }
    }

    public void StopAll()
    {
        _up = _down = _left = _right = false;
        _isStepping = false;
        _queuedStepDir = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (controlMode == ControlMode.Hold) TickHold(Time.fixedDeltaTime);
        else TickStep(Time.fixedDeltaTime);
    }

    private void TickHold(float dt)
    {
        Vector2 input = GetHoldInputDir();
        if (input.sqrMagnitude < EPS) return;

        Vector2 current = GetPlanePos(transform.position);
        Vector2 desired = current + input * speed * dt;
        desired = ClampToBounds(desired);

        if ((desired - current).sqrMagnitude < EPS) return;

        MoveToPlane(desired);
    }

    private void TickStep(float dt)
    {
        if (!_isStepping) return;

        Vector2 current = GetPlanePos(transform.position);
        Vector2 next = Vector2.MoveTowards(current, _stepTarget, speed * dt);
        next = ClampToBounds(next);

        MoveToPlane(next);

        bool reached = (next - _stepTarget).sqrMagnitude < (0.01f * 0.01f);
        if (!reached) return;

        _isStepping = false;

        if (_queuedStepDir.sqrMagnitude > EPS)
        {
            Vector2 d = _queuedStepDir;
            _queuedStepDir = Vector2.zero;
            StartStep(d);
        }
    }

    private void StartStep(Vector2 dir)
    {
        Vector2 current = GetPlanePos(transform.position);
        Vector2 target = current + dir.normalized * stepDistance;
        target = ClampToBounds(target);

        if ((target - current).sqrMagnitude < EPS)
        {
            _isStepping = false;
            return;
        }

        _stepTarget = target;
        _isStepping = true;
    }

    // -------------------------
    // Helpers
    // -------------------------

    private Vector2 GetHoldInputDir()
    {
        float x = 0f, y = 0f;
        if (_left) x -= 1f;
        if (_right) x += 1f;
        if (_down) y -= 1f;
        if (_up) y += 1f;

        Vector2 v = new Vector2(x, y);

        if (!allowDiagonal)
        {
            if (Mathf.Abs(v.y) > EPS) v.x = 0f;
        }

        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    private Vector2 DirToVec(Dir d) => d switch
    {
        Dir.Up => Vector2.up,
        Dir.Down => Vector2.down,
        Dir.Left => Vector2.left,
        Dir.Right => Vector2.right,
        _ => Vector2.zero
    };

    private Vector2 ClampToBounds(Vector2 planePos) => new Vector2(
        Mathf.Clamp(planePos.x, _boundsMin.x, _boundsMax.x),
        Mathf.Clamp(planePos.y, _boundsMin.y, _boundsMax.y)
    );

    // Plane coordinates:
    // XY: (x,y)
    // XZ: (x,z)
    // YZ: (y,z)  <-- requested
    private Vector2 GetPlanePos(Vector3 world) => plane switch
    {
        Plane2D.XY => new Vector2(world.x, world.y),
        Plane2D.XZ => new Vector2(world.x, world.z),
        Plane2D.YZ => new Vector2(world.y, world.z),
        _ => Vector2.zero
    };

    private void MoveToPlane(Vector2 planePos)
    {
        Vector3 world = transform.position;

        switch (plane)
        {
            case Plane2D.XY:
                world.x = planePos.x;
                world.y = planePos.y;

                if (rb2D != null)
                {
                    rb2D.MovePosition(new Vector2(world.x, world.y));
                    return;
                }
                break;

            case Plane2D.XZ:
                world.x = planePos.x;
                world.z = planePos.y;
                break;

            case Plane2D.YZ:
                world.y = planePos.x;
                world.z = planePos.y;
                break;
        }

        if (rb3D != null) rb3D.MovePosition(world);
        else transform.position = world;
    }

    private void GetPlaneMinMaxFromAABB(Vector3 aabbMin, Vector3 aabbMax, out Vector2 pMin, out Vector2 pMax)
    {
        switch (plane)
        {
            case Plane2D.XY:
                pMin = new Vector2(aabbMin.x, aabbMin.y);
                pMax = new Vector2(aabbMax.x, aabbMax.y);
                break;

            case Plane2D.XZ:
                pMin = new Vector2(aabbMin.x, aabbMin.z);
                pMax = new Vector2(aabbMax.x, aabbMax.z);
                break;

            case Plane2D.YZ:
                pMin = new Vector2(aabbMin.y, aabbMin.z);
                pMax = new Vector2(aabbMax.y, aabbMax.z);
                break;

            default:
                pMin = minPlane;
                pMax = maxPlane;
                break;
        }
    }

    private Vector2 GetPlatformPlaneExtents()
    {
        if (!keepWholePlatformInside) return Vector2.zero;

        // 2D collider extents only apply to XY
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
                Plane2D.YZ => new Vector2(e.y, e.z),
                _ => Vector2.zero
            };
        }

        return Vector2.zero;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) RecomputeBounds();

        Vector2 bmin = _boundsMin;
        Vector2 bmax = _boundsMax;

        Vector3 p1, p2, p3, p4;

        // Draw rectangle in the selected plane, anchored at current position for the "fixed" axis
        Vector3 a = transform.position;

        switch (plane)
        {
            case Plane2D.XY:
                {
                    float z = a.z;
                    p1 = new Vector3(bmin.x, bmin.y, z);
                    p2 = new Vector3(bmax.x, bmin.y, z);
                    p3 = new Vector3(bmax.x, bmax.y, z);
                    p4 = new Vector3(bmin.x, bmax.y, z);
                    break;
                }
            case Plane2D.XZ:
                {
                    float y = a.y;
                    p1 = new Vector3(bmin.x, y, bmin.y);
                    p2 = new Vector3(bmax.x, y, bmin.y);
                    p3 = new Vector3(bmax.x, y, bmax.y);
                    p4 = new Vector3(bmin.x, y, bmax.y);
                    break;
                }
            case Plane2D.YZ:
                {
                    float x = a.x;
                    p1 = new Vector3(x, bmin.x, bmin.y); // (x, yMin, zMin)
                    p2 = new Vector3(x, bmax.x, bmin.y); // (x, yMax, zMin)
                    p3 = new Vector3(x, bmax.x, bmax.y); // (x, yMax, zMax)
                    p4 = new Vector3(x, bmin.x, bmax.y); // (x, yMin, zMax)
                    break;
                }
            default:
                return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
#endif
}
