using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Chase Intruder")]
public class CombatAction_RobotChaseIntruder : CombatAction
{
    private RobotMaster _brain;
    private bool _started;

    public override void Initialization()
    {
        base.Initialization();
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _started = false;
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_brain == null) Initialization();
        if (_brain == null) { ActionInProgress = false; return; }

        if (_brain.CurrentJob != RobotMaster.JobType.RespondIntruder)
        {
            ActionInProgress = false;
            return;
        }

        if (!_started)
        {
            _started = true;
            _brain.StartChaseIntruder();
        }

        // chase is “in progress” until we’re in range or it failed
        ActionInProgress = !_brain.InAttackRange && !_brain.NavigationFailed && !_brain.JobComplete;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopChaseIntruder();
        _started = false;
    }
}
