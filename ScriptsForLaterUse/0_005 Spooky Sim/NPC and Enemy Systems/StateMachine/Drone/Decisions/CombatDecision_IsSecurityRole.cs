using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Is Security Role")]
public class CombatDecision_IsSecurityRole : CombatDecision
{
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
        return _brain.Role == RobotMaster.RobotRole.Security;
    }
}
