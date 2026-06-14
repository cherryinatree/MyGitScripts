using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Is Dead")]
public class CombatDecision_RobotIsDead : CombatDecision
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
        return _brain != null && _brain.IsDead;
    }
}
