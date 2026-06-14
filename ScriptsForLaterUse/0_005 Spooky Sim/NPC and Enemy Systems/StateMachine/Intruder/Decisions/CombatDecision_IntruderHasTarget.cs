using UnityEngine;

public class CombatDecision_IntruderHasTarget : CombatDecision
{
    private IntruderMaster _m;

    private void Awake()
    {
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override bool Decide()
    {
        return _m != null && _m.HasTarget;
    }
}
