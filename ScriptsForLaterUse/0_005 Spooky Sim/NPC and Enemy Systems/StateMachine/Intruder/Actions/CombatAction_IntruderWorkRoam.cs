using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Intruder/Work Roam")]
public class CombatAction_IntruderWorkRoam : CombatAction
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
        _m.TickWorkRoam();
        ActionInProgress = true;
    }
}
