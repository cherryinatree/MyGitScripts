using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Job Complete")]
public class CombatDecision_JobComplete : CombatDecision
{
    private RobotMaster _brain;

    protected override void Awake()
    {
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override bool Decide()
    {
        if (_brain == null) return false;
        return _brain.JobComplete;
    }
}
