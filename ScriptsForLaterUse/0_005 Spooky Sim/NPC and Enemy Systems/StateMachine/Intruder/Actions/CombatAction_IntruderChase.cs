using UnityEngine;

public class CombatAction_IntruderChase : CombatAction
{
    private IntruderMaster _m;

    public override void Initialization()
    {
        base.Initialization();
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override void PerformAction()
    {
        if (_m == null) { ActionInProgress = false; return; }
        _m.ScanForRobot();
        _m.ChaseTarget();
        ActionInProgress = true;
    }
}
