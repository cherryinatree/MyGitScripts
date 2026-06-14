using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Think")]
public class CombatAction_RobotThink : CombatAction
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
        // Thinking is immediate (no coroutine)
        ActionInProgress = false;
    }

    public override void PerformAction()
    {
        if (_brain == null) Initialization();
        if (_brain == null) { ActionInProgress = false; return; }

        _brain.Think();
        ActionInProgress = false;
    }
}
