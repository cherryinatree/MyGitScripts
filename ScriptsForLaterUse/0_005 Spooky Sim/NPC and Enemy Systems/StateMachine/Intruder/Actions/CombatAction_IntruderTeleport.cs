using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Intruder/Teleport")]
public class CombatAction_IntruderTeleport : CombatAction
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

        if (!_started)
        {
            _started = true;
            _m.StartTeleportToRandomPatrolPoint();
        }

        ActionInProgress = !_m.TeleportComplete;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        _started = false;
    }
}
