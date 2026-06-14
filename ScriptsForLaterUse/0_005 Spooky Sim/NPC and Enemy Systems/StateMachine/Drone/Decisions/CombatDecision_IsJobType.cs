using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Is Job Type")]
public class CombatDecision_IsJobType : CombatDecision
{
    [SerializeField] private RobotMaster.JobType jobType;

    private RobotMaster _brain;

    protected override void Awake()
    {
        base.Awake();
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override bool Decide()
    {
        if (_brain == null) return false;
        return _brain.CurrentJob == jobType;
    }
}
