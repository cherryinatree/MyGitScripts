using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class IntruderMaster : MonoBehaviour
{
    public enum IntruderJob { MessMaker, Saboteur, Illusionist, Haunter, Attacker }

    [Header("Job")]
    public IntruderJob Job = IntruderJob.MessMaker;
    public float JobDurationSeconds = 10f;

    [Header("Wander")]
    public float WanderRadius = 8f;
    public float ArriveSlack = 0.4f;

    [Header("Threat Detection")]
    public float DetectRadius = 14f;
    public string PlayerTag = "Player";
    public LayerMask LOSBlockers = ~0;

    [Header("Flee")]
    public float FleeDistance = 10f;
    public float FleeRepathInterval = 0.35f;
    public float FleeSpeedMultiplier = 1.25f;

    [Header("Teleport")]
    public float TeleportCooldownSeconds = 6f;
    public float TeleportOutSeconds = 0.6f;
    public float TeleportInSeconds = 0.3f;
    public string TeleportOutTrigger = "TeleportOut";
    public string TeleportInTrigger = "TeleportIn";

    [Header("Combat")]
    public float AttackRange = 1.7f;
    public float AttackCooldown = 1.2f;
    public float AttackDamage = 12f;
    public float AttackHitDelay = 0.35f;
    public string AttackTrigger = "Attack";

    [Header("Haunt")]
    public float ScareRange = 7f;
    public float ScareCooldown = 2.0f;
    public float ScareDamage = 8f;
    public float ScareHitDelay = 0.25f;
    public string ScareTrigger = "Scare";

    [Header("Refs")]
    public Animator Anim;
    public NavMeshAgent Agent;

    [Header("Flee Smarts")]
    public int FleeCandidateCount = 12;
    public float FleeMinDistance = 6f;
    public float FleeMaxDistance = 14f;
    public float FleeMinEdgeDistance = 0.7f; // avoid corners/edges
    public float FleeRepathIfStuckSeconds = 0.6f;
    public float FleeStuckVelocity = 0.2f;
    public float FleeNavSampleRadius = 6f;

    private float _stuckTimer;
    private float _lastRemaining = float.PositiveInfinity;


    // Flags Decisions read
    public Transform CurrentThreat { get; private set; }
    public bool HasThreat => CurrentThreat != null;
    public bool WantsTeleport { get; private set; }
    public bool TeleportComplete { get; private set; }
    public bool IllusionDone { get; private set; }
    public bool InAttackRange { get; private set; }
    public bool CanTeleportNow => Time.time >= _nextTeleportAllowed;

    private Health _health;
    private float _jobEndsAt;
    private float _nextTeleportAllowed;
    private float _nextAttackAllowed;
    private float _nextScareAllowed;
    private float _nextFleeRepath;
    private Coroutine _teleportRoutine;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();

        // If damaged, treat attacker as threat (optional)
        _health.OnDamaged.AddListener((amt, attacker) =>
        {
            if (attacker != null) CurrentThreat = attacker.transform;
        });
    }

    private void OnEnable()
    {
        BeginNewJobLoop();
    }

    public void BeginNewJobLoop()
    {
        WantsTeleport = false;
        TeleportComplete = false;
        IllusionDone = false;
        _jobEndsAt = Time.time + JobDurationSeconds;

        PickNewWanderDestination();
    }

    // Called by WorkRoam Action every frame
    public void TickWorkRoam()
    {
        TeleportComplete = false;
        IllusionDone = false;

        UpdateThreat();

        // Job timer
        if (!WantsTeleport && Time.time >= _jobEndsAt)
            WantsTeleport = true;

        // Keep wandering
        if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + ArriveSlack)
            PickNewWanderDestination();

        // Do job “ambiently” while roaming
        TickJobBehavior();
    }

    public void TickFlee()
    {
        TeleportComplete = false;
        IllusionDone = false;

        UpdateThreat();

        if (!HasThreat)
        {
            BeginNewJobLoop();
            return;
        }

        // Attackers can still use this flag
        InAttackRange = Job == IntruderJob.Attacker &&
                        Vector3.Distance(transform.position, CurrentThreat.position) <= AttackRange;

        // Repath cadence
        bool timeToRepath = Time.time >= _nextFleeRepath;
        bool stuck = IsStuck();

        if (timeToRepath || stuck)
        {
            _nextFleeRepath = Time.time + FleeRepathInterval;

            if (TryPickBestFleeDestination(CurrentThreat.position, out var dest))
                Agent.SetDestination(dest);
        }
    }

    private bool IsStuck()
    {
        if (Agent.pathPending) return false;

        // If velocity is low and remaining distance isn't improving, count stuck time
        float vel = Agent.velocity.magnitude;
        float rem = Agent.remainingDistance;

        bool noProgress = rem >= (_lastRemaining - 0.05f);
        _lastRemaining = rem;

        if (vel < FleeStuckVelocity && noProgress)
            _stuckTimer += Time.deltaTime;
        else
            _stuckTimer = 0f;

        return _stuckTimer >= FleeRepathIfStuckSeconds;
    }

    private bool TryPickBestFleeDestination(Vector3 threatPos, out Vector3 best)
    {
        best = transform.position;

        Vector3 awayDir = (transform.position - threatPos);
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.001f) awayDir = transform.forward;
        awayDir.Normalize();

        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < FleeCandidateCount; i++)
        {
            // Bias samples in a cone away from threat (not a single straight line)
            float angle = Random.Range(-75f, 75f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * awayDir;

            float dist = Random.Range(FleeMinDistance, FleeMaxDistance);
            Vector3 candidate = transform.position + dir * dist;

            // Sample to NavMesh
            if (!NavMesh.SamplePosition(candidate, out var hit, FleeNavSampleRadius, NavMesh.AllAreas))
                continue;

            Vector3 pos = hit.position;

            // Avoid hugging corners/edges
            if (NavMesh.FindClosestEdge(pos, out var edge, NavMesh.AllAreas))
            {
                float edgeDist = Vector3.Distance(pos, edge.position);
                if (edgeDist < FleeMinEdgeDistance) continue;
            }

            // Must be reachable
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, pos, NavMesh.AllAreas, path)) continue;
            if (path.status != NavMeshPathStatus.PathComplete) continue;

            float pathLen = PathLength(path);
            float threatDist = Vector3.Distance(pos, threatPos);

            // Bonus: break line of sight (makes fleeing feel clever)
            bool blocksLOS = Physics.Raycast(
                transform.position + Vector3.up * 1.3f,
                (threatPos + Vector3.up * 1.3f) - (transform.position + Vector3.up * 1.3f),
                out var _,
                Vector3.Distance(transform.position, threatPos),
                LOSBlockers,
                QueryTriggerInteraction.Ignore);

            // Score: farther from threat is good, shorter path is good, LOS break is good
            float score = threatDist - (pathLen * 0.4f) + (blocksLOS ? 4f : 0f);

            if (score > bestScore)
            {
                bestScore = score;
                best = pos;
                found = true;
            }
        }

        return found;
    }

    private float PathLength(NavMeshPath path)
    {
        float sum = 0f;
        var c = path.corners;
        for (int i = 1; i < c.Length; i++)
            sum += Vector3.Distance(c[i - 1], c[i]);
        return sum;
    }


    public void StartTeleportToRandomPatrolPoint()
    {
        if (_teleportRoutine != null) return;
        if (!CanTeleportNow) return;

        _teleportRoutine = StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        TeleportComplete = false;
        _nextTeleportAllowed = Time.time + TeleportCooldownSeconds;

        Agent.ResetPath();
        if (Anim != null) Anim.SetTrigger(TeleportOutTrigger);

        yield return new WaitForSeconds(TeleportOutSeconds);

        // Pick a patrol point
        if (PatrolPoint.All.Count == 0)
        {
            // fallback: just restart job loop where you are
            BeginNewJobLoop();
        }
        else
        {
            var p = PatrolPoint.All[Random.Range(0, PatrolPoint.All.Count)];
            if (p != null)
            {
                Agent.Warp(p.transform.position);
            }
            BeginNewJobLoop();
        }

        if (Anim != null) Anim.SetTrigger(TeleportInTrigger);
        yield return new WaitForSeconds(TeleportInSeconds);

        WantsTeleport = false;
        TeleportComplete = true;

        _teleportRoutine = null;
    }

    // Attacker job: hit security robot OR player when seen
    public void TickAttack()
    {
        UpdateThreat();
        if (!HasThreat) return;

        float dist = Vector3.Distance(transform.position, CurrentThreat.position);
        InAttackRange = dist <= AttackRange;

        if (!InAttackRange) return;
        if (Time.time < _nextAttackAllowed) return;

        _nextAttackAllowed = Time.time + AttackCooldown;

        Face(CurrentThreat.position);
        if (Anim != null) Anim.SetTrigger(AttackTrigger);

        StartCoroutine(DealDamageAfterDelay(CurrentThreat.gameObject, AttackDamage, AttackHitDelay));
    }

    // Haunter job: “scare” does damage (can be treated as psychic damage)
    public void TickHaunt()
    {
        UpdateThreat();
        if (!HasThreat) return;

        float dist = Vector3.Distance(transform.position, CurrentThreat.position);
        if (dist > ScareRange) return;
        if (Time.time < _nextScareAllowed) return;

        _nextScareAllowed = Time.time + ScareCooldown;

        Face(CurrentThreat.position);
        if (Anim != null) Anim.SetTrigger(ScareTrigger);

        StartCoroutine(DealDamageAfterDelay(CurrentThreat.gameObject, ScareDamage, ScareHitDelay));
    }

    // Illusionist job: call once per “encounter”, then flee/teleport
    public void DoIllusionTrickOnce()
    {
        IllusionDone = false;
        UpdateThreat();
        if (!HasThreat) { IllusionDone = true; return; }

        // Example: pick one trick based on target type
        if (CurrentThreat.CompareTag(PlayerTag))
        {
            // teleport player to random patrol point (you’ll implement Player teleporter hook)
            TryTeleportPlayer(CurrentThreat.gameObject);
        }
        else
        {
            // try to confuse a security robot to attack others
            TryConfuseRobot(CurrentThreat.gameObject);
        }

        // Optional: invis + clone hook goes here
        // SpawnCloneAndHide();

        IllusionDone = true;
    }

    private void TickJobBehavior()
    {
        // Keep these lightweight. Think “leave a mess every few seconds” not “every frame”.
        // You’ll implement these job-specific systems next.
        switch (Job)
        {
            case IntruderJob.MessMaker:
                // leave mess occasionally (spawn a MessMark / Cleanable)
                break;

            case IntruderJob.Saboteur:
                // damage/break fixables occasionally
                break;

            case IntruderJob.Illusionist:
                // usually does tricks only when threatened, not during normal roam
                break;

            case IntruderJob.Haunter:
                // could do ambient scares sometimes too
                break;

            case IntruderJob.Attacker:
                // attacker tends to attack only when it sees a target
                break;
        }
    }

    private void PickNewWanderDestination()
    {
        Vector3 candidate = transform.position + Random.insideUnitSphere * WanderRadius;
        candidate.y = transform.position.y;

        if (NavMesh.SamplePosition(candidate, out var hit, WanderRadius, NavMesh.AllAreas))
            Agent.SetDestination(hit.position);
    }

    private void UpdateThreat()
    {
        // Prefer closest visible security robot or player
        Transform best = null;
        float bestD = float.PositiveInfinity;

        // Security robots
        var robots = FindObjectsOfType<RobotMaster>();
        for (int i = 0; i < robots.Length; i++)
        {
            var r = robots[i];
            if (r == null) continue;
            if (r.Role != RobotMaster.RobotRole.Security) continue;
            if (r.IsDead) continue;

            if (!HasLineOfSight(r.transform)) continue;
            float d = Vector3.Distance(transform.position, r.transform.position);
            if (d < bestD && d <= DetectRadius) { bestD = d; best = r.transform; }
        }

        // Player by tag (optional)
        var player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (player != null && HasLineOfSight(player.transform))
        {
            float d = Vector3.Distance(transform.position, player.transform.position);
            if (d < bestD && d <= DetectRadius) { bestD = d; best = player.transform; }
        }

        CurrentThreat = best;
    }

    private bool HasLineOfSight(Transform t)
    {
        Vector3 from = transform.position + Vector3.up * 1.3f;
        Vector3 to = t.position + Vector3.up * 1.2f;
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;

        if (Physics.Raycast(from, dir.normalized, out var hit, dist, LOSBlockers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.IsChildOf(t);
        }
        return true;
    }

    private void Face(Vector3 pos)
    {
        Vector3 look = pos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

    private IEnumerator DealDamageAfterDelay(GameObject target, float amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target == null) yield break;
        var hp = target.GetComponentInParent<Health>();
        if (hp != null && !hp.IsDead)
            hp.TakeDamage(amount, gameObject);
    }

    private void TryTeleportPlayer(GameObject player)
    {
        // Hook point: implement your own player teleport API
        // Example: player.GetComponent<PlayerTeleporter>()?.TeleportTo(point.position);
    }

    private void TryConfuseRobot(GameObject robot)
    {
        var confused = robot.GetComponentInParent<RobotConfusedEffect>();
        if (confused == null) confused = robot.AddComponent<RobotConfusedEffect>();
        confused.Begin(6f);
    }

    // ---------------------------
    // Compatibility shim (older Actions/Decisions support)
    // ---------------------------

    // Old name used by earlier scripts:
    public bool HasTarget => HasThreat;

    // Old patrol API used by CombatAction_IntruderPatrol:
    public void PickPatrolDestination()
    {
        // In the newer design, WorkRoam handles wandering/job.
        // This keeps the old action working by picking a roam point.
        // (If you want "patrol points only", swap this to pick a PatrolPoint instead.)
        PickNewWanderDestination_Compat();
    }

    // Old sensing API used by CombatAction_IntruderPatrol / decisions:
    public void ScanForRobot()
    {
        // Old behavior: refresh a target each tick
        // New behavior: UpdateThreat() does that
        UpdateThreat_Compat();
    }

    // Old chase API used by CombatAction_IntruderChase:
    public void ChaseTarget()
    {
        UpdateThreat_Compat();
        if (!HasThreat || Agent == null) return;

        Agent.SetDestination(CurrentThreat.position);
    }

    // Helpers that call your existing internal methods.
    // If your IntruderMaster already has these methods with different names,
    // edit these two wrappers to call the correct ones.
    private void UpdateThreat_Compat()
    {
        // If your IntruderMaster already has UpdateThreat(), call it.
        // Otherwise, paste your threat scan here.
        // --- CALL YOUR REAL METHOD HERE ---
        UpdateThreat();
    }

    private void PickNewWanderDestination_Compat()
    {
        // If your IntruderMaster already has PickNewWanderDestination(), call it.
        // Otherwise, paste your wander destination logic here.
        // --- CALL YOUR REAL METHOD HERE ---
        PickNewWanderDestination();
    }

}
