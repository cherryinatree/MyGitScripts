using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class GoToItem : CombatAction
{
    [Header("Events")]
    public UnityEvent onArrivedAtItem;

    [Tooltip("Called when no valid item exists AND the customer has items carried (go checkout).")]
    public UnityEvent GoToLine;

    [Tooltip("Called when no valid item exists AND the customer has nothing carried (leave store).")]
    public UnityEvent LeaveStore;

    private CustomerStoreManager customerStoreManager;
    private CustomerShopping customerShopping;

    private NavMeshAgent navMeshAgent;
    private Animator animator;

    private Carryable _targetItem;
    private MerchandisingFixtures _targetFixture;

    private Vector3 _itemPos;
    private Vector3 _anchor;
    private Vector3 _currentDestination;

    private Collider _viewCollider;
    private float _areaRadius;

    private bool hasInitializedShopping = false;
    private bool _exitedEarly;

    [Header("Rotation")]
    public float turnSpeed = 12f;

    [Header("Arrival")]
    [Tooltip("How close is 'good enough' to consider arrived (added to stoppingDistance).")]
    [SerializeField] private float arriveEpsilon = 0.25f;

    [Tooltip("If we're within this distance of the ITEM, we consider ourselves arrived (matches PutItemInBag() ~ 3f).")]
    [SerializeField] private float itemReachDistance = 2.8f;

    [Header("Crowd / Blocking")]
    [Tooltip("Put customers on a Customers layer and set this to that layer.")]
    [SerializeField] private LayerMask customersMask = ~0;

    [Tooltip("Minimum spacing between customers when choosing a viewing spot.")]
    [SerializeField] private float minSeparation = 0.8f;

    [Tooltip("How many random samples we try when finding a free spot.")]
    [SerializeField] private int sampleSpotAttempts = 14;

    [Tooltip("If we WANT to move but aren't moving for this long, we pick a new nearby spot.")]
    [SerializeField] private float blockedRepathAfterSeconds = 0.6f;

    [Tooltip("Velocity below this is considered 'blocked'.")]
    [SerializeField] private float blockedVelocityThreshold = 0.08f;

    [Tooltip("Desired velocity above this means the agent *wants* to move.")]
    [SerializeField] private float blockedDesiredThreshold = 0.25f;

    [Tooltip("Fallback radius around the item if fixture viewing area isn't available.")]
    [SerializeField] private float fallbackRadius = 1.2f;

    private float _blockedTimer;
    private float _stuckTimer;

    [Tooltip("If the agent can't make progress for this long, repick a spot.")]
    [SerializeField] private float stuckTimeoutSeconds = 4f;

    [Header("Animation")]
    [Tooltip("Optional bool param to toggle walking (e.g. IsWalking). Leave empty to skip.")]
    [SerializeField] private string walkBoolParam = "IsWalking";

    [Tooltip("Optional float param for speed (e.g. Speed). Leave empty to skip.")]
    [SerializeField] private string speedFloatParam = "Speed";

    [Tooltip("Speed (m/s) below this counts as idle for animation.")]
    [SerializeField] private float walkThreshold = 0.05f;

    [Tooltip("If true, speed float is normalized by agent.speed (good for blend trees expecting 0..1).")]
    [SerializeField] private bool normalizeSpeed = true;

    private int _walkBoolHash;
    private int _speedFloatHash;
    private bool _hasWalkBool;
    private bool _hasSpeedFloat;

    public override void OnEnterState()
    {
        base.OnEnterState();

        _exitedEarly = false;
        _blockedTimer = 0f;
        _stuckTimer = 0f;

        if (customerStoreManager == null) customerStoreManager = FindFirstObjectByType<CustomerStoreManager>();
        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Cache animator hashes
        _hasWalkBool = !string.IsNullOrWhiteSpace(walkBoolParam);
        _hasSpeedFloat = !string.IsNullOrWhiteSpace(speedFloatParam);
        if (_hasWalkBool) _walkBoolHash = Animator.StringToHash(walkBoolParam);
        if (_hasSpeedFloat) _speedFloatHash = Animator.StringToHash(speedFloatParam);

        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.autoRepath = true;

            // Optional: helps if spawned slightly off navmesh
            navMeshAgent.Warp(transform.position);
        }

        // Ensure shopping list exists
        if (customerShopping != null && !hasInitializedShopping)
        {
            customerShopping.InitializeShoppingList();
            hasInitializedShopping = true;
        }

        // Pick a valid target item or route away (NO DESTROY)
        if (!TryResolveTargetItemAndPosition())
        {
            RouteNoItem();
            return;
        }

        ResolveViewingAreaFromItem(_targetItem, _itemPos);

        // Pick a free spot inside that viewing area (or near item) and go
        _currentDestination = FindFreeNavMeshSpotNear(_anchor, _areaRadius, minSeparation, sampleSpotAttempts);
        if (navMeshAgent != null)
            navMeshAgent.SetDestination(_currentDestination);

        UpdateWalkAnim();
    }

    public override void PerformAction()
    {
        if (_exitedEarly)
        {

            RouteNoItem();
            return;

        }

        if (navMeshAgent == null)
        {
            StopWalkAnim();
            return;
        }

        // Target might disappear mid-walk (item bought, despawned, etc.)
        if (_targetItem == null)
        {
            StopWalkAnim();
            RouteNoItem();
            return;
        }

        UpdateWalkAnim();

        // Re-path if blocked by other customers
        HandleBlockedRepath();

        // Stuck handling
        HandleStuckFallback();

        // Rotate toward movement direction
        Vector3 dir = navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f
            ? navMeshAgent.desiredVelocity
            : (navMeshAgent.steeringTarget - transform.position);

        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Arrived: good enough in area OR close enough to the actual item
        if (HasArrivedCloseEnough())
        {
            StopWalkAnim();
            onArrivedAtItem?.Invoke();
        }
    }

    private bool TryResolveTargetItemAndPosition()
    {
        if (customerShopping == null || customerShopping.shoppingList == null)
            return false;

        // Clean out null entries at the front
        while (customerShopping.shoppingList.Count > 0 && customerShopping.shoppingList[0] == null)
            customerShopping.shoppingList.RemoveAt(0);

        if (customerShopping.shoppingList.Count == 0)
            return false;

        _targetItem = customerShopping.shoppingList[0];
        if (_targetItem == null)
            return false;

        // Prefer your storeManager mapping if possible
        if (customerStoreManager != null)
        {
            Transform t = null;
            try
            {
                t = customerStoreManager.GetItemPosition(_targetItem);
            }
            catch
            {
                t = null;
            }

            if (t != null)
            {
                _itemPos = t.position;
                return true;
            }
        }

        // Fallback: use item transform if it exists
        _itemPos = _targetItem.transform.position;
        return true;
    }

    private void RouteNoItem()
    {
        _exitedEarly = true;
        StopWalkAnim();

        int carried = (customerShopping != null && customerShopping.itemsCarried != null)
            ? customerShopping.itemsCarried.Count
            : 0;

        // If they have items, they should check out. If not, they should leave.
        if (carried > 0)
        {
            GoToLine?.Invoke();
        }
        else
        {
            Debug.Log("GoToItem: No valid target item and nothing carried, leaving store.");
            LeaveStore?.Invoke();
        }
    }

    private void ResolveViewingAreaFromItem(Carryable item, Vector3 itemPos)
    {
        _targetFixture = item != null ? item.GetFixtureParent() : null;

        _viewCollider = null;
        _areaRadius = fallbackRadius;

        if (_targetFixture != null && _targetFixture.viewingArea != null)
        {
            _anchor = _targetFixture.viewingArea.transform.position;
            _viewCollider = _targetFixture.viewingArea.GetComponent<Collider>();

            if (_viewCollider != null)
            {
                // Area size from collider bounds
                var ext = _viewCollider.bounds.extents;
                _areaRadius = Mathf.Max(ext.x, ext.z);
                _areaRadius = Mathf.Max(_areaRadius, 0.75f);

                // Bias anchor toward the item so they tend to stand on the correct side
                Vector3 closest = _viewCollider.ClosestPoint(itemPos);
                closest.y = _anchor.y;
                _anchor = Vector3.Lerp(_anchor, closest, 0.6f);
            }
            return;
        }

        // Fallback: no fixture/viewing area
        _anchor = itemPos;
        _areaRadius = fallbackRadius;
    }

    private bool HasArrivedCloseEnough()
    {
        if (navMeshAgent.pathPending) return false;

        // Close to the item itself (most important for PutItemInBag)
        if (_targetItem != null)
        {
            float dItem = Vector3.Distance(transform.position, _targetItem.transform.position);
            if (dItem <= itemReachDistance) return true;
        }

        // Normal navmesh arrival
        if (navMeshAgent.remainingDistance <= (navMeshAgent.stoppingDistance + arriveEpsilon))
            return true;

        // Inside viewing area = good enough
        if (_viewCollider != null && _viewCollider.bounds.Contains(transform.position))
            return true;

        // Close enough to anchor
        float arriveRadius = Mathf.Clamp(_areaRadius * 0.55f, 0.75f, 2.0f);
        return Vector3.Distance(transform.position, _anchor) <= arriveRadius;
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

                _currentDestination = FindFreeNavMeshSpotNear(_anchor, _areaRadius, minSeparation, sampleSpotAttempts);
                navMeshAgent.SetDestination(_currentDestination);
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

            _currentDestination = FindFreeNavMeshSpotNear(_anchor, _areaRadius, minSeparation, sampleSpotAttempts);
            navMeshAgent.SetDestination(_currentDestination);
        }
    }

    private Vector3 FindFreeNavMeshSpotNear(Vector3 anchor, float radius, float separation, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = SampleCandidate(anchor, radius);

            if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                continue;

            Vector3 pos = hit.position;

            if (IsSpotOccupied(pos, separation))
                continue;

            return pos;
        }

        // Fallback: navmesh-snap anchor
        if (NavMesh.SamplePosition(anchor, out var anchorHit, 1.5f, NavMesh.AllAreas))
            return anchorHit.position;

        return anchor;
    }

    private Vector3 SampleCandidate(Vector3 anchor, float radius)
    {
        // If viewing area is a BoxCollider, sample inside it (respects rotation)
        if (_viewCollider is BoxCollider box)
        {
            Vector3 local = box.center;
            Vector3 half = box.size * 0.5f;

            local += new Vector3(
                Random.Range(-half.x, half.x),
                0f,
                Random.Range(-half.z, half.z)
            );

            Vector3 world = box.transform.TransformPoint(local);
            world.y = anchor.y;
            return world;
        }

        // Otherwise, sample a disk around anchor
        Vector2 r = Random.insideUnitCircle * radius;
        return anchor + new Vector3(r.x, 0f, r.y);
    }

    private bool IsSpotOccupied(Vector3 pos, float separation)
    {
        var hits = Physics.OverlapSphere(pos, separation, customersMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var otherAgent = hits[i].GetComponentInParent<NavMeshAgent>();
            if (otherAgent == null) continue;
            if (navMeshAgent != null && otherAgent == navMeshAgent) continue; // self
            return true;
        }

        return false;
    }

    private void UpdateWalkAnim()
    {
        if (animator == null || navMeshAgent == null) return;

        float speed = navMeshAgent.velocity.magnitude;
        bool isWalking = speed > walkThreshold && !navMeshAgent.isStopped;

        if (_hasWalkBool)
            animator.SetBool(_walkBoolHash, isWalking);

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

        if (_hasWalkBool)
            animator.SetBool(_walkBoolHash, false);

        if (_hasSpeedFloat)
            animator.SetFloat(_speedFloatHash, 0f);
    }
}
