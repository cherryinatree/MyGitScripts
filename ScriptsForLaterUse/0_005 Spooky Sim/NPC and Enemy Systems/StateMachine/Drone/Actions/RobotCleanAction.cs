using UnityEngine;

public class RobotCleanAction : CombatAction
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

        if (_brain.CurrentJob != RobotMaster.JobType.Clean)
        {
            ActionInProgress = false;
            return;
        }

        if (!_brain.ArrivedAtJob)
        {
            // Let your state machine route first
            ActionInProgress = false;
            return;
        }

        if (!_started)
        {
            _started = true;
            _brain.StartDoCurrentJob();
        }

        ActionInProgress = !_brain.JobComplete;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopJobRoutine();
        _started = false;
    }
}
