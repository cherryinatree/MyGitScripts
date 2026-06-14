using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyMovingPlatform : MonoBehaviour
{
    [Header("Start State")]
    [SerializeField] private bool startRunning = true;

    [Tooltip("If true, when turned back on it continues toward the current target. If false, it restarts from waypoint 0.")]
    [SerializeField] private bool resumeFromCurrentTarget = true;

    [Header("Path")]
    [Tooltip("Platform will move to these points in order.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("If true, waypoint positions are treated as local offsets from the platform's start pose.")]
    [SerializeField] private bool waypointsAreLocalOffsets = false;

    [Header("Motion")]
    [SerializeField] private float speed = 2.0f;          // units/sec
    [SerializeField] private bool pingPong = true;        // go back and forth
    [SerializeField] private float waitAtWaypoint = 0.0f; // seconds

    [Header("Rigidbody Mode")]
    [Tooltip("Kinematic = platform follows path exactly. Dynamic = uses velocity (can be pushed/affected).")]
    [SerializeField] private bool useKinematic = true;

    public Vector3 CurrentVelocity { get; private set; }
    public bool IsRunning => _running;

    private Rigidbody _rb;
    private bool _running;

    private int _index;
    private int _dir = 1;
    private float _waitTimer;

    private Vector3 _startPos;
    private Vector3[] _targets;
    private Vector3 _lastPos;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Recommended platform settings
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.isKinematic = useKinematic;

        _startPos = transform.position;
        BuildTargets();

        _index = 0;
        _waitTimer = 0f;

        _lastPos = _rb.position;

        // Apply start state
        _running = startRunning;
        if (!_running) StopMotionImmediate();
    }

    private void OnValidate()
    {
        if (speed < 0f) speed = 0f;
        if (waitAtWaypoint < 0f) waitAtWaypoint = 0f;
    }

    private void BuildTargets()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            _targets = null;
            return;
        }

        _targets = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            _targets[i] = waypointsAreLocalOffsets
                ? _startPos + waypoints[i].localPosition
                : waypoints[i].position;
        }
    }

    private void FixedUpdate()
    {
        if (_targets == null || _targets.Length == 0) return;

        if (!_running)
        {
            // Keep velocity reporting sane while stopped
            CurrentVelocity = Vector3.zero;
            _lastPos = _rb.position;
            return;
        }

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            UpdateVelocity();
            return;
        }

        Vector3 target = _targets[_index];
        Vector3 current = _rb.position;

        float step = speed * Time.fixedDeltaTime;
        Vector3 next = Vector3.MoveTowards(current, target, step);

        if (useKinematic)
        {
            _rb.MovePosition(next);
        }
        else
        {
            Vector3 toTarget = (target - current);
            Vector3 desiredVel = toTarget.normalized * speed;

            // Slow down near target to avoid jitter
            if (toTarget.magnitude < 0.05f)
                desiredVel = toTarget / Time.fixedDeltaTime;

#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = desiredVel;
#else
            _rb.velocity = desiredVel;
#endif
        }

        // Arrived?
        if ((target - next).sqrMagnitude <= 0.000001f)
        {
            _waitTimer = waitAtWaypoint;
            AdvanceIndex();
        }

        UpdateVelocity();
    }

    private void AdvanceIndex()
    {
        if (_targets.Length <= 1) return;

        int nextIndex = _index + _dir;

        if (nextIndex >= _targets.Length || nextIndex < 0)
        {
            if (pingPong)
            {
                _dir *= -1;
                nextIndex = _index + _dir;
            }
            else
            {
                nextIndex = (nextIndex >= _targets.Length) ? 0 : _targets.Length - 1;
            }
        }

        _index = Mathf.Clamp(nextIndex, 0, _targets.Length - 1);
    }

    private void UpdateVelocity()
    {
        Vector3 pos = _rb.position;
        CurrentVelocity = (pos - _lastPos) / Time.fixedDeltaTime;
        _lastPos = pos;
    }

    private void StopMotionImmediate()
    {
        _waitTimer = 0f;
        CurrentVelocity = Vector3.zero;

        if (!useKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
#else
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
#endif
        }

        _lastPos = _rb.position;
    }

    // -------- Public Controls (call these from switches, triggers, etc.) --------

    public void SetRunning(bool running)
    {
        if (_running == running) return;

        _running = running;

        if (_running)
        {
            if (!resumeFromCurrentTarget)
            {
                _index = 0;
                _dir = 1;
                _waitTimer = 0f;
            }

            _lastPos = _rb.position;
        }
        else
        {
            StopMotionImmediate();
        }
    }

    public void ToggleRunning() => SetRunning(!_running);
    public void StartRunning() => SetRunning(true);
    public void StopRunning() => SetRunning(false);
}
