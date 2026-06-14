using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Wander Pause")]
public class CombatAction_RobotWanderPause : CombatAction
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

        if (_brain.CurrentJob != RobotMaster.JobType.Wander)
        {
            ActionInProgress = false;
            return;
        }

        if (!_brain.ArrivedAtJob)
        {
            // Route state should run first
            ActionInProgress = false;
            return;
        }

        if (!_started)
        {
            _started = true;
            _brain.StartDoCurrentJob(); // will run DoWanderPause()
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
