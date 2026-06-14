using UnityEngine;

/// <summary>
/// Attack using NavMesh: sprint to melee range, stop, play attack anim, and damage the player.
/// </summary>
public class AttackNavAction : CombatAction
{
    [Header("Refs")]
    public Transform player;
    public EnemyNavMovement navMove;
    public EnemyAnimatorController anim;
    public EnemySoundController sfx;

    [Header("Chase")]
    public float sprintSpeed = 4.2f;
    public float replanInterval = 0.18f;
    public float attackRange = 1.8f;       // total reach
    public float stopBuffer = 0.15f;       // stop a bit before the range

    [Header("Attack")]
    public string attackTrigger = "attack";
    public float attackWindup = 0.22f;
    public float attackHitWindow = 0.18f;
    public float attackCooldown = 0.8f;
    public int damage = 20;
    public bool requireClearLOS = true;
    public LayerMask losMask = ~0;

    [Header("SFX")]
    public AudioClip windupSfx;
    public AudioClip swingSfx;

    private float _nextPlanAt;
    private AttackPhase _phase = AttackPhase.Chase;
    private float _phaseUntil;
    private bool _hitApplied;

    private enum AttackPhase { Chase, Windup, HitWindow, Cooldown }

    protected override void Awake()
    {
        base.Awake();
        if (!navMove) navMove = GetComponentInParent<EnemyNavMovement>();
        if (!anim) anim = GetComponentInParent<EnemyAnimatorController>();
        if (!sfx) sfx = GetComponentInParent<EnemySoundController>();
        if (!player && Camera.main) player = Camera.main.transform;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        if (!player || !navMove) return;

        navMove.SetMoveSpeed(sprintSpeed);
        navMove.SetStoppingDistance(Mathf.Max(0f, attackRange - stopBuffer));
        _nextPlanAt = 0f;
        _phase = AttackPhase.Chase;
        _phaseUntil = 0f;
        _hitApplied = false;

        anim?.PlayWalk(true);
    }

    public override void PerformAction()
    {
        if (!player || !navMove) return;

        switch (_phase)
        {
            case AttackPhase.Chase:
                ChaseTick();
                // Enter windup once we’ve reached melee stop
                if (navMove.ReachedDestination(0.05f))
                {
                    EnterPhase(AttackPhase.Windup, attackWindup);
                    anim?.PlayTrigger(attackTrigger);
                    if (windupSfx && sfx != null) sfx.PlayOneShot(windupSfx);
                    navMove.Stop(); // hold position for the strike
                }
                break;

            case AttackPhase.Windup:
                FacePlayer(navMove.Agent.angularSpeed);
                if (Time.time >= _phaseUntil)
                {
                    EnterPhase(AttackPhase.HitWindow, attackHitWindow);
                    _hitApplied = false;
                    if (swingSfx && sfx != null) sfx.PlayOneShot(swingSfx);
                }
                break;

            case AttackPhase.HitWindow:
                FacePlayer(navMove.Agent.angularSpeed * 1.25f);
                if (!_hitApplied)
                {
                    TryApplyHit();
                    _hitApplied = true;
                }
                if (Time.time >= _phaseUntil)
                {
                    EnterPhase(AttackPhase.Cooldown, attackCooldown);
                }
                break;

            case AttackPhase.Cooldown:
                // small pause then resume chase (player may have moved)
                if (Time.time >= _phaseUntil)
                {
                    EnterPhase(AttackPhase.Chase, 0f);
                    navMove.SetMoveSpeed(sprintSpeed);
                }
                break;
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        navMove?.Stop();
        anim?.PlayIdle();
        _phase = AttackPhase.Chase;
        _hitApplied = false;
    }

    // ---------- Helpers ----------
    private void ChaseTick()
    {
        anim?.PlayWalk(true);
        if (Time.time >= _nextPlanAt)
        {
            // keep pathing to the player
            if (NavUtils.Sample(player.position, out var onMesh, 2f)) navMove.SetDestination(onMesh);
            else navMove.SetDestination(player.position);

            _nextPlanAt = Time.time + replanInterval;
        }
    }

    private void FacePlayer(float angularSpeed)
    {
        Vector3 to = player.position - navMove.transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(to.normalized);
        navMove.transform.rotation =
            Quaternion.RotateTowards(navMove.transform.rotation, target, angularSpeed * Time.deltaTime);
    }

    private void TryApplyHit()
    {
        // distance check
        Vector3 a = navMove.transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        if (Vector3.Distance(a, b) > attackRange) return;

        // optional LOS check
        if (requireClearLOS)
        {
            Vector3 origin = navMove.transform.position + Vector3.up * 0.9f;
            Vector3 target = player.position + Vector3.up * 0.9f;
            Vector3 dir = (target - origin);
            float len = dir.magnitude;
            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, len, losMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != player && hit.transform.root != player) return;
            }
        }

        // Apply damage on PlayerHealth (your directional overload)
        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage, navMove.transform.position);
        }
    }

    private void EnterPhase(AttackPhase p, float duration)
    {
        _phase = p;
        _phaseUntil = Time.time + Mathf.Max(0f, duration);
    }
}
