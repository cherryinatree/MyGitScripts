using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Wander Loop")]
public class CombatAction_RobotWanderLoop : CombatAction
{
    private RobotMaster _brain;

    public override void Initialization()
    {
        base.Initialization();
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_brain == null) Initialization();
        if (_brain == null) { ActionInProgress = false; return; }

        // Think will pick Wander if nothing else is needed
        _brain.Think();

        // If we have a wander destination (or any job destination), keep routing
        if (_brain.HasJob && !_brain.ArrivedAtJob && !_brain.NavigationFailed)
            _brain.StartRouteToCurrentDestination();

        // Always stay "in progress" so this state persists
        ActionInProgress = true;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopRoute();
    }
}
