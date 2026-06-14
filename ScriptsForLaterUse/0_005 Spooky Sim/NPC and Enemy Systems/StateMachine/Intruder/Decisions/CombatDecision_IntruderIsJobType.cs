using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Is Job Type")]
public class CombatDecision_IntruderIsJobType : CombatDecision
{
    [SerializeField] private IntruderMaster.IntruderJob jobType;
    private IntruderMaster _m;

    private void Awake() => _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();

    public override bool Decide()
    {
        return _m != null && _m.Job == jobType;
    }
}
