using UnityEngine;

public class CombatAction_IntruderPatrol : CombatAction
{
    private IntruderMaster _m;
    private bool _started;

    public override void Initialization()
    {
        base.Initialization();
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _started = false;
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_m == null) { ActionInProgress = false; return; }

        _m.ScanForRobot();

        if (!_started)
        {
            _started = true;
            _m.PickPatrolDestination();
        }

        // keep patrolling; when close to destination, pick a new one
        if (!_m.Agent.pathPending && _m.Agent.remainingDistance <= _m.Agent.stoppingDistance + 0.2f)
        {
            _m.PickPatrolDestination();
        }

        ActionInProgress = true;
    }
}
