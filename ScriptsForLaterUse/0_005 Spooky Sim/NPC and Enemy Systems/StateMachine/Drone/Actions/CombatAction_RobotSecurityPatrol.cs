using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Security Patrol")]
public class CombatAction_RobotSecurityPatrol : CombatAction
{
    private RobotMaster _brain;
    private bool _routing;

    public override void Initialization()
    {
        base.Initialization();
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _routing = false;
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_brain == null) Initialization();
        if (_brain == null) { ActionInProgress = false; return; }

        // Always scan while patrolling
        _brain.ScanForIntruder();

        // If we acquired an intruder target, stop routing so chase can take over
        if (_brain.HasActiveIntruderTarget)
        {
            _brain.StopRoute();
            ActionInProgress = false; // let decisions transition to Chase
            return;
        }

        // No intruder: keep patrolling forever
        if (_brain.CurrentDestination == Vector3.zero || _brain.ArrivedAtJob || _brain.NavigationFailed)
        {
            _brain.PickNextPatrolPoint();
            _routing = false;
        }

        if (!_routing)
        {
            _routing = true;
            _brain.StartRouteToCurrentDestination(); // doors + transports + fallback logic
        }

        // Stay in patrol state
        ActionInProgress = true;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        if (_brain != null) _brain.StopRoute();
        _routing = false;
    }
}
