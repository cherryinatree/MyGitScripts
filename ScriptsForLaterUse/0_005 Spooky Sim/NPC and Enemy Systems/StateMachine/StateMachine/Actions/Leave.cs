using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Leave : CombatAction
{
    [Header("Events")]
    public UnityEvent onArrivedAtItem;

    [Header("Exit")]
    [SerializeField] private string exitPointName = "ExitPoint";

    [Tooltip("How close to the exit counts as 'left'.")]
    [SerializeField] private float exitArrivalRadius = 1.2f;

    [Tooltip("Customers will aim for a random spot near the exit to avoid piling up.")]
    [SerializeField] private float exitSpreadRadius = 2.0f;

    [Header("Crowd / Blocking")]
    [Tooltip("Put customers on a Customers layer and set this to that layer.")]
    [SerializeField] private LayerMask customersMask = ~0;

    [SerializeField] private float minSeparation = 0.8f;

    [SerializeField] private int sampleSpotAttempts = 14;

    [SerializeField] private float blockedRepathAfterSeconds = 0.6f;
    [SerializeField] private float blockedVelocityThreshold = 0.08f;
    [SerializeField] private float blockedDesiredThreshold = 0.25f;

    [Header("Arrival / Safety")]
    [SerializeField] private float arriveEpsilon = 0.25f;

    [Tooltip("If the agent isn't making progress for this long, pick a new exit spot.")]
    [SerializeField] private float stuckTimeoutSeconds = 4f;

    [Header("Rotation")]
    public float turnSpeed = 12f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBoolParam = "IsWalking";
    [SerializeField] private string speedFloatParam = "Speed";
    [SerializeField] private float walkThreshold = 0.05f;
    [SerializeField] private bool normalizeSpeed = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Transform exitPoint;
    private NavMeshAgent navMeshAgent;
    private CustomerShopping customerShopping;

    private int _walkBoolHash;
    private int _speedFloatHash;
    private bool _hasWalkBool;
    private bool _hasSpeedFloat;

    private float _blockedTimer;
    private float _stuckTimer;
    private bool _invoked;

    private Vector3 _exitDestination;

    public override void OnEnterState()
    {
        base.OnEnterState();
        Debug.Log("Leave State Entered");
        _blockedTimer = 0f;
        _stuckTimer = 0f;
        _invoked = false;

        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        if (customerShopping != null && customerShopping.AssignedCheckOutLine != null)
            customerShopping.AssignedCheckOutLine.LeaveLine(gameObject);

        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _hasWalkBool = !string.IsNullOrWhiteSpace(walkBoolParam);
        _hasSpeedFloat = !string.IsNullOrWhiteSpace(speedFloatParam);
        if (_hasWalkBool) _walkBoolHash = Animator.StringToHash(walkBoolParam);
        if (_hasSpeedFloat) _speedFloatHash = Animator.StringToHash(speedFloatParam);

        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.autoRepath = true;
        }

        ResolveExitPoint();

        if (exitPoint != null && navMeshAgent != null)
        {
            _exitDestination = FindFreeNavMeshSpotNear(exitPoint.position, exitSpreadRadius, minSeparation, sampleSpotAttempts);
            navMeshAgent.SetDestination(_exitDestination);

            if (debugLogs)
                Debug.Log($"[Leave] {name} destination set to {_exitDestination} (exit={exitPoint.position})");
        }
        else
        {
            if (debugLogs)
                Debug.LogWarning($"[Leave] {name} cannot set destination. exitPoint={(exitPoint ? "OK" : "NULL")} agent={(navMeshAgent ? "OK" : "NULL")}");
        }

        UpdateWalkAnim();
    }

    public override void PerformAction()
    {
        if (_invoked) return;

        if (navMeshAgent == null)
        {
            StopWalkAnim();
            return;
        }

        if (exitPoint == null)
        {
            ResolveExitPoint();
            StopWalkAnim(); // don’t “complete” if we don’t even know where to go
            return;
        }

        UpdateWalkAnim();

        // Keep destination alive (in case something cleared the path)
        if (!navMeshAgent.pathPending && !navMeshAgent.hasPath && !navMeshAgent.isStopped)
        {
            navMeshAgent.SetDestination(_exitDestination);
        }

        HandleBlockedRepath();
        HandleStuckFallback();

        // Rotate toward movement (or toward exit if velocity is tiny)
        Vector3 dir = navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f
            ? navMeshAgent.desiredVelocity
            : (exitPoint.position - transform.position);

        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // ARRIVAL: do NOT use "dir ~ 0" as success.
        if (HasArrivedAtExit())
        {
            StopWalkAnim();
            _invoked = true;

            if (debugLogs)
                Debug.Log($"[Leave] {name} arrived at exit. Invoking onArrivedAtItem.");

            onArrivedAtItem?.Invoke();
        }
    }

    private void ResolveExitPoint()
    {
        if (exitPoint != null) return;

        var exitObj = GameObject.Find(exitPointName);
        exitPoint = exitObj ? exitObj.transform : null;

        if (exitPoint == null && debugLogs)
            Debug.LogError($"[Leave] Exit point '{exitPointName}' not found in scene.");
    }

    private bool HasArrivedAtExit()
    {
        if (navMeshAgent.pathPending) return false;

        // If we are physically near the exit, we’re done
        if (Vector3.Distance(transform.position, exitPoint.position) <= exitArrivalRadius)
            return true;

        // If we are near our chosen exit spot, we’re done
        if (navMeshAgent.hasPath && navMeshAgent.remainingDistance <= (navMeshAgent.stoppingDistance + arriveEpsilon))
            return true;

        return false;
    }

    private void HandleBlockedRepath()
    {
        bool barelyMoving = navMeshAgent.velocity.magnitude < blockedVelocityThreshold;
        bool wantsToMove = navMeshAgent.desiredVelocity.magnitude > blockedDesiredThreshold;

        bool notArrived = navMeshAgent.remainingDistance > (navMeshAgent.stoppingDistance + arriveEpsilon + 0.2f);

        if (wantsToMove && barelyMoving && notArrived)
        {
            _blockedTimer += Time.deltaTime;
            if (_blockedTimer >= blockedRepathAfterSeconds)
            {
                _blockedTimer = 0f;

                _exitDestination = FindFreeNavMeshSpotNear(exitPoint.position, exitSpreadRadius, minSeparation, sampleSpotAttempts);
                navMeshAgent.SetDestination(_exitDestination);

                if (debugLogs)
                    Debug.Log($"[Leave] {name} blocked -> repath to {_exitDestination}");
            }
        }
        else
        {
            _blockedTimer = 0f;
        }
    }

    private void HandleStuckFallback()
    {
        if (navMeshAgent.pathPending) return;

        bool barelyMoving = navMeshAgent.velocity.sqrMagnitude < 0.01f;
        bool badPath = navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid;

        if (barelyMoving || badPath)
            _stuckTimer += Time.deltaTime;
        else
            _stuckTimer = 0f;

        if (_stuckTimer >= stuckTimeoutSeconds)
        {
            _stuckTimer = 0f;

            _exitDestination = FindFreeNavMeshSpotNear(exitPoint.position, exitSpreadRadius, minSeparation, sampleSpotAttempts);
            navMeshAgent.SetDestination(_exitDestination);

            if (debugLogs)
                Debug.Log($"[Leave] {name} stuck/bad path -> new exit spot {_exitDestination}");
        }
    }

    private Vector3 FindFreeNavMeshSpotNear(Vector3 anchor, float radius, float separation, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 candidate = anchor + new Vector3(r.x, 0f, r.y);

            if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                continue;

            Vector3 pos = hit.position;

            if (IsSpotOccupied(pos, separation))
                continue;

            return pos;
        }

        if (NavMesh.SamplePosition(anchor, out var anchorHit, 1.5f, NavMesh.AllAreas))
            return anchorHit.position;

        return anchor;
    }

    private bool IsSpotOccupied(Vector3 pos, float separation)
    {
        var hits = Physics.OverlapSphere(pos, separation, customersMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var other = hits[i].GetComponentInParent<NavMeshAgent>();
            if (other == null) continue;
            if (other == navMeshAgent) continue;
            return true;
        }
        return false;
    }

    private void UpdateWalkAnim()
    {
        if (animator == null || navMeshAgent == null) return;

        float speed = navMeshAgent.velocity.magnitude;
        bool isWalking = speed > walkThreshold && !navMeshAgent.isStopped;

        if (_hasWalkBool) animator.SetBool(_walkBoolHash, isWalking);

        if (_hasSpeedFloat)
        {
            float v = speed;
            if (normalizeSpeed && navMeshAgent.speed > 0.0001f)
                v = Mathf.Clamp01(speed / navMeshAgent.speed);

            animator.SetFloat(_speedFloatHash, v);
        }
    }

    private void StopWalkAnim()
    {
        if (animator == null) return;

        if (_hasWalkBool) animator.SetBool(_walkBoolHash, false);
        if (_hasSpeedFloat) animator.SetFloat(_speedFloatHash, 0f);
    }
}
