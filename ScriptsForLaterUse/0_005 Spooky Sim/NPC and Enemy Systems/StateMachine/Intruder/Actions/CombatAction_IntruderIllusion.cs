using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Intruder/Illusion")]
public class CombatAction_IntruderIllusion : CombatAction
{
    private IntruderMaster _m;
    private bool _doneOnce;

    public override void Initialization()
    {
        base.Initialization();
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _doneOnce = false;
        ActionInProgress = true;
    }

    public override void PerformAction()
    {
        if (_m == null) { ActionInProgress = false; return; }

        if (!_doneOnce)
        {
            _doneOnce = true;
            _m.DoIllusionTrickOnce();
        }

        ActionInProgress = !_m.IllusionDone;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        _doneOnce = false;
    }
}
