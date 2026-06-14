using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class GoToLine : CombatAction
{
    [Header("Events")]
    public UnityEvent onArrivedAtItem;

    private CustomerStoreManager customerStoreManager;
    private CustomerShopping customerShopping;
    private Vector3 targetPosition;
    private NavMeshAgent navMeshAgent;

    [Header("Rotation")]
    public float turnSpeed = 12f; // higher = snappier

    [Header("Animation")]
    [Tooltip("Animator on this character (or a child). If null, we'll try GetComponentInChildren<Animator>().")]
    [SerializeField] private Animator animator;

    [Tooltip("Optional bool param to toggle walking (e.g. IsWalking). Leave empty to skip.")]
    [SerializeField] private string walkBoolParam = "IsWalking";

    [Tooltip("Optional float param for speed (e.g. Speed). Leave empty to skip.")]
    [SerializeField] private string speedFloatParam = "Speed";

    [Tooltip("Speed (m/s) below this counts as idle for animation.")]
    [SerializeField] private float walkThreshold = 0.05f;

    [Tooltip("If true, speed float is normalized by agent.speed (0..1).")]
    [SerializeField] private bool normalizeSpeed = true;

    private int _walkBoolHash;
    private int _speedFloatHash;
    private bool _hasWalkBool;
    private bool _hasSpeedFloat;

    public override void OnEnterState()
    {
        base.OnEnterState();

        if (customerStoreManager == null) customerStoreManager = FindFirstObjectByType<CustomerStoreManager>();
        if (customerShopping == null) customerShopping = GetComponent<CustomerShopping>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _hasWalkBool = !string.IsNullOrWhiteSpace(walkBoolParam);
        _hasSpeedFloat = !string.IsNullOrWhiteSpace(speedFloatParam);
        if (_hasWalkBool) _walkBoolHash = Animator.StringToHash(walkBoolParam);
        if (_hasSpeedFloat) _speedFloatHash = Animator.StringToHash(speedFloatParam);

        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false; // we rotate manually
            navMeshAgent.isStopped = false;
            navMeshAgent.Warp(transform.position);
        }

        // Pick a line + reserve a spot
        if (customerShopping != null && customerStoreManager != null)
        {
            customerShopping.AssignedCheckOutLine = customerStoreManager.GetRandomCheckoutLine();

            if (customerShopping.AssignedCheckOutLine != null && !customerShopping.AssignedCheckOutLine.IsLineFull())
            {
                targetPosition = customerShopping.AssignedCheckOutLine.GetFirstAvailableSpot(gameObject);
            }
            else
            {
                // No valid line/spot; stop walk anim so we don't moonwalk in place
                StopWalkAnim();
                return;
            }
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.SetDestination(targetPosition);
        }

        UpdateWalkAnim();
    }

    public override void PerformAction()
    {
        if (navMeshAgent == null)
        {
            StopWalkAnim();
            return;
        }

        UpdateWalkAnim();

        // Prefer desiredVelocity; fallback to steeringTarget when slowing near destination
        Vector3 dir = navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f
            ? navMeshAgent.desiredVelocity
            : (navMeshAgent.steeringTarget - transform.position);

        dir.y = 0f;

        // Arrived / not meaningfully moving
        if (dir.sqrMagnitude < 0.0001f)
        {
            StopWalkAnim();
            onArrivedAtItem?.Invoke();
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
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
