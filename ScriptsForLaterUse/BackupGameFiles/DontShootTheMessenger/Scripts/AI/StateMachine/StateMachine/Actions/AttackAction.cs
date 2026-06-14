using UnityEngine;

/// <summary>
/// ATTACK: run at player with avoidance, stop in range, play attack anim, and damage PlayerHealth.
/// Physics-based (EnemyMovement), no NavMesh.
/// </summary>
public class AttackAction : CombatAction
{
    [Header("References")]
    public Transform player;
    public MonsterPerception perception;
    public CoreEnemy core;
    public EnemyMovement movement;
    public EnemyAnimatorController anim;
    public EnemySoundController sfx;

    [Header("Chase / Steering")]
    public float desiredSpeed = 3.2f;                 // sprint
    [Range(0.05f, 1f)] public float steerResponsiveness = 0.4f;
    public float thinkInterval = 0.18f;
    public bool avoidWallsDuringChase = true;
    public float avoidThreshold = 1.35f;
    public float hallwaySideDistance = 1.2f;
    [Range(0f, 1f)] public float hallwayCenterBias = 0.35f;
    public float faceTurnSpeed = 8f;                  // during windup/hit

    [Header("Small Obstacle Jump (optional)")]
    public bool allowSmallObstacleJump = true;
    public float smallObstacleThreshold = 1.0f;
    public float maxJumpHeight = 1.25f;
    public EnemyJump jumper;

    [Header("Attack")]
    public float stopDistance = 1.6f;                 // where we stop before attacking
    public float attackRange = 1.9f;                  // max range to land hit
    public bool requireClearLOS = true;               // don't hit through walls
    public LayerMask losMask = ~0;                    // blockers for LOS ray

    public int damage = 20;                           // damage dealt to PlayerHealth
    public float attackWindup = 0.25f;
    public float attackHitWindow = 0.20f;
    public float attackCooldown = 0.8f;

    [Header("Animator / SFX")]
    public string attackTrigger = "attack";
    public AudioClip attackGrowl;
    public AudioClip attackSwing;

    // internals
    private float nextThinkTime;
    private Vector3 desiredDir;

    private enum AttackPhase { Chase, Windup, HitWindow, Cooldown }
    private AttackPhase phase = AttackPhase.Chase;
    private float phaseUntilTime;
    private bool hitApplied;

    // Perception ray indices (MonsterPerception layout)
    private const int R_FWD = 0;
    private const int R_RIGHT = 2;
    private const int R_LEFT = 6;
    private const int R_FWD_DOWN = 8;
    private const int R_FWD_UP = 12;
    private const int R_UP = 17;

    private float oldSpeed;

    protected override void Awake()
    {
        base.Awake();
        if (!core) core = GetComponentInParent<CoreEnemy>();
        if (!movement) movement = GetComponentInParent<EnemyMovement>();
        if (!anim) anim = GetComponentInParent<EnemyAnimatorController>();
        if (!sfx) sfx = GetComponentInParent<EnemySoundController>();
        if (!perception) perception = GetComponentInParent<MonsterPerception>();
        if (!player && perception) player = perception.player;
        if (!jumper) jumper = GetComponentInParent<EnemyJump>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        desiredDir = Flatten(core.transform.forward);
        phase = AttackPhase.Chase;
        nextThinkTime = Time.time;
        phaseUntilTime = 0f;
        anim?.PlayWalk(true);
        hitApplied = false;
        oldSpeed = movement.moveSpeed;
        movement.moveSpeed = desiredSpeed;
    }

    public override void PerformAction()
    {
        if (!core || !movement || !player) return;

        // refresh steering periodically (not during locked hit window)
        if (Time.time >= nextThinkTime && phase != AttackPhase.HitWindow)
        {
            desiredDir = ComputeSteerDirTowardPlayer();
            nextThinkTime = Time.time + Mathf.Max(0.06f, thinkInterval);
        }

        float dist = HorizontalDistance(core.transform.position, player.position);

        switch (phase)
        {
            case AttackPhase.Chase:
                {
                    // move toward player until within stopDistance
                    if (dist > stopDistance)
                    {
                        anim?.PlayWalk(true);
                        movement.SetMoveDirection(desiredDir.normalized * desiredSpeed);
                    }
                    else
                    {
                        // within striking distance → start windup
                        EnterPhase(AttackPhase.Windup, attackWindup);
                        anim?.PlayTrigger(attackTrigger);
                        if (attackGrowl && sfx != null) sfx.PlayOneShot(attackGrowl);
                    }
                    break;
                }

            case AttackPhase.Windup:
                {
                    // slow/hold position & face the player
                    FacePlayer(faceTurnSpeed);
                    movement.SetMoveDirection(Vector3.zero);

                    if (Time.time >= phaseUntilTime)
                    {
                        EnterPhase(AttackPhase.HitWindow, attackHitWindow);
                        hitApplied = false;
                        if (attackSwing && sfx != null) sfx.PlayOneShot(attackSwing);
                    }
                    break;
                }

            case AttackPhase.HitWindow:
                {
                    // lock in place & face player; try to apply hit once
                    FacePlayer(faceTurnSpeed * 1.25f);
                    movement.SetMoveDirection(Vector3.zero);

                    if (!hitApplied)
                    {
                        TryApplyHit();
                        hitApplied = true;
                    }

                    if (Time.time >= phaseUntilTime)
                    {
                        EnterPhase(AttackPhase.Cooldown, attackCooldown);
                    }
                    break;
                }

            case AttackPhase.Cooldown:
                {
                    // short recovery; then resume chase
                    if (Time.time >= phaseUntilTime)
                    {
                        EnterPhase(AttackPhase.Chase, 0f);
                    }
                    else
                    {
                        // light forward pressure to follow target drift
                        movement.SetMoveDirection(desiredDir.normalized * (desiredSpeed * 0.6f));
                        FacePlayer(faceTurnSpeed * 0.8f);
                    }
                    break;
                }
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        movement?.SetMoveDirection(Vector3.zero);
        anim?.PlayIdle();
        phase = AttackPhase.Chase;
        hitApplied = false;

        movement.moveSpeed = oldSpeed;
    }

    // ---------------- Steering & avoidance ----------------
    private Vector3 ComputeSteerDirTowardPlayer()
    {
        Vector3 forward = Flatten(core.transform.forward);
        Vector3 right = Flatten(core.transform.right);

        Vector3 intent = Flatten(player.position - core.transform.position);
        if (intent.sqrMagnitude < 0.0001f) intent = forward;

        if (!avoidWallsDuringChase || perception == null || perception.RayDistances == null || perception.RayDistances.Length < 18)
            return intent;

        var d = perception.RayDistances;
        float fwdDist = d[R_FWD];
        float leftDist = d[R_LEFT];
        float rightDist = d[R_RIGHT];

        // Hallway centering
        bool leftClose = leftDist <= hallwaySideDistance;
        bool rightClose = rightDist <= hallwaySideDistance;

        Vector3 desired = intent;
        if (leftClose && rightClose)
        {
            float sideBias = Mathf.Clamp((rightDist - leftDist), -1f, 1f);
            desired = (intent + right * sideBias * hallwayCenterBias).normalized;
        }

        // Forward avoidance & side choice
        if (fwdDist <= avoidThreshold)
        {
            desired = (rightDist > leftDist)
                ? Vector3.Slerp(desired, right, steerResponsiveness).normalized
                : Vector3.Slerp(desired, -right, steerResponsiveness).normalized;

            if (allowSmallObstacleJump) TrySmallObstacleJump(d);
        }

        // Smooth steer from current desiredDir toward desired
        Vector3 from = (desiredDir.sqrMagnitude < 0.0001f) ? intent : desiredDir;
        return Vector3.Slerp(from, desired, Mathf.Clamp01(steerResponsiveness)).normalized;
    }

    private void TrySmallObstacleJump(float[] d)
    {
        if (!jumper) return;

        float front = d[R_FWD];
        if (front > smallObstacleThreshold) return;

        float headroomFwd = d[R_FWD_UP];
        float headroomUp = d[R_UP];
        float groundAhead = d[R_FWD_DOWN];

        bool hasHeadroom = (headroomFwd > maxJumpHeight && headroomUp > maxJumpHeight * 0.75f);
        bool groundSoon = groundAhead < (perception.rayRange * 0.75f);

        if (hasHeadroom && groundSoon) jumper.Jump();
    }

    private void FacePlayer(float turnSpeed)
    {
        if (!core || core.rb == null || !player) return;
        Vector3 to = player.position - core.transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(to.normalized);
        Quaternion rot = Quaternion.Slerp(core.rb.rotation, target, Time.deltaTime * Mathf.Max(1f, turnSpeed));
        core.rb.MoveRotation(rot);
    }

    // ---------------- Attack / damage ----------------
    private void TryApplyHit()
    {
        // Ensure in range
        float dist = HorizontalDistance(core.transform.position, player.position);
        if (dist > attackRange) return;

        // Optional LOS check
        if (requireClearLOS)
        {
            Vector3 origin = core.transform.position + Vector3.up * 0.9f;
            Vector3 target = player.position + Vector3.up * 0.9f;
            Vector3 dir = (target - origin);
            float len = dir.magnitude;
            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, len, losMask, QueryTriggerInteraction.Ignore))
            {
                // blocked before reaching player
                if (hit.transform != player && hit.transform.root != player)
                    return;
            }
        }

        // Apply damage via PlayerHealth on the player
        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage, core.transform.position); // <-- Ensure your PlayerHealth exposes this
        }
        else
        {
            // Fallback: if you had an older system, you can add a backup here.
            // player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void EnterPhase(AttackPhase p, float duration)
    {
        phase = p;
        phaseUntilTime = Time.time + Mathf.Max(0f, duration);
    }

    // ---------------- Utils ----------------
    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
    }
}
