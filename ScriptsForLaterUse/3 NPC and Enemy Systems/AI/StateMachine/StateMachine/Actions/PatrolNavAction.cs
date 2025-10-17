using UnityEngine;

/// <summary>
/// Patrol using NavMesh: always run at a set speed; pick new reachable targets within a radius.
/// No waypoints required.
/// </summary>
public class PatrolNavAction : CombatAction
{
    [Header("Patrol")]
    public float patrolRadius = 12f;
    public float repathInterval = 3.0f;           // force a new point every few seconds
    public float reachedTolerance = 0.2f;

    [Header("Speed")]
    public float runSpeed = 3.8f;                 // state-owned speed
    public float stoppingDistance = 0.2f;         // keep moving—tiny stop radius

    // Modules
    private CoreEnemy core;
    private EnemyNavMovement navMove;
    private EnemyAnimatorController anim;
    private EnemySoundController sfx;

    // State
    private Vector3 spawnOrigin;
    private float nextRepathAt;

    protected override void Awake()
    {
        base.Awake();
        core = GetComponentInParent<CoreEnemy>();
        navMove = GetComponentInParent<EnemyNavMovement>();
        anim = GetComponentInParent<EnemyAnimatorController>();
        sfx = GetComponentInParent<EnemySoundController>();
    }

    public override void Initialization()
    {
        base.Initialization();
        spawnOrigin = core ? core.transform.position : transform.position;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        if (ShouldInitialize) Initialization();

        // Speed & stopping config
        navMove?.SetMoveSpeed(runSpeed);
        navMove?.SetStoppingDistance(stoppingDistance);

        // Start moving to a target
        PickNewTarget();
        nextRepathAt = Time.time + repathInterval;

        anim?.PlayWalk(true);
    }

    public override void PerformAction()
    {
        if (navMove == null) return;

        // Force a new random point periodically, or when reached
        if (navMove.ReachedDestination(reachedTolerance) || Time.time >= nextRepathAt || !navMove.HasValidPath)
        {
            PickNewTarget();
            nextRepathAt = Time.time + repathInterval;
        }

        // Optional: play footsteps by time or animation events
        anim?.PlayWalk(true);
    }

    public override void OnExitState()
    {
        base.OnExitState();
        navMove?.Stop();
        anim?.PlayIdle();
        sfx?.PlayIdle();
    }

    private void PickNewTarget()
    {
        Vector3 origin = spawnOrigin;
        if (core) origin = core.transform.position; // wander around current position

        if (NavUtils.RandomReachablePoint(origin, patrolRadius, out var target))
        {
            navMove.SetDestination(target);
        }
        else
        {
            // fallback: try forward a bit
            Vector3 forward = (core ? core.transform.forward : transform.forward);
            Vector3 candidate = origin + forward * Mathf.Max(3f, patrolRadius * 0.25f);
            if (NavUtils.Sample(candidate, out var hit, 3f))
                navMove.SetDestination(hit);
        }
    }
}
