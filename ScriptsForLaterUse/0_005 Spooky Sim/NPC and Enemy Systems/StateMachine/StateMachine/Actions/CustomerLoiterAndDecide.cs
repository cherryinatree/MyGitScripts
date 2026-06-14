using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class CustomerLoiterAndDecide : CombatAction
{
    [Header("Decision Events")]
    public UnityEvent DecideToBuy;   // hook this to your "start shopping" state/event
    public UnityEvent DecideToLeave; // hook this to your Leave state/event

    [Header("Loiter Targets")]
    [Tooltip("If empty, this state will auto-find all CustomerLoiterPoint in the scene on enter.")]
    [SerializeField] private List<CustomerLoiterPoint> points = new List<CustomerLoiterPoint>();

    [Header("Randomness")]
    [SerializeField] private int minThingsToLookAt = 2;
    [SerializeField] private int maxThingsToLookAt = 6;

    [SerializeField] private float minTotalSeconds = 8f;
    [SerializeField] private float maxTotalSeconds = 25f;

    [SerializeField] private float minLookSeconds = 1.2f;
    [SerializeField] private float maxLookSeconds = 3.5f;

    [Header("Movement")]
    [SerializeField] private float arriveEpsilon = 0.25f;
    [SerializeField] private float turnSpeed = 10f;

    [Tooltip("If the agent can't reach a point after this long, pick a different one.")]
    [SerializeField] private float stuckTimeoutSeconds = 4f;

    [Header("Crowd / Blocking")]
    [Tooltip("Set this to your Customers layer for best results. If left as Everything, we filter by NavMeshAgent anyway.")]
    [SerializeField] private LayerMask customerMask = ~0;

    [Tooltip("How many random samples we try when finding a free spot around a point.")]
    [SerializeField] private int sampleSpotAttempts = 12;

    [Tooltip("If we WANT to move but are barely moving for this long, we repath to a new nearby spot.")]
    [SerializeField] private float blockedRepathAfterSeconds = 0.6f;

    [Tooltip("Velocity below this is considered 'not moving'.")]
    [SerializeField] private float blockedVelocityThreshold = 0.08f;

    [Tooltip("Desired velocity above this means the agent *wants* to move.")]
    [SerializeField] private float blockedDesiredThreshold = 0.25f;

    [Header("Looking Around")]
    [Tooltip("How fast the customer scans when no valid look target exists.")]
    [SerializeField] private float scanDegreesPerSecond = 35f;

    [Header("Decision Tuning")]
    [Range(0f, 1f)]
    [SerializeField] private float baseBuyChance = 0.65f;

    [Tooltip("Extra buy chance gained per inspected thing.")]
    [SerializeField] private float buyChancePerLook = 0.05f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Tooltip("Walking bool parameter (optional).")]
    [SerializeField] private string walkBoolParam = "IsWalking";

    [Tooltip("Speed float parameter for blend trees (optional).")]
    [SerializeField] private string speedFloatParam = "Speed";

    [Tooltip("Looking bool parameter that enables your LookAt animation (recommended).")]
    [SerializeField] private string lookBoolParam = "IsLooking";

    [SerializeField] private float walkThreshold = 0.05f;
    [SerializeField] private bool normalizeSpeed = true;

    private NavMeshAgent _agent;

    private enum Phase { Walking, Looking }
    private Phase _phase;

    private CustomerLoiterPoint _current;
    private readonly HashSet<CustomerLoiterPoint> _visited = new HashSet<CustomerLoiterPoint>();

    private float _endTime;
    private int _targetLookCount;
    private int _lookedCount;

    private float _lookEndTime;
    private float _stuckTimer;
    private float _blockedTimer;

    private Vector3 _currentDestination;

    // Animator cached
    private bool _hasWalkBool, _hasSpeedFloat, _hasLookBool;
    private int _walkBoolHash, _speedFloatHash, _lookBoolHash;

    public override void OnEnterState()
    {
        base.OnEnterState();

        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (_agent != null)
        {
            _agent.updateRotation = false; // manual rotation
            _agent.isStopped = false;
            _agent.autoRepath = true;
        }

        // Cache animator params
        _hasWalkBool = !string.IsNullOrWhiteSpace(walkBoolParam);
        _hasSpeedFloat = !string.IsNullOrWhiteSpace(speedFloatParam);
        _hasLookBool = !string.IsNullOrWhiteSpace(lookBoolParam);

        if (_hasWalkBool) _walkBoolHash = Animator.StringToHash(walkBoolParam);
        if (_hasSpeedFloat) _speedFloatHash = Animator.StringToHash(speedFloatParam);
        if (_hasLookBool) _lookBoolHash = Animator.StringToHash(lookBoolParam);

        // Auto-find points if not assigned
        if (points == null) points = new List<CustomerLoiterPoint>();
        if (points.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            points.AddRange(Object.FindObjectsByType<CustomerLoiterPoint>(FindObjectsSortMode.None));
#else
            points.AddRange(Object.FindObjectsOfType<CustomerLoiterPoint>());
#endif
        }

        _visited.Clear();
        _lookedCount = 0;

        _targetLookCount = Random.Range(minThingsToLookAt, maxThingsToLookAt + 1);
        _endTime = Time.time + Random.Range(minTotalSeconds, maxTotalSeconds);

        _phase = Phase.Walking;
        _stuckTimer = 0f;
        _blockedTimer = 0f;

        // Start in walking mode
        SetLooking(false);
        SetWalking(true);
        UpdateSpeedFloat();

        PickNextPointAndGo();
    }

    public override void OnExitState()
    {
        base.OnExitState();

        // Let your normal controller take over
        SetWalking(false);
        SetLooking(false);
        if (_agent != null) _agent.isStopped = false;
    }

    public override void PerformAction()
    {
        if (_agent == null)
        {
            SetWalking(false);
            SetLooking(false);
            DecideNow();
            return;
        }

        // Hard stop conditions
        if (Time.time >= _endTime || _lookedCount >= _targetLookCount)
        {
            SetWalking(false);
            SetLooking(false);
            DecideNow();
            return;
        }

        if (_phase == Phase.Walking)
        {
            SetLooking(false);
            SetWalking(true);
            UpdateSpeedFloat();

            FaceMoveDirection();

            // If we're blocked by a crowd, try a new nearby destination around the SAME point
            HandleBlockedRepath();

            // Stuck handling (invalid path or basically no movement)
            if (!_agent.pathPending)
            {
                bool barelyMoving = _agent.velocity.sqrMagnitude < 0.01f;
                bool badPath = _agent.pathStatus == NavMeshPathStatus.PathInvalid;

                _stuckTimer = (barelyMoving || badPath) ? _stuckTimer + Time.deltaTime : 0f;

                if (_stuckTimer >= stuckTimeoutSeconds)
                {
                    _stuckTimer = 0f;
                    PickNextPointAndGo();
                    return;
                }
            }

            // "Close enough" arrival check
            if (HasArrivedCloseEnough())
            {
                EnterLookingPhase();
                return;
            }
        }
        else // Looking
        {
            SetWalking(false);
            SetLooking(true);
            UpdateSpeedFloat(); // will go to 0 nicely

            LookAtCurrentOrScan();

            if (Time.time >= _lookEndTime)
            {
                _lookedCount++;
                _agent.isStopped = false;
                _phase = Phase.Walking;

                PickNextPointAndGo();
                return;
            }
        }
    }

    private bool HasArrivedCloseEnough()
    {
        if (_agent.pathPending) return false;

        // allow "in the area" rather than exact destination
        float areaRadius = 0.8f;
        if (_current != null)
            areaRadius = Mathf.Max(0.6f, _current.standRadius * 0.65f);

        bool inArea = Vector3.Distance(transform.position, _currentDestination) <= areaRadius;

        bool nearByNav = _agent.remainingDistance <= (_agent.stoppingDistance + arriveEpsilon);

        return inArea || nearByNav;
    }

    private void EnterLookingPhase()
    {
        _phase = Phase.Looking;
        _agent.isStopped = true;

        float minL = minLookSeconds;
        float maxL = maxLookSeconds;

        if (_current != null && _current.minLookSecondsOverride > 0f && _current.maxLookSecondsOverride > 0f)
        {
            minL = _current.minLookSecondsOverride;
            maxL = _current.maxLookSecondsOverride;
        }

        _lookEndTime = Time.time + Random.Range(minL, maxL);

        // instantly switch anim mode
        SetWalking(false);
        SetLooking(true);
        UpdateSpeedFloat();
    }

    private void PickNextPointAndGo()
    {
        if (points == null || points.Count == 0)
        {
            DecideNow();
            return;
        }

        _current = ChoosePointPreferLessCrowded(points, _visited, transform.position, customerMask);
        if (_current != null) _visited.Add(_current);

        Vector3 anchor = _current != null ? _current.StandPosition : transform.position;

        float radius = _current != null ? _current.standRadius : 1.2f;
        float sep = _current != null ? _current.minSeparation : 0.8f;

        _currentDestination = FindFreeNavMeshSpotNear(anchor, radius, sep, sampleSpotAttempts, customerMask);

        _agent.isStopped = false;
        _agent.SetDestination(_currentDestination);

        _stuckTimer = 0f;
        _blockedTimer = 0f;
    }

    private void HandleBlockedRepath()
    {
        // If we WANT to move but aren't moving, likely blocked by other customers.
        bool barelyMoving = _agent.velocity.magnitude < blockedVelocityThreshold;
        bool wantsToMove = _agent.desiredVelocity.magnitude > blockedDesiredThreshold;

        // Don't repath if basically arrived
        bool notArrived = _agent.remainingDistance > (_agent.stoppingDistance + arriveEpsilon + 0.2f);

        if (wantsToMove && barelyMoving && notArrived)
        {
            _blockedTimer += Time.deltaTime;
            if (_blockedTimer >= blockedRepathAfterSeconds)
            {
                _blockedTimer = 0f;

                // Re-pick a nearby spot around the same anchor (sidestep)
                Vector3 anchor = _current != null ? _current.StandPosition : _currentDestination;
                float radius = _current != null ? _current.standRadius : 1.2f;
                float sep = _current != null ? _current.minSeparation : 0.8f;

                _currentDestination = FindFreeNavMeshSpotNear(anchor, radius, sep, sampleSpotAttempts, customerMask);
                _agent.SetDestination(_currentDestination);
            }
        }
        else
        {
            _blockedTimer = 0f;
        }
    }

    private static CustomerLoiterPoint ChoosePointPreferLessCrowded(
        List<CustomerLoiterPoint> all,
        HashSet<CustomerLoiterPoint> visited,
        Vector3 seekerPos,
        LayerMask customerMask)
    {
        CustomerLoiterPoint best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < all.Count; i++)
        {
            var p = all[i];
            if (p == null) continue;

            int nearbyCustomers = CountNearbyCustomers(p.StandPosition, Mathf.Max(1f, p.standRadius), customerMask);

            float notVisitedBonus = visited.Contains(p) ? 0f : 1.5f;

            // If too crowded, penalize heavily
            int over = Mathf.Max(0, nearbyCustomers - p.softMaxViewers);
            float crowdPenalty = over * 2.0f;

            float dist = Vector3.Distance(seekerPos, p.StandPosition);
            float distPenalty = dist * 0.15f;

            float score = (p.weight * 2f) + notVisitedBonus - crowdPenalty - distPenalty;

            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        return best != null ? best : (all.Count > 0 ? all[0] : null);
    }

    private static int CountNearbyCustomers(Vector3 pos, float radius, LayerMask customerMask)
    {
        int count = 0;
        var hits = Physics.OverlapSphere(pos, radius, customerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            var agent = hits[i].GetComponentInParent<NavMeshAgent>();
            if (agent != null) count++;
        }

        return count;
    }

    private Vector3 FindFreeNavMeshSpotNear(Vector3 anchor, float radius, float minSeparation, int attempts, LayerMask customerMask)
    {
        // Try random candidates around anchor
        for (int i = 0; i < attempts; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 candidate = anchor + new Vector3(r.x, 0f, r.y);

            if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                continue;

            Vector3 pos = hit.position;

            if (IsSpotOccupied(pos, minSeparation, customerMask))
                continue;

            return pos;
        }

        // Fallback: snap anchor itself to navmesh
        if (NavMesh.SamplePosition(anchor, out var anchorHit, 1.5f, NavMesh.AllAreas))
            return anchorHit.position;

        return anchor;
    }

    private bool IsSpotOccupied(Vector3 pos, float minSeparation, LayerMask customerMask)
    {
        var hits = Physics.OverlapSphere(pos, minSeparation, customerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            var otherAgent = hits[i].GetComponentInParent<NavMeshAgent>();
            if (otherAgent == null) continue;

            // Ignore self
            if (_agent != null && otherAgent == _agent) continue;

            return true;
        }

        return false;
    }

    private void DecideNow()
    {
        float buyChance = Mathf.Clamp01(baseBuyChance + (_lookedCount * buyChancePerLook));
        if (Random.value < buyChance) DecideToBuy?.Invoke();
        else DecideToLeave?.Invoke();
    }

    private void FaceMoveDirection()
    {
        Vector3 dir = _agent.desiredVelocity.sqrMagnitude > 0.01f
            ? _agent.desiredVelocity
            : (_agent.steeringTarget - transform.position);

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void LookAtCurrentOrScan()
    {
        Vector3 lookDir;

        if (_current != null)
        {
            lookDir = _current.LookPosition - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
                return;
            }
        }

        // Scan if no valid target
        float yaw = Mathf.Sin(Time.time * 0.9f) * scanDegreesPerSecond;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, transform.eulerAngles.y + yaw * Time.deltaTime, 0f),
            0.5f
        );
    }

    // -------------------------
    // Animation helpers
    // -------------------------
    private void SetWalking(bool value)
    {
        if (animator == null || !_hasWalkBool) return;
        animator.SetBool(_walkBoolHash, value);
    }

    private void SetLooking(bool value)
    {
        if (animator == null || !_hasLookBool) return;
        animator.SetBool(_lookBoolHash, value);
    }

    private void UpdateSpeedFloat()
    {
        if (animator == null || !_hasSpeedFloat || _agent == null) return;

        float speed = _agent.velocity.magnitude;

        float v = speed;
        if (normalizeSpeed && _agent.speed > 0.0001f)
            v = Mathf.Clamp01(speed / _agent.speed);

        if (_agent.isStopped || speed < walkThreshold)
            v = 0f;

        animator.SetFloat(_speedFloatHash, v);
    }
}
