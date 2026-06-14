using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Terrorize using NavMesh: stalks (or flees) the player and sprinkles creepy behavior.
/// Keeps pathing simple: replan at intervals, let NavMesh handle avoidance.
/// </summary>
public class TerrorizeNavAction : CombatAction
{
    [Header("Refs")]
    public Transform player;
    public EnemyNavMovement navMove;
    public EnemyAnimatorController anim;
    public EnemySoundController sfx;

    [Header("Stalk / Flee")]
    public float stalkSpeed = 3.2f;
    public float fleeSpeed = 4.0f;
    public float replanInterval = 0.35f;  // how often we refresh destination
    public float stoppingDistance = 1.0f; // don’t hug the player in this state
    public bool fleeWhenLookedAt = false;
    public bool fleeWhenLit = false;
    [Tooltip("Set this true externally when the player’s light hits the monster.")]
    public bool IsLit = false;

    [Header("Player Vision Approx (for 'looked at')")]
    [Range(10f, 180f)] public float playerFOV = 90f;
    public float visionRange = 25f;
    public Transform playerEye;           // camera or head
    public LayerMask losBlockersMask = ~0;

    [Header("Creepy Audio")]
    public bool enableCreepySounds = true;
    public float creepyInterval = 10f;

    [Header("Blink Behind Player (optional)")]
    public bool enableBlink = false;
    public float blinkDistance = 3.0f;     // ~10ft
    public float blinkCooldown = 8f;

    private float _nextPlanAt;
    private float _nextCreepyAt;
    private float _nextBlinkAt;

    protected override void Awake()
    {
        base.Awake();
        if (!navMove) navMove = GetComponentInParent<EnemyNavMovement>();
        if (!anim) anim = GetComponentInParent<EnemyAnimatorController>();
        if (!sfx) sfx = GetComponentInParent<EnemySoundController>();
        if (!player && Camera.main) player = Camera.main.transform; // fallback
        if (!playerEye && player) playerEye = player;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        if (!player || !navMove) return;

        navMove.SetMoveSpeed(stalkSpeed);
        navMove.SetStoppingDistance(stoppingDistance);
        _nextPlanAt = 0f;
        _nextCreepyAt = Time.time + Mathf.Max(2f, creepyInterval);
        _nextBlinkAt = Time.time + 2f;

        anim?.PlayWalk(true);
    }

    public override void PerformAction()
    {
        if (!player || !navMove) return;

        bool playerSeesUs = PlayerCanSeeMe();
        bool shouldFlee = (fleeWhenLookedAt && playerSeesUs) || (fleeWhenLit && IsLit);

        // Creepy sound when not seen
        if (enableCreepySounds && !playerSeesUs && Time.time >= _nextCreepyAt)
        {
            sfx?.PlayIdle(); // swap for your creepy cue
            _nextCreepyAt = Time.time + creepyInterval;
        }

        // Optional blink behind if unseen
        if (enableBlink && !playerSeesUs && Time.time >= _nextBlinkAt)
        {
            TryBlinkBehindPlayer();
            _nextBlinkAt = Time.time + blinkCooldown;
        }

        // Replan path at interval
        if (Time.time >= _nextPlanAt)
        {
            if (shouldFlee)
            {
                navMove.SetMoveSpeed(fleeSpeed);
                Vector3 away = (navMove.transform.position - player.position);
                if (away.sqrMagnitude < 0.01f) away = navMove.transform.forward;
                away.Normalize();
                Vector3 target = navMove.transform.position + away * 6f; // flee 6m and keep running
                if (NavUtils.Sample(target, out var hit, 3f)) navMove.SetDestination(hit);
            }
            else
            {
                navMove.SetMoveSpeed(stalkSpeed);
                // Stalk: set destination to the player (sample ensures on-mesh)
                if (NavUtils.Sample(player.position, out var onMesh, 2f)) navMove.SetDestination(onMesh);
                else navMove.SetDestination(player.position);
            }
            _nextPlanAt = Time.time + replanInterval;
        }

        anim?.PlayWalk(true);
    }

    public override void OnExitState()
    {
        base.OnExitState();
        navMove?.Stop();
        anim?.PlayIdle();
    }

    // --------- Helpers ---------
    private bool PlayerCanSeeMe()
    {
        if (!playerEye) return false;

        Vector3 toMe = navMove.transform.position - playerEye.position;
        float dist = toMe.magnitude;
        if (dist > visionRange) return false;

        Vector3 dir = toMe.normalized;
        if (Vector3.Angle(playerEye.forward, dir) > playerFOV * 0.5f) return false;

        if (Physics.Raycast(playerEye.position, dir, out RaycastHit hit, dist, losBlockersMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private void TryBlinkBehindPlayer()
    {
        Vector3 behind = -Flatten(player.forward) * blinkDistance;
        Vector3 target = player.position + behind;
        if (NavUtils.Sample(target, out var onMesh, 2.5f))
        {
            navMove.Agent.Warp(onMesh); // instant, safe on-navmesh
            sfx?.PlayIdle();            // cue — swap to your blink SFX if you have one
        }
    }

    private static Vector3 Flatten(Vector3 v) { v.y = 0; return v.sqrMagnitude > 0 ? v.normalized : Vector3.forward; }
}
