using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Route To Job")]
public class CombatAction_RobotRouteToJob : CombatAction
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

        if (!_brain.HasJob)
        {
            ActionInProgress = false;
            return;
        }

        if (!_started)
        {
            _started = true;
            _brain.StartRouteToCurrentDestination();
        }

        // Keep running until arrived or failed
        ActionInProgress = !_brain.ArrivedAtJob && !_brain.NavigationFailed;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopRoute();
        _started = false;
    }
}
