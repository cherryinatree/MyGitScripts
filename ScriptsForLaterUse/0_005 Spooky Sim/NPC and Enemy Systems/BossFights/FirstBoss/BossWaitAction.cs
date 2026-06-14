using UnityEngine;

public class BossWaitAction : BossCombatAction
{
    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginWait();
    }

    public override void PerformAction()
    {
    }
}