using UnityEngine;

public class CA_Idle : CombatAction
{
    public override void Initialization()
    {
        base.Initialization();
    }

    public override void PerformAction()
    {

    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        if(stateMachine.targets != null)
        stateMachine.targets.Clear();
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }
}
