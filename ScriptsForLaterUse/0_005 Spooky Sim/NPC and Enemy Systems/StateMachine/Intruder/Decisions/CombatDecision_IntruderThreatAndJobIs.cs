using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Threat AND Job Is")]
public class CombatDecision_IntruderThreatAndJobIs : CombatDecision
{
    [SerializeField] private IntruderMaster.IntruderJob jobType;
    private IntruderMaster _m;

    private void Awake()
    {
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override bool Decide()
    {
        if (_m == null) return false;
        return _m.HasThreat && _m.Job == jobType;
    }
}
