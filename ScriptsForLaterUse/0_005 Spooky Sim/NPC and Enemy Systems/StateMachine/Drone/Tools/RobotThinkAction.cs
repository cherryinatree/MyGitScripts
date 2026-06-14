using UnityEngine;

public class RobotThinkAction : CombatAction
{
    private RobotMaster _brain;

    public override void Initialization()
    {
        base.Initialization();
        _brain = GetComponentInParent<RobotMaster>();
        if (_brain == null) _brain = GetComponent<RobotMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        ActionInProgress = false;
    }

    public override void PerformAction()
    {
        if (_brain == null) Initialization();
        if (_brain == null) return;

        _brain.Think();
        ActionInProgress = false; // thinking is instant
    }
}
