using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Janitor Route To Airlock")]
public class CombatAction_JanitorRouteToAirlock : CombatAction
{
    private JanitorBrain _j;
    private bool _started;

    public override void Initialization()
    {
        base.Initialization();
        _j = GetComponentInParent<JanitorBrain>() ?? GetComponent<JanitorBrain>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _started = false;
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_j == null) { ActionInProgress = false; return; }

        if (!_started)
        {
            _started = true;
            _j.RouteToAirlock();
        }

        ActionInProgress = true;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        _started = false;
    }
}
