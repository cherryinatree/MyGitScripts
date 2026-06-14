using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Intruder/Haunt")]
public class CombatAction_IntruderHaunt : CombatAction
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
        _m.TickHaunt();
        ActionInProgress = true;
    }
}
