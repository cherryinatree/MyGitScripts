using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/In Attack Range AND Job Is Attacker")]
public class CombatDecision_IntruderInAttackRangeAndAttacker : CombatDecision
{
    private IntruderMaster _m;

    private void Awake()
    {
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override bool Decide()
    {
        return _m != null && _m.Job == IntruderMaster.IntruderJob.Attacker && _m.InAttackRange;
    }
}
