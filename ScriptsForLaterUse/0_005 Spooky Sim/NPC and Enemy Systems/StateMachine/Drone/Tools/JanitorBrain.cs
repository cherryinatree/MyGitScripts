using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class JanitorBrain : MonoBehaviour
{
    public enum Plan { None, GoToMess, GoToAirlock, GoToPatrol }

    [Header("Senses")]
    public JanitorSenseBubble Sensor;

    [Header("Carry Behavior")]
    [Tooltip("If carrying trash but not full, dump after this many seconds. Set 0 to disable.")]
    public float DumpAfterSeconds = 0f;

    [Header("Patrol")]
    public Vector3 PatrolDestination { get; private set; }
    private Vector3 _lastPatrol;

    [Header("Refs")]
    public RobotMaster Robot;
    public TrashCarrier Carrier;
    public AirLockDisposal Airlock;

    [Header("Runtime")]
    public MessItem CurrentMess;

    public Plan CurrentPlan { get; private set; } = Plan.None;
    public bool TaskDone { get; private set; }

    private Coroutine _taskRoutine;
    private float _carryingSince;

    private void Awake()
    {
        if (Robot == null) Robot = GetComponentInParent<RobotMaster>() ?? GetComponent<RobotMaster>();

        if (Carrier == null)
            Carrier = GetComponentInParent<TrashCarrier>() ??
                      (Robot != null ? Robot.GetComponentInParent<TrashCarrier>() ?? Robot.GetComponent<TrashCarrier>() : null);

        if (Sensor == null) Sensor = GetComponentInChildren<JanitorSenseBubble>(includeInactive: true);
        if (Airlock == null) Airlock = FindFirstObjectByType<AirLockDisposal>();

        _carryingSince = 0f;
    }

    public void DecidePlan()
    {
        TaskDone = false;

        int carried = (Carrier != null) ? Carrier.Count : 0;

        // Track how long we've been carrying trash
        if (carried > 0 && _carryingSince <= 0f) _carryingSince = Time.time;
        if (carried == 0) _carryingSince = 0f;

        // If full, dump now
        if (carried > 0 && Carrier != null && Carrier.IsFull && Airlock != null)
        {
            CurrentPlan = Plan.GoToAirlock;
            return;
        }

        // Optional: dump after carrying for a while
        if (carried > 0 && Airlock != null && DumpAfterSeconds > 0f && _carryingSince > 0f)
        {
            if (Time.time - _carryingSince >= DumpAfterSeconds)
            {
                CurrentPlan = Plan.GoToAirlock;
                return;
            }
        }

        // Keep current target if still valid
        if (CurrentMess != null && !CurrentMess.IsResolved)
        {
            CurrentPlan = Plan.GoToMess;
            return;
        }

        // Only acquire a mess if we have detected it in the sensor bubble
        if (AcquireDetectedMessTarget())
        {
            CurrentPlan = Plan.GoToMess;
            return;
        }

        // Default: patrol until we detect something
        PickPatrolDestination();
        CurrentPlan = Plan.GoToPatrol;
    }

    private bool AcquireDetectedMessTarget()
    {
        if (Sensor == null) return false;

        if (!Sensor.TryGetBest(transform.position, Carrier, out var m))
            return false;

        if (!m.TryClaim(gameObject))
            return false;

        CurrentMess = m;
        return true;
    }

    public void ClearMessTarget()
    {
        if (CurrentMess != null) CurrentMess.ReleaseClaim(gameObject);
        CurrentMess = null;
    }

    // ---------- Patrol ----------
    public void PickPatrolDestination()
    {
        if (PatrolPoint.All.Count > 0)
        {
            for (int i = 0; i < 12; i++)
            {
                var p = PatrolPoint.All[Random.Range(0, PatrolPoint.All.Count)];
                if (p == null) continue;

                var pos = p.transform.position;
                if (Vector3.Distance(pos, _lastPatrol) < 2f) continue;

                _lastPatrol = pos;
                PatrolDestination = pos;
                return;
            }

            var fallback = PatrolPoint.All[Random.Range(0, PatrolPoint.All.Count)];
            PatrolDestination = fallback != null ? fallback.transform.position : transform.position;
            _lastPatrol = PatrolDestination;
            return;
        }

        PatrolDestination = transform.position + Random.insideUnitSphere * 8f;
        _lastPatrol = PatrolDestination;
    }

    public void RouteToPatrol()
    {
        if (Robot == null) return;

        if (PatrolDestination == Vector3.zero)
            PickPatrolDestination();

        Robot.ArrivedAtJob = false;
        Robot.NavigationFailed = false;

        Robot.CurrentDestination = PatrolDestination;
        Robot.StartRouteToCurrentDestination();
    }

    // ---------- Routing ----------
    public void RouteToMess()
    {
        if (Robot == null || CurrentMess == null) return;

        Robot.ArrivedAtJob = false;
        Robot.NavigationFailed = false;

        Robot.CurrentDestination = CurrentMess.JobPoint;
        Robot.StartRouteToCurrentDestination();
    }

    public void RouteToAirlock()
    {
        if (Robot == null || Airlock == null || Airlock.InteractionPoint == null) return;

        Robot.ArrivedAtJob = false;
        Robot.NavigationFailed = false;

        Robot.CurrentDestination = Airlock.InteractionPoint.position;
        Robot.StartRouteToCurrentDestination();
    }

    // ---------- Execution ----------
    public void BeginDoMess()
    {
        if (_taskRoutine != null) return;
        _taskRoutine = StartCoroutine(DoMessRoutine());
    }

    public void BeginDoAirlock()
    {
        if (_taskRoutine != null) return;
        _taskRoutine = StartCoroutine(DoAirlockRoutine());
    }

    private IEnumerator DoMessRoutine()
    {
        if (Robot != null) Robot.StopRoute();

        var m = CurrentMess;
        if (m == null)
        {
            TaskDone = true;
            _taskRoutine = null;
            yield break;
        }

        GameObject interactor = (Robot != null) ? Robot.gameObject : gameObject;

        m.Interact(interactor);

        float timeout = (m is MessStain) ? 5.0f : 0.75f;
        float t = 0f;

        while (m != null && !m.IsResolved && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        ClearMessTarget();

        TaskDone = true;
        _taskRoutine = null;
    }

    private IEnumerator DoAirlockRoutine()
    {
        if (Robot != null) Robot.StopRoute();

        if (Airlock == null)
        {
            TaskDone = true;
            _taskRoutine = null;
            yield break;
        }

        GameObject interactor = (Robot != null) ? Robot.gameObject : gameObject;

        var carrier = interactor.GetComponentInParent<TrashCarrier>() ?? interactor.GetComponent<TrashCarrier>();
        if (carrier == null || carrier.Count <= 0)
        {
            TaskDone = true;
            _taskRoutine = null;
            yield break;
        }

        float wait = 0f;
        while (Airlock.IsBusy && wait < 5f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        yield return Airlock.RunDisposalCycle(interactor);

        // After dumping, reset carry timer
        _carryingSince = 0f;

        TaskDone = true;
        _taskRoutine = null;
    }
}
