using UnityEngine;

public class BossDeadAction : BossCombatAction
{
    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginDead();
    }

    public override void PerformAction()
    {
    }
}