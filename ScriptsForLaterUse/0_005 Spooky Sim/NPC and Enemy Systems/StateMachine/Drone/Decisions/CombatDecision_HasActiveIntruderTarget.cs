using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Has Active Intruder Target")]
public class CombatDecision_HasActiveIntruderTarget : CombatDecision
{
    private RobotMaster _brain;

    private void Awake()
    {
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override bool Decide()
    {
        return _brain != null && _brain.HasActiveIntruderTarget;
    }
}
