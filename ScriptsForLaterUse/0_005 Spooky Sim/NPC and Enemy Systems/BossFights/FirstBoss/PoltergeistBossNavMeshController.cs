using Cherry.Combat;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Cherry/Bosses/Poltergeist Boss NavMesh Controller")]
[RequireComponent(typeof(BeamEnemy))]
[RequireComponent(typeof(NavMeshAgent))]
public class PoltergeistBossNavMeshController : MonoBehaviour
{
    private enum BossState
    {
        MovingToWaypoint,
        Waiting,
        AttackWindup,
        AttackingPlayer,
        Stunned,
        Dead
    }

    [Header("References")]
    [SerializeField] private BeamEnemy beamEnemy;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Waypoints")]
    [SerializeField] private BossWaypoint[] waypoints;
    [SerializeField] private bool avoidImmediateRepeat = true;
    [Min(0f)][SerializeField] private float minDashRelocationDistance = 5f;

    [Header("Movement")]
    [Min(0.1f)][SerializeField] private float runSpeed = 4.5f;
    [Min(0.1f)][SerializeField] private float flySpeed = 6f;
    [Min(0.1f)][SerializeField] private float dashSpeed = 12f;
    [Min(0.1f)][SerializeField] private float attackRushSpeed = 10f;
    [Min(0.1f)][SerializeField] private float manualTurnSpeed = 720f;

    [Header("Timing")]
    [Min(0f)][SerializeField] private float waitAtWaypointMin = 0.25f;
    [Min(0f)][SerializeField] private float waitAtWaypointMax = 0.8f;

    [Header("Attack")]
    [Min(0.05f)][SerializeField] private float attackCheckInterval = 1f;
    [Range(0f, 1f)][SerializeField] private float attackChancePerCheck = 0.3f;
    [Min(0f)][SerializeField] private float attackCooldown = 2.5f;
    [Min(0f)][SerializeField] private float attackWindupDuration = 0.35f;
    [Min(0.1f)][SerializeField] private float attackRushDuration = 1.15f;
    [Min(0.1f)][SerializeField] private float attackStopDistance = 1.4f;

    [Header("Attack Damage")]
    [SerializeField] private bool dealContactDamage = true;
    [Min(0f)][SerializeField] private float contactDamage = 10f;
    [Min(0.1f)][SerializeField] private float contactDamageRadius = 1.25f;
    [SerializeField] private string playerDamageMessage = "TakeDamage";

    [Header("Hit Reaction")]
    [Min(0f)][SerializeField] private float hitStunDuration = 1f;
    [Min(0f)][SerializeField] private float hitReactionCooldown = 2f;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private string hitTrigger = "Hit";

    [Header("Death")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private bool destroyAfterDeath = true;
    [Min(0f)][SerializeField] private float destroyDelay = 2f;

    [Header("Animation")]
    [SerializeField] private string moveSpeedFloat = "MoveSpeed";
    [SerializeField] private string isFlyingBool = "IsFlying";
    [SerializeField] private string attackTrigger = "Attack";

    private BossState _state = BossState.MovingToWaypoint;
    private BossWaypoint _currentWaypoint;
    private BossWaypoint _lastWaypoint;
    private Vector3 _manualTarget;
    private bool _manualFlying;
    private bool _manualDashing;
    private float _waitTimer;
    private float _nextAttackCheckTime;
    private float _nextAttackAllowedTime;
    private float _nextHitReactionAllowedTime;
    private float _attackEndTime;
    private bool _didDamagePlayerThisAttack;
    private bool _dead;
    private Coroutine _attackRoutine;
    private Coroutine _hitRoutine;

    public bool IsDead => _dead;
    public bool IsStunned => _state == BossState.Stunned;

    private void Reset()
    {
        beamEnemy = GetComponent<BeamEnemy>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (beamEnemy == null) beamEnemy = GetComponent<BeamEnemy>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (agent != null)
        {
            agent.autoBraking = true;
            agent.updateRotation = true;
        }
    }

    private void OnEnable()
    {
        if (beamEnemy != null)
        {
            beamEnemy.BeamDamaged += OnBeamDamaged;
            beamEnemy.BeamKilled += OnBeamKilled;
        }
    }

    private void OnDisable()
    {
        if (beamEnemy != null)
        {
            beamEnemy.BeamDamaged -= OnBeamDamaged;
            beamEnemy.BeamKilled -= OnBeamKilled;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        _nextAttackCheckTime = Time.time + attackCheckInterval;

        BossWaypoint first = PickNextWaypoint(false);
        if (first != null)
            BeginMoveToWaypoint(first, false);
        else
            _state = BossState.Waiting;
    }

    private void Update()
    {
        if (_dead) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        switch (_state)
        {
            case BossState.MovingToWaypoint:
                TickMoveToWaypoint();
                TickAttackChecks();
                break;

            case BossState.Waiting:
                TickWaiting();
                TickAttackChecks();
                break;

            case BossState.AttackWindup:
                // handled by coroutine
                break;

            case BossState.AttackingPlayer:
                TickAttackRush();
                break;

            case BossState.Stunned:
                // frozen during stun
                break;
        }

        UpdateAnimator();
    }

    private void TickWaiting()
    {
        _waitTimer -= Time.deltaTime;
        if (_waitTimer <= 0f)
        {
            BossWaypoint next = PickNextWaypoint(false);
            if (next != null)
                BeginMoveToWaypoint(next, false);
        }
    }

    private void TickMoveToWaypoint()
    {
        if (_currentWaypoint == null)
        {
            _state = BossState.Waiting;
            _waitTimer = Random.Range(waitAtWaypointMin, waitAtWaypointMax);
            return;
        }

        if (_manualFlying)
        {
            float speed = _manualDashing ? dashSpeed : flySpeed;
            MoveManuallyTowards(_manualTarget, speed);

            if (Vector3.Distance(transform.position, _manualTarget) <= _currentWaypoint.arrivalRadius)
            {
                ArriveAtWaypoint();
            }
        }
        else
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                BossWaypoint next = PickNextWaypoint(false);
                if (next != null)
                    BeginMoveToWaypoint(next, false);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, _currentWaypoint.arrivalRadius))
            {
                ArriveAtWaypoint();
            }
        }
    }

    private void TickAttackChecks()
    {
        if (player == null) return;
        if (Time.time < _nextAttackCheckTime) return;
        if (Time.time < _nextAttackAllowedTime) return;

        _nextAttackCheckTime = Time.time + attackCheckInterval;

        if (Random.value <= attackChancePerCheck)
        {
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        _state = BossState.AttackWindup;
        _nextAttackAllowedTime = Time.time + attackCooldown;

        StopMovement();

        if (animator != null && !string.IsNullOrWhiteSpace(attackTrigger))
            animator.SetTrigger(attackTrigger);

        yield return new WaitForSeconds(attackWindupDuration);

        if (_dead || _state == BossState.Stunned)
            yield break;

        _didDamagePlayerThisAttack = false;
        _attackEndTime = Time.time + attackRushDuration;
        _state = BossState.AttackingPlayer;
    }

    private void TickAttackRush()
    {
        if (player == null)
        {
            ExitAttackAndRelocate();
            return;
        }

        MoveManuallyTowards(player.position, attackRushSpeed);

        if (dealContactDamage && !_didDamagePlayerThisAttack)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= contactDamageRadius)
            {
                _didDamagePlayerThisAttack = true;

                var playerHealth = player.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage, gameObject);
                }
            }
        }

        if (Vector3.Distance(transform.position, player.position) <= attackStopDistance || Time.time >= _attackEndTime)
        {
            ExitAttackAndRelocate();
        }
    }

    private void ExitAttackAndRelocate()
    {
        BossWaypoint next = PickNextWaypoint(true);
        if (next != null)
            BeginMoveToWaypoint(next, true);
        else
        {
            _state = BossState.Waiting;
            _waitTimer = Random.Range(waitAtWaypointMin, waitAtWaypointMax);
        }
    }

    private void ArriveAtWaypoint()
    {
        StopMovement();
        _state = BossState.Waiting;
        _waitTimer = Random.Range(waitAtWaypointMin, waitAtWaypointMax);
    }

    private void BeginMoveToWaypoint(BossWaypoint waypoint, bool dash)
    {
        if (waypoint == null) return;

        _currentWaypoint = waypoint;
        _manualDashing = dash;
        _manualFlying = waypoint.moveMode == BossWaypointMoveMode.Fly;
        _state = BossState.MovingToWaypoint;

        if (_manualFlying)
        {
            DisableAgent();
            _manualTarget = waypoint.transform.position;
        }
        else
        {
            EnableAgentAndMove(waypoint.transform.position, dash ? dashSpeed : runSpeed, waypoint.arrivalRadius);
        }
    }

    private void EnableAgentAndMove(Vector3 destination, float speed, float arrivalRadius)
    {
        if (agent == null) return;

        if (!EnsureAgentOnNavMesh())
        {
            DisableAgent();
            _manualFlying = true;
            _manualTarget = destination;
            return;
        }

        agent.speed = speed;
        agent.acceleration = Mathf.Max(speed * 4f, 8f);
        agent.angularSpeed = manualTurnSpeed;
        agent.stoppingDistance = Mathf.Max(0.05f, arrivalRadius * 0.5f);
        agent.isStopped = false;

        Vector3 finalDestination = destination;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            finalDestination = hit.position;

        agent.SetDestination(finalDestination);
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null) return false;

        if (!agent.enabled)
            agent.enabled = true;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            bool warped = agent.Warp(hit.position);
            if (warped && agent.isOnNavMesh)
                return true;
        }

        return false;
    }

    private void DisableAgent()
    {
        if (agent == null) return;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.enabled = false;
        }
    }

    private void StopMovement()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private void MoveManuallyTowards(Vector3 worldTarget, float speed)
    {
        Vector3 toTarget = worldTarget - transform.position;
        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, manualTurnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, worldTarget, speed * Time.deltaTime);
    }

    private BossWaypoint PickNextWaypoint(bool preferFar)
    {
        if (waypoints == null || waypoints.Length == 0)
            return null;

        BossWaypoint chosen = null;

        for (int i = 0; i < 16; i++)
        {
            BossWaypoint candidate = waypoints[Random.Range(0, waypoints.Length)];
            if (candidate == null) continue;

            if (avoidImmediateRepeat && candidate == _currentWaypoint)
                continue;

            if (preferFar)
            {
                float dist = Vector3.Distance(transform.position, candidate.transform.position);
                if (dist < minDashRelocationDistance)
                    continue;
            }

            chosen = candidate;
            break;
        }

        if (chosen == null)
            chosen = waypoints[Random.Range(0, waypoints.Length)];

        _lastWaypoint = _currentWaypoint;
        _currentWaypoint = chosen;
        return chosen;
    }

    private void OnBeamDamaged(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (_dead) return;
        if (Time.time < _nextHitReactionAllowedTime) return;

        _nextHitReactionAllowedTime = Time.time + hitReactionCooldown;

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);

        _hitRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        StopMovement();
        DisableAgent();

        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _state = BossState.Stunned;

        if (animator != null && !string.IsNullOrWhiteSpace(hitTrigger))
            animator.SetTrigger(hitTrigger);

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        yield return new WaitForSeconds(hitStunDuration);

        if (_dead) yield break;

        BossWaypoint next = PickNextWaypoint(true);
        if (next != null)
            BeginMoveToWaypoint(next, true);
        else
        {
            _state = BossState.Waiting;
            _waitTimer = Random.Range(waitAtWaypointMin, waitAtWaypointMax);
        }

        _hitRoutine = null;
    }

    private void OnBeamKilled(BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (_dead) return;
        _dead = true;

        StopAllCoroutines();
        StopMovement();
        DisableAgent();
        _state = BossState.Dead;

        if (animator != null && !string.IsNullOrWhiteSpace(deathTrigger))
            animator.SetTrigger(deathTrigger);

        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (destroyAfterDeath)
            Destroy(gameObject, destroyDelay);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (!string.IsNullOrWhiteSpace(isFlyingBool))
        {
            bool isFlyingNow = _manualFlying || _state == BossState.AttackingPlayer;
            animator.SetBool(isFlyingBool, isFlyingNow);
        }

        if (!string.IsNullOrWhiteSpace(moveSpeedFloat))
        {
            float value = 0f;

            switch (_state)
            {
                case BossState.MovingToWaypoint:
                    value = _manualDashing ? 1f : 0.65f;
                    break;
                case BossState.AttackingPlayer:
                    value = 1f;
                    break;
                default:
                    value = 0f;
                    break;
            }

            animator.SetFloat(moveSpeedFloat, value);
        }
    }
}