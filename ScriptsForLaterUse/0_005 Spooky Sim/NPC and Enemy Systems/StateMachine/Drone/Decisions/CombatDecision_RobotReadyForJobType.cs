using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Ready For Job Type")]
public class CombatDecision_RobotReadyForJobType : CombatDecision
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
        return _brain.ArrivedAtJob && _brain.CurrentJob == jobType;
    }
}
