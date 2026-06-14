using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class RobotMaster : MonoBehaviour, IRobotAccess
{
    public enum RobotRole { Cleaner, Maintenance, Security }
    public enum JobType { None, Wander, Clean, Fix, RespondIntruder }


    public RobotCombatController Combat;

    [Header("Role")]
    public RobotRole Role = RobotRole.Cleaner;

    [Header("Refs")]
    public RobotNavigator Navigator;
    public TransportRouter Router;

    [Header("Senses")]
    public float ThinkInterval = 0.5f;
    public float DetectionRadius = 18f;        // for intruders (security)
    public float JobSearchRadius = 40f;        // for clean/fix (role based)

    [Header("Wander")]
    public float WanderRadius = 10f;
    public float WanderPauseSeconds = 2f;

    [Header("Job Interaction")]
    public float JobArriveDistance = 1.0f;

    [Header("Robot Anim Triggers")]
    public string CleanTrigger = "Clean";
    public string RepairTrigger = "Repair";
    public string TaseTrigger = "Tase";

    [Header("Combat (Security)")] 
    public RobotGun Gun;

    public Health RobotHealth;                 // assign or auto-find
    public float ChaseRepathInterval = 0.25f;
    public float RepathMoveThreshold = 0.6f;   // only repath if target moved enough
    public float AttackRange = 1.8f;
    public float AttackCooldown = 1.0f;
    public float AttackHitDelay = 0.35f;
    public float AttackDamage = 25f;

    [Header("Animations (Robot)")]
    public string AttackTrigger = "Attack";    // or "Tase" if that’s your anim
    public string SuccessTrigger = "Success";
    public float SuccessSeconds = 1.2f;

    public bool InAttackRange { get; set; }
    public bool IsDead => (RobotHealth != null && RobotHealth.IsDead);

    private Coroutine _chaseRoutine;
    private Coroutine _attackRoutine;
    private bool _finishingSuccess;


    // Public flags for Decisions to read
    public JobType CurrentJob { get; set; } = JobType.None;
    public bool HasJob => CurrentJob != JobType.None;
    public bool ArrivedAtJob { get; set; }
    public bool JobComplete { get; set; }
    public bool NavigationFailed { get; set; }

    // Current job targets
    public CleanableTarget CurrentCleanTarget { get; set; }
    public FixableTarget CurrentFixTarget { get; set; }
    public Intruder CurrentIntruder { get; set; }
    public Vector3 CurrentDestination { get; set; }

    public bool HasSecurityClearance => Role == RobotRole.Security;

    private float _nextThinkTime;
    private Coroutine _routeRoutine;
    private Coroutine _jobRoutine;

    private float _nextWanderPickTime;

    [Header("Debug")]
    public bool DebugRouting = true;
    public float NavSampleRadius = 6f;

    public bool HasActiveIntruderTarget => CurrentIntruder != null && CurrentIntruder.IsActive;

    private Vector3 _lastPatrolPos;



    private void Log(string msg)
    {
        if (DebugRouting) Debug.Log($"[RobotMaster:{name}] {msg}");
    }

    private bool TryProjectToNavMesh(Vector3 pos, out Vector3 projected)
    {
        if (NavMesh.SamplePosition(pos, out var hit, NavSampleRadius, NavMesh.AllAreas))
        {
            projected = hit.position;
            return true;
        }

        projected = pos;
        return false;
    }

    private bool HasDirectPath(Vector3 from, Vector3 to)
    {
        // Works even if Router is null.
        if (!NavMesh.SamplePosition(from, out var hitA, NavSampleRadius, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(to, out var hitB, NavSampleRadius, NavMesh.AllAreas)) return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(hitA.position, hitB.position, NavMesh.AllAreas, path)) return false;
        return path.status == NavMeshPathStatus.PathComplete;
    }


    private void Awake()
    {
        if (Navigator == null) Navigator = GetComponentInParent<RobotNavigator>();
        if (Navigator == null) Navigator = GetComponent<RobotNavigator>();

        if (Router == null) Router = FindFirstObjectByType<TransportRouter>(); 
        if (RobotHealth == null) RobotHealth = GetComponentInParent<Health>();
        if (RobotHealth == null) RobotHealth = GetComponent<Health>(); 
        if (Combat == null) Combat = GetComponentInParent<RobotCombatController>() ?? GetComponent<RobotCombatController>();
        if (Gun == null) Gun = GetComponentInChildren<RobotGun>();


    }

    public void ScanForIntruder()
    {
        if (Role != RobotRole.Security) return;

        var intr = FindBestIntruder();
        if (intr != null)
            AssignIntruderJob(intr);
    }

    public void PickNextPatrolPoint()
    {
        // Security default job is "Wander" (we’ll use it as Patrol job)
        CurrentJob = JobType.Wander;

        // Prefer PatrolPoints if any exist
        if (PatrolPoint.All.Count > 0)
        {
            // Try a few times to avoid picking the same point repeatedly
            for (int i = 0; i < 12; i++)
            {
                var p = PatrolPoint.All[Random.Range(0, PatrolPoint.All.Count)];
                if (p == null) continue;

                Vector3 pos = p.transform.position;
                if (Vector3.Distance(pos, _lastPatrolPos) < 2.0f) continue;

                _lastPatrolPos = pos;
                CurrentDestination = pos;
                ResetProgressFlags();
                return;
            }

            // fallback: just pick one
            var fallback = PatrolPoint.All[Random.Range(0, PatrolPoint.All.Count)];
            if (fallback != null)
            {
                _lastPatrolPos = fallback.transform.position;
                CurrentDestination = _lastPatrolPos;
                ResetProgressFlags();
                return;
            }
        }

        // If no points exist, fallback to local random navmesh
        CurrentDestination = PickWanderPoint();
        ResetProgressFlags();
    }

    public void ReturnToPatrol()
    {
        // clear target and force new patrol destination
        CurrentIntruder = null;
        JobComplete = false;
        NavigationFailed = false;
        ArrivedAtJob = false;
        CurrentDestination = Vector3.zero;
        PickNextPatrolPoint();
    }



    public void StartChaseIntruder()
    {
        StopChaseIntruder();
        if (CurrentIntruder == null || !CurrentIntruder.IsActive) return;

        _chaseRoutine = StartCoroutine(ChaseIntruderRoutine());
    }

    public void StopChaseIntruder()
    {
        if (_chaseRoutine != null)
        {
            StopCoroutine(_chaseRoutine);
            _chaseRoutine = null;
        }
    }

    public void StartAttackIntruder()
    {
        StopAttackIntruder();
        if (CurrentIntruder == null || !CurrentIntruder.IsActive) return;

        _attackRoutine = StartCoroutine(AttackIntruderRoutine());
    }

    public void StopAttackIntruder()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
    }


    private IEnumerator ChaseIntruderRoutine()
    {
        NavigationFailed = false;
        JobComplete = false;
        _finishingSuccess = false;

        if (Navigator == null || Navigator.Agent == null || !Navigator.Agent.isOnNavMesh)
        {
            NavigationFailed = true;
            yield break;
        }

        Vector3 lastGoal = Vector3.positiveInfinity;
        float nextRepath = 0f;

        while (CurrentIntruder != null && CurrentIntruder.IsActive && !IsDead)
        {
            Vector3 raw = CurrentIntruder.transform.position;
            if (!TryProjectToNavMesh(raw, out var projected))
            {
                NavigationFailed = true;
                yield break;
            }

            // Attack-range check (this drives state transition)
            float dist = Vector3.Distance(transform.position, projected);
            InAttackRange = dist <= AttackRange;

            if (InAttackRange)
            {
                // Stop so engage state can play attack cleanly
                Navigator.Stop();
                yield break;
            }

            // Repath occasionally or when intruder moved enough
            if (Time.time >= nextRepath || Vector3.Distance(projected, lastGoal) >= RepathMoveThreshold)
            {
                nextRepath = Time.time + ChaseRepathInterval;
                lastGoal = projected;

                // Direct chase (doors will open via triggers)
                Navigator.DesiredTransportDestination = null;
                Navigator.SetGoal(projected);
            }

            yield return null;
        }

        // Intruder died or disappeared
        InAttackRange = false;
    }
    private IEnumerator AttackIntruderRoutine()
    {
        JobComplete = false;
        NavigationFailed = false;

        if (Combat == null || Combat.Loadout == null) { NavigationFailed = true; yield break; }

        while (CurrentIntruder != null && CurrentIntruder.IsActive && !IsDead)
        {
            // Decide melee vs ranged
            var mode = Combat.Loadout.ChooseMode(CurrentIntruder);

            if (mode == RobotCombatLoadout.AttackMode.Ranged)
            {
                if (Gun == null) Gun = GetComponentInChildren<RobotGun>();
                if (Gun == null) { NavigationFailed = true; yield break; }

                // Keep gun stats synced from loadout (optional)
                Gun.Damage = Combat.Loadout.RangedDamage;
                Gun.ProjectileSpeed = Combat.Loadout.ProjectileSpeed;
                // YOU want 0.1 sec fire rate:
                Gun.FireInterval = 0.1f;
                Gun.Range = Combat.Loadout.GetRange(mode);

                // Fire while in range, otherwise return to chase state
                Gun.StartFiring(gameObject, CurrentIntruder);

                while (CurrentIntruder != null && CurrentIntruder.IsActive && !IsDead)
                {
                    float dist = Vector3.Distance(transform.position, CurrentIntruder.transform.position);
                    InAttackRange = dist <= Gun.Range;

                    if (!InAttackRange) break;

                    yield return null;
                }

                Gun.StopFiring();

                if (CurrentIntruder == null || !CurrentIntruder.IsActive) break;

                yield break; // out of range: let decisions push back to Chase
            }

            // --- melee fallback (keep your existing melee approach) ---
            float distMelee = Vector3.Distance(transform.position, CurrentIntruder.transform.position);
            InAttackRange = distMelee <= Combat.Loadout.GetRange(mode);

            if (!InAttackRange) yield break;

            Combat.FaceTarget(CurrentIntruder.transform.position);
            if (Combat.TryStartAttack(CurrentIntruder))
            {
                while (Combat.IsAttacking && CurrentIntruder != null && CurrentIntruder.IsActive)
                    yield return null;
            }
            else
            {
                yield return null;
            }
        }

        // Intruder dead: success animation
        if (Navigator != null) Navigator.PlayTrigger(SuccessTrigger);
        yield return new WaitForSeconds(SuccessSeconds);

        JobComplete = true;
    }


    // Called by ThinkAction
    public void Think()
    {
        if (Time.time < _nextThinkTime) return;
        _nextThinkTime = Time.time + ThinkInterval;

        // If we have a valid job in progress, keep it.
        if (IsCurrentJobStillValid())
        {
            UpdateArrivalFlag();
            return;
        }

        ClearJobInternal();

        // Pick new job based on role
        if (Role == RobotRole.Security)
        {
            var intr = FindBestIntruder();
            if (intr != null)
            {
                AssignIntruderJob(intr);
                return;
            }
        }
        else
        {
            if (!IntruderAlertSystem.AlertActive)
            {
                if (Role == RobotRole.Cleaner)
                {
                    var c = FindBestCleanable();
                    if (c != null)
                    {
                        AssignCleanJob(c);
                        return;
                    }
                }

                if (Role == RobotRole.Maintenance)
                {
                    var f = FindBestFixable();
                    if (f != null)
                    {
                        AssignFixJob(f);
                        return;
                    }
                }
            }
        }

        // Default idle: wander
        AssignWanderJob();
    }

    private bool IsCurrentJobStillValid()
    {
        switch (CurrentJob)
        {
            case JobType.Clean:
                return CurrentCleanTarget != null && CurrentCleanTarget.NeedsCleaning && !CurrentCleanTarget.IsClaimedByOther(this);
            case JobType.Fix:
                return CurrentFixTarget != null && CurrentFixTarget.NeedsFixing && !CurrentFixTarget.IsClaimedByOther(this);
            case JobType.RespondIntruder:
                return CurrentIntruder != null && CurrentIntruder.IsActive;
            case JobType.Wander:
                return true;
            default:
                return false;
        }
    }

    private void UpdateArrivalFlag()
    {
        float dist = Vector3.Distance(transform.position, CurrentDestination);
        ArrivedAtJob = dist <= JobArriveDistance && !Navigator.IsBusy;
    }

    // ---------- Assign jobs ----------
    private void AssignCleanJob(CleanableTarget target)
    {
        if (target == null) return;
        if (!target.TryClaim(this)) return;

        CurrentJob = JobType.Clean;
        CurrentCleanTarget = target;

        CurrentDestination = target.InteractionPosition;
        ResetProgressFlags();
    }

    private void AssignFixJob(FixableTarget target)
    {
        if (target == null) return;
        if (!target.TryClaim(this)) return;

        CurrentJob = JobType.Fix;
        CurrentFixTarget = target;

        CurrentDestination = target.InteractionPosition;
        ResetProgressFlags();
    }
    private void AssignIntruderJob(Intruder intruder)
    {
        CurrentJob = JobType.RespondIntruder;
        CurrentIntruder = intruder;

        Vector3 raw = intruder.transform.position;
        if (TryProjectToNavMesh(raw, out var projected))
            CurrentDestination = projected;
        else
            CurrentDestination = raw;

        ResetProgressFlags();
    }


    private void AssignWanderJob()
    {
        CurrentJob = JobType.Wander;

        if (Time.time >= _nextWanderPickTime || CurrentDestination == Vector3.zero)
        {
            CurrentDestination = PickWanderPoint();
            _nextWanderPickTime = Time.time + WanderPauseSeconds;
        }

        ResetProgressFlags();
    }

    private void ResetProgressFlags()
    {
        ArrivedAtJob = false;
        JobComplete = false;
        NavigationFailed = false;
    }

    private void ClearJobInternal()
    {
        if (CurrentCleanTarget != null) CurrentCleanTarget.Release(this);
        if (CurrentFixTarget != null) CurrentFixTarget.Release(this);

        CurrentJob = JobType.None;
        CurrentCleanTarget = null;
        CurrentFixTarget = null;
        CurrentIntruder = null;
        CurrentDestination = Vector3.zero;

        ArrivedAtJob = false;
        JobComplete = false;
        NavigationFailed = false;
    }

    // ---------- Routing ----------
    public void StartRouteToCurrentDestination()
    {
        StopRoute();

        if (Navigator == null)
        {
            NavigationFailed = true;
            return;
        }

        // For intruders, update destination to their current position before routing
        if (CurrentJob == JobType.RespondIntruder && CurrentIntruder != null)
        {
            Vector3 raw = CurrentIntruder.transform.position;
            if (TryProjectToNavMesh(raw, out var projected))
                CurrentDestination = projected;
            else
                CurrentDestination = raw;
        }

        _routeRoutine = StartCoroutine(RouteTo(CurrentDestination));
    }

    public void StopRoute()
    {
        if (_routeRoutine != null)
        {
            StopCoroutine(_routeRoutine);
            _routeRoutine = null;
        }
    }
    private IEnumerator RouteTo(Vector3 goal)
    {
        NavigationFailed = false;
        ArrivedAtJob = false;

        if (Navigator == null || Navigator.Agent == null)
        {
            NavigationFailed = true;
            Log("No Navigator/Agent found.");
            yield break;
        }

        if (!Navigator.Agent.isOnNavMesh)
        {
            NavigationFailed = true;
            Log("NavMeshAgent is NOT on a NavMesh (agent.isOnNavMesh == false). Check spawn position / baking.");
            yield break;
        }

        // Project the goal onto navmesh (intruder transforms often aren't exactly on it)
        if (!TryProjectToNavMesh(goal, out var projectedGoal))
        {
            NavigationFailed = true;
            Log($"Could not project GOAL onto NavMesh. Goal={goal}");
            yield break;
        }

        goal = projectedGoal;

        // If direct path is not complete but we *can* reach via transports,
        // skip the "walk into wall" phase and use transports immediately.
        bool directComplete = HasDirectPath(transform.position, goal);

        if (!directComplete)
        {
            if (Router != null && TransportPad.AllPads.Count > 0)
            {
                var earlyPads = Router.FindBestPadRoute(transform.position, goal, TransportPad.AllPads);
                if (earlyPads != null && earlyPads.Count > 0)
                {
                    Log("Direct path incomplete; using transport route immediately.");
                    yield return DoTransportRoute(earlyPads, goal);
                    yield break;
                }
            }

            // No transport route found, so we still try walking (doors might open, etc.)
        }


        // 1) Always try direct nav first (lets doors open via triggers)
        Navigator.DesiredTransportDestination = null;
        Navigator.SetGoal(goal);

        float stuckTimer = 0f;
        float lastRemaining = float.PositiveInfinity;
        const float stuckSecondsToFallback = 3.0f;   // tweak 2–5 seconds
        const float progressEpsilon = 0.05f;

        while (true)
        {
            if (Navigator.ReachedGoal())
            {
                ArrivedAtJob = true;
                yield break;
            }

            // If we're in an interaction (door anim etc), don't count as stuck
            if (Navigator.IsBusy)
            {
                stuckTimer = 0f;
                lastRemaining = float.PositiveInfinity;
                yield return null;
                continue;
            }

            if (!Navigator.Agent.pathPending)
            {
                float rem = Navigator.Agent.remainingDistance;

                // If remaining distance isn't going down, count time as "stuck"
                bool noProgress = rem >= (lastRemaining - progressEpsilon);
                stuckTimer = noProgress ? stuckTimer + Time.deltaTime : 0f;

                lastRemaining = rem;

                // If we're stuck too long, break and try transport routing
                if (stuckTimer >= stuckSecondsToFallback)
                    break;
            }

            yield return null;
        }

        // If we got here, we tried walking and didn't make progress.
        // Now attempt transports (disconnected islands case).


        // 2) Need transports
        if (Router == null)
        {
            NavigationFailed = true;
            Log("No TransportRouter in scene and direct path is not complete. Add a TransportRouter.");
            yield break;
        }

        if (TransportPad.AllPads.Count == 0)
        {
            NavigationFailed = true;
            Log("No TransportPads registered (TransportPad.AllPads is empty). Are pads enabled in scene?");
            yield break;
        }

        var routePads = Router.FindBestPadRoute(transform.position, goal, TransportPad.AllPads);
        if (routePads == null || routePads.Count == 0)
        {
            NavigationFailed = true;
            Log("Router could not find a pad route to goal. Check pad networkIds and disconnected islands coverage.");
            yield break;
        }

        // Hop along pad route
        for (int i = 0; i < routePads.Count - 1; i++)
        {
            var from = routePads[i];
            var to = routePads[i + 1];

            Navigator.DesiredTransportDestination = to;

            // Walk to the FROM pad
            Navigator.SetGoal(from.InteractionPoint.position);
            while (!Navigator.ReachedGoal())
                yield return null;

            // Force the interaction (no trigger needed)
            int startSeq = Navigator.TransportSequence;
            yield return Navigator.ForceInteract(from);

            // If it didn't transport, treat as failure (prevents soft-lock)
            if (Navigator.TransportSequence == startSeq)
            {
                NavigationFailed = true;
                Log($"Transport did not fire on pad '{from.name}'. Check CanInteract/networkId/destination setup.");
                yield break;
            }

            // Verify we arrived where expected
            if (Navigator.LastArrivedPad != to)
            {
                NavigationFailed = true;
                Log($"Transport ended at wrong pad. Expected '{to.name}', got '{(Navigator.LastArrivedPad ? Navigator.LastArrivedPad.name : "null")}'");
                yield break;
            }


            if (Navigator.LastArrivedPad != to)
            {
                NavigationFailed = true;
                Log($"Transport ended at wrong pad. Expected {to.name}, got {(Navigator.LastArrivedPad ? Navigator.LastArrivedPad.name : "null")}");
                yield break;
            }
        }

        Navigator.DesiredTransportDestination = null;
        Navigator.SetGoal(goal);

        while (!Navigator.ReachedGoal())
            yield return null;

        ArrivedAtJob = true;
    }


    private IEnumerator DoTransportRoute(System.Collections.Generic.IList<TransportPad> routePads, Vector3 goal)
    {
        // Hop along pad route
        for (int i = 0; i < routePads.Count - 1; i++)
        {
            var from = routePads[i];
            var to = routePads[i + 1];

            Navigator.DesiredTransportDestination = to;

            // Walk to the FROM pad
            Navigator.SetGoal(from.InteractionPoint.position);
            while (!Navigator.ReachedGoal())
                yield return null;

            // Force the interaction (no trigger needed)
            int startSeq = Navigator.TransportSequence;
            yield return Navigator.ForceInteract(from);

            // If it didn't transport, treat as failure (prevents soft-lock)
            if (Navigator.TransportSequence == startSeq)
            {
                NavigationFailed = true;
                Log($"Transport did not fire on pad '{from.name}'. Check CanInteract/networkId/destination setup.");
                yield break;
            }

            // Verify we arrived where expected
            if (Navigator.LastArrivedPad != to)
            {
                NavigationFailed = true;
                Log($"Transport ended at wrong pad. Expected '{to.name}', got '{(Navigator.LastArrivedPad ? Navigator.LastArrivedPad.name : "null")}'");
                yield break;
            }
        }

        // Finally walk to the goal on the destination island
        Navigator.DesiredTransportDestination = null;
        Navigator.SetGoal(goal);

        while (!Navigator.ReachedGoal())
            yield return null;

        ArrivedAtJob = true;
    }


    // ---------- Job execution ----------
    public void StartDoCurrentJob()
    {
        StopJobRoutine();

        switch (CurrentJob)
        {
            case JobType.Clean:
                if (CurrentCleanTarget != null)
                    _jobRoutine = StartCoroutine(DoClean(CurrentCleanTarget));
                break;

            case JobType.Fix:
                if (CurrentFixTarget != null)
                    _jobRoutine = StartCoroutine(DoFix(CurrentFixTarget));
                break;

            case JobType.RespondIntruder:
                StartAttackIntruder();
                break;


            case JobType.Wander:
                _jobRoutine = StartCoroutine(DoWanderPause());
                break;


            default:
                JobComplete = true;
                break;
        }
    }
    private IEnumerator DoWanderPause()
    {
        // Stop at the wander point and chill briefly
        if (Navigator != null) Navigator.Stop();

        yield return new WaitForSeconds(WanderPauseSeconds);

        // Force Think() to pick a new wander destination next time
        CurrentDestination = Vector3.zero;

        JobComplete = true;
    }

    public void StopJobRoutine()
    {
        if (_jobRoutine != null)
        {
            StopCoroutine(_jobRoutine);
            _jobRoutine = null;
        }
    }

    private IEnumerator DoClean(CleanableTarget target)
    {
        if (target == null || !target.NeedsCleaning) { JobComplete = true; yield break; }

        Navigator.Stop();
        Navigator.PlayTrigger(CleanTrigger);
        target.OnCleaningStarted?.Invoke();

        yield return new WaitForSeconds(target.CleanSeconds);

        target.MarkCleaned();
        target.Release(this);

        JobComplete = true;
    }

    private IEnumerator DoFix(FixableTarget target)
    {
        if (target == null || !target.NeedsFixing) { JobComplete = true; yield break; }

        Navigator.Stop();
        Navigator.PlayTrigger(RepairTrigger);
        target.OnFixStarted?.Invoke();

        yield return new WaitForSeconds(target.FixSeconds);

        target.MarkFixed();
        target.Release(this);

        JobComplete = true;
    }

    private IEnumerator DoSecurity(Intruder intruder)
    {
        if (intruder == null || !intruder.IsActive) { JobComplete = true; yield break; }

        // If we are not close enough, fail so the state machine can route first.
        float d = Vector3.Distance(transform.position, intruder.transform.position);
        if (d > 1.75f)
        {
            JobComplete = false;
            yield break;
        }

        Navigator.Stop();
        Navigator.PlayTrigger(TaseTrigger);

        yield return new WaitForSeconds(0.6f);

        intruder.Neutralize();
        JobComplete = true;
    }

    // ---------- Find targets ----------
    private CleanableTarget FindBestCleanable()
    {
        CleanableTarget best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < CleanableTarget.All.Count; i++)
        {
            var t = CleanableTarget.All[i];
            if (t == null || !t.NeedsCleaning) continue;
            if (t.IsClaimedByOther(this)) continue;

            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist > JobSearchRadius) continue;

            // Lightweight: accept if direct path OR transport route exists
            bool reachable = Router != null && (Router.HasDirectPath(transform.position, t.InteractionPosition) ||
                              Router.FindBestPadRoute(transform.position, t.InteractionPosition, TransportPad.AllPads) != null);

            if (!reachable) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = t;
            }
        }

        return best;
    }

    private FixableTarget FindBestFixable()
    {
        FixableTarget best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < FixableTarget.All.Count; i++)
        {
            var t = FixableTarget.All[i];
            if (t == null || !t.NeedsFixing) continue;
            if (t.IsClaimedByOther(this)) continue;

            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist > JobSearchRadius) continue;

            bool reachable = Router != null && (Router.HasDirectPath(transform.position, t.InteractionPosition) ||
                              Router.FindBestPadRoute(transform.position, t.InteractionPosition, TransportPad.AllPads) != null);

            if (!reachable) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = t;
            }
        }

        return best;
    }

    private Intruder FindBestIntruder()
    {
        Intruder best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < Intruder.All.Count; i++)
        {
            var intr = Intruder.All[i];
            if (intr == null || !intr.IsActive) continue;

            float dist = Vector3.Distance(transform.position, intr.transform.position);
            if (dist > DetectionRadius) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = intr;
            }
        }

        return best;
    }

    private Vector3 PickWanderPoint()
    {
        Vector3 basePos = transform.position;
        Vector3 random = basePos + Random.insideUnitSphere * WanderRadius;
        random.y = basePos.y;

        // Sample on navmesh if possible
        if (UnityEngine.AI.NavMesh.SamplePosition(random, out var hit, WanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            return hit.position;

        return basePos;
    }
}
