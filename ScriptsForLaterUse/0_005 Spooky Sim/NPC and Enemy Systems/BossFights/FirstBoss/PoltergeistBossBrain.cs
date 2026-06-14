using Cherry.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Bosses/Poltergeist Boss Brain")]
[RequireComponent(typeof(BeamEnemy))]
[RequireComponent(typeof(NavMeshAgent))]
public class PoltergeistBossBrain : CombatStateMachine
{

    [Header("References")]
    public BeamEnemy BeamEnemy;
    public NavMeshAgent Agent;
    public Animator Animator;
    public AudioSource AudioSource;
    public Transform Player;
    public string PlayerTag = "Player";

    [Header("Boss Defeat Events")]
    public UnityEvent OnBossDefeated;
    [SerializeField] private bool invokeDefeatEventOnDeathStateEnter = true;
    private bool _bossDefeatEventInvoked;

    [Header("Waypoints")]
    public BossWaypoint[] Waypoints;
    public bool AvoidImmediateRepeat = true;
    [Min(0f)] public float MinDashRelocationDistance = 5f;

    [Header("Movement")]
    [Min(0.1f)] public float RunSpeed = 4.5f;
    [Min(0.1f)] public float FlySpeed = 6f;
    [Min(0.1f)] public float DashSpeed = 12f;
    [Min(0.1f)] public float AttackRushSpeed = 10f;
    [Min(0.1f)] public float TurnSpeed = 720f;

    [Header("Wait")]
    [Min(0f)] public float WaitAtWaypointMin = 0.25f;
    [Min(0f)] public float WaitAtWaypointMax = 0.8f;

    [Header("Attack")]
    [Min(0.05f)] public float AttackCheckInterval = 1f;
    [Range(0f, 1f)] public float AttackChancePerCheck = 0.3f;
    [Min(0f)] public float AttackCooldown = 2.5f;
    [Min(0f)] public float AttackWindupDuration = 0.35f;
    [Min(0.1f)] public float AttackRushDuration = 1.15f;
    [Min(0.1f)] public float AttackStopDistance = 1.4f;

    [Header("Attack Damage")]
    public bool DealContactDamage = true;
    [Min(0f)] public float ContactDamage = 10f;
    [Min(0.1f)] public float ContactDamageRadius = 1.25f;

    [Header("Hit Reaction")]
    [Min(0f)] public float HitStunDuration = 1f;
    [Min(0f)] public float HitReactionCooldown = 2f;
    public AudioClip HitClip;
    public string HitTrigger = "Hit";

    [Header("Death")]
    public AudioClip DeathClip;
    public string DeathTrigger = "Die";
    public bool DestroyAfterDeath = true;
    [Min(0f)] public float DestroyDelay = 2f;

    [Header("Animation")]
    public string MoveSpeedFloat = "MoveSpeed";
    public string IsFlyingBool = "IsFlying";
    public string AttackTrigger = "Attack";

    [Header("Debug Runtime")]
    public BossWaypoint CurrentWaypoint;
    public bool ManualFlying;
    public bool ManualDashing;
    public bool ArrivedAtWaypoint;
    public bool AttackRushFinished;
    public float WaitEndTime;
    public float AttackWindupEndTime;
    public float AttackRushEndTime;
    public float StunEndTime;

    protected Vector3 _manualTarget;
    protected BossWaypoint _lastWaypoint;
    protected float _nextAttackCheckTime;
    protected float _nextAttackAllowedTime;
    protected float _nextHitReactionAllowedTime;
    protected bool _didDamagePlayerThisAttack;
    protected bool _pendingHitReaction;
    protected bool _pendingDeath;

    protected override void Awake()
    {
        BeamEnemy = GetComponent<BeamEnemy>();
        Agent = GetComponent<NavMeshAgent>();
        if (Animator == null) Animator = GetComponentInChildren<Animator>();
        if (AudioSource == null) AudioSource = GetComponent<AudioSource>();

        FindPlayerIfNeeded();

        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (BeamEnemy != null)
        {
            BeamEnemy.BeamDamaged += OnBeamDamaged;
            BeamEnemy.BeamKilled += OnBeamKilled;
        }
    }

    protected virtual void OnDisable()
    {
        if (BeamEnemy != null)
        {
            BeamEnemy.BeamDamaged -= OnBeamDamaged;
            BeamEnemy.BeamKilled -= OnBeamKilled;
        }
    }
    public override void ResetBrain()
    {
        _pendingHitReaction = false;
        _pendingDeath = false;
        _bossDefeatEventInvoked = false;
        ArrivedAtWaypoint = false;
        AttackRushFinished = false;
        ManualFlying = false;
        ManualDashing = false;
        _nextAttackCheckTime = Time.time + AttackCheckInterval;

        base.ResetBrain();
    }
    public void FindPlayerIfNeeded()
    {
        if (Player != null) return;

        GameObject found = GameObject.FindGameObjectWithTag(PlayerTag);
        if (found != null) Player = found.transform;
    }

    protected virtual void OnBeamDamaged(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (BeamEnemy != null && BeamEnemy.IsDead) return;

        if (Time.time < _nextHitReactionAllowedTime)
            return;

        _nextHitReactionAllowedTime = Time.time + HitReactionCooldown;
        _pendingHitReaction = true;
    }

    protected virtual void OnBeamKilled(BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        _pendingDeath = true;
    }

    public bool ConsumePendingHitReaction()
    {
        if (!_pendingHitReaction) return false;
        _pendingHitReaction = false;
        return true;
    }

    public bool ConsumePendingDeath()
    {
        if (!_pendingDeath) return false;
        _pendingDeath = false;
        return true;
    }

    public bool RollForAttackStart()
    {
        FindPlayerIfNeeded();
        if (Player == null) return false;

        if (Time.time < _nextAttackAllowedTime) return false;
        if (Time.time < _nextAttackCheckTime) return false;

        _nextAttackCheckTime = Time.time + AttackCheckInterval;

        if (Random.value <= AttackChancePerCheck)
        {
            _nextAttackAllowedTime = Time.time + AttackCooldown;
            return true;
        }

        return false;
    }

    public void PickNextWaypoint(bool preferFar)
    {
        if (Waypoints == null || Waypoints.Length == 0)
        {
            CurrentWaypoint = null;
            return;
        }

        BossWaypoint chosen = null;

        for (int i = 0; i < 16; i++)
        {
            BossWaypoint candidate = Waypoints[Random.Range(0, Waypoints.Length)];
            if (candidate == null) continue;

            if (AvoidImmediateRepeat && candidate == CurrentWaypoint)
                continue;

            if (preferFar)
            {
                float dist = Vector3.Distance(transform.position, candidate.transform.position);
                if (dist < MinDashRelocationDistance)
                    continue;
            }

            chosen = candidate;
            break;
        }

        if (chosen == null)
            chosen = Waypoints[Random.Range(0, Waypoints.Length)];

        _lastWaypoint = CurrentWaypoint;
        CurrentWaypoint = chosen;
    }

    public void BeginMoveToWaypoint(bool chooseNewWaypoint, bool preferFar, bool dash)
    {
        ArrivedAtWaypoint = false;

        if (chooseNewWaypoint || CurrentWaypoint == null)
            PickNextWaypoint(preferFar);

        if (CurrentWaypoint == null)
            return;

        ManualDashing = dash;
        ManualFlying = CurrentWaypoint.moveMode == BossWaypointMoveMode.Fly;

        if (ManualFlying)
        {
            DisableAgent();
            _manualTarget = CurrentWaypoint.transform.position;
        }
        else
        {
            EnableAgentAndMove(CurrentWaypoint.transform.position, dash ? DashSpeed : RunSpeed, CurrentWaypoint.arrivalRadius);
        }
    }

    public void TickMoveToWaypoint()
    {
        if (CurrentWaypoint == null)
        {
            ArrivedAtWaypoint = true;
            return;
        }

        if (ManualFlying)
        {
            float speed = ManualDashing ? DashSpeed : FlySpeed;
            MoveManuallyTowards(_manualTarget, speed);

            if (Vector3.Distance(transform.position, _manualTarget) <= CurrentWaypoint.arrivalRadius)
                ArrivedAtWaypoint = true;

            return;
        }

        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh)
        {
            ArrivedAtWaypoint = true;
            return;
        }

        if (!Agent.pathPending &&
            Agent.remainingDistance <= Mathf.Max(Agent.stoppingDistance, CurrentWaypoint.arrivalRadius))
        {
            ArrivedAtWaypoint = true;
        }
    }

    public void BeginWait()
    {
        StopMovement();
        ArrivedAtWaypoint = false;
        WaitEndTime = Time.time + Random.Range(WaitAtWaypointMin, WaitAtWaypointMax);
    }

    public bool IsWaitFinished()
    {
        return Time.time >= WaitEndTime;
    }

    public void BeginAttackWindup()
    {
        StopMovement();
        DisableAgent();

        AttackWindupEndTime = Time.time + AttackWindupDuration;

        if (Animator != null && !string.IsNullOrWhiteSpace(AttackTrigger))
            Animator.SetTrigger(AttackTrigger);
    }

    public bool IsAttackWindupFinished()
    {
        return Time.time >= AttackWindupEndTime;
    }

    public void BeginAttackRush()
    {
        DisableAgent();
        ManualFlying = true;
        ManualDashing = true;
        _didDamagePlayerThisAttack = false;
        AttackRushFinished = false;
        AttackRushEndTime = Time.time + AttackRushDuration;
    }

    public void TickAttackRush()
    {
        FindPlayerIfNeeded();

        if (Player == null)
        {
            AttackRushFinished = true;
            return;
        }

        MoveManuallyTowards(Player.position, AttackRushSpeed);

        if (DealContactDamage && !_didDamagePlayerThisAttack)
        {
            float dist = Vector3.Distance(transform.position, Player.position);
            if (dist <= ContactDamageRadius)
            {
                _didDamagePlayerThisAttack = true;

                var playerHealth = Player.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(ContactDamage, gameObject);
                }
            }
        }

        if (Vector3.Distance(transform.position, Player.position) <= AttackStopDistance)
        {
            AttackRushFinished = true;
            return;
        }

        if (Time.time >= AttackRushEndTime)
        {
            AttackRushFinished = true;
        }
    }

    public void BeginStunned()
    {
        StopMovement();
        DisableAgent();

        ManualFlying = false;
        ManualDashing = false;
        StunEndTime = Time.time + HitStunDuration;

        if (Animator != null && !string.IsNullOrWhiteSpace(HitTrigger))
            Animator.SetTrigger(HitTrigger);

        if (AudioSource != null && HitClip != null)
            AudioSource.PlayOneShot(HitClip);
    }

    public bool IsStunFinished()
    {
        return Time.time >= StunEndTime;
    }

    public void BeginDead()
    {
        StopMovement();
        DisableAgent();
        ManualFlying = false;
        ManualDashing = false;

        if (Animator != null && !string.IsNullOrWhiteSpace(DeathTrigger))
            Animator.SetTrigger(DeathTrigger);

        if (AudioSource != null && DeathClip != null)
            AudioSource.PlayOneShot(DeathClip);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (invokeDefeatEventOnDeathStateEnter && !_bossDefeatEventInvoked)
        {
            _bossDefeatEventInvoked = true;
            OnBossDefeated?.Invoke();
        }

        if (DestroyAfterDeath)
            Destroy(gameObject, DestroyDelay);
    }

    public void UpdateAnimatorMotion()
    {
        if (Animator == null) return;

        if (!string.IsNullOrWhiteSpace(IsFlyingBool))
        {
            Animator.SetBool(IsFlyingBool, ManualFlying);
        }

        if (!string.IsNullOrWhiteSpace(MoveSpeedFloat))
        {
            float speedValue = 0f;

            if (CurrentState != null)
            {
                string stateName = CurrentState.StateName;

                if (stateName == "Move")
                    speedValue = ManualDashing ? 1f : 0.65f;
                else if (stateName == "Relocate")
                    speedValue = 1f;
                else if (stateName == "AttackRush")
                    speedValue = 1f;
            }

            Animator.SetFloat(MoveSpeedFloat, speedValue);
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateAnimatorMotion();
    }

    protected void EnableAgentAndMove(Vector3 destination, float speed, float arrivalRadius)
    {
        if (Agent == null) return;

        if (!EnsureAgentOnNavMesh())
        {
            DisableAgent();
            ManualFlying = true;
            _manualTarget = destination;
            return;
        }

        ManualFlying = false;

        Agent.speed = speed;
        Agent.acceleration = Mathf.Max(speed * 4f, 8f);
        Agent.angularSpeed = TurnSpeed;
        Agent.stoppingDistance = Mathf.Max(0.05f, arrivalRadius * 0.5f);
        Agent.isStopped = false;

        Vector3 finalDestination = destination;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            finalDestination = hit.position;

        Agent.SetDestination(finalDestination);
    }

    protected bool EnsureAgentOnNavMesh()
    {
        if (Agent == null) return false;

        if (!Agent.enabled)
            Agent.enabled = true;

        if (Agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            bool warped = Agent.Warp(hit.position);
            if (warped && Agent.isOnNavMesh)
                return true;
        }

        return false;
    }

    public void DisableAgent()
    {
        if (Agent == null) return;

        if (Agent.enabled)
        {
            Agent.ResetPath();
            Agent.isStopped = true;
            Agent.enabled = false;
        }
    }

    public void StopMovement()
    {
        if (Agent != null && Agent.enabled)
        {
            Agent.ResetPath();
            Agent.isStopped = true;
        }
    }

    public void MoveManuallyTowards(Vector3 worldTarget, float speed)
    {
        Vector3 toTarget = worldTarget - transform.position;
        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, TurnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, worldTarget, speed * Time.deltaTime);
    }
}