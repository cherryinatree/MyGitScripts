using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Attack Intruder")]
public class CombatAction_RobotAttackIntruder : CombatAction
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

        if (!_brain.InAttackRange)
        {
            // let decisions push back to chase state
            ActionInProgress = false;
            return;
        }

        if (!_started)
        {
            _started = true;
            _brain.StartAttackIntruder();
        }

        ActionInProgress = !_brain.JobComplete && !_brain.NavigationFailed;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopAttackIntruder();
        _started = false;
    }
}
