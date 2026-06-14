using UnityEngine;

public class BossAttackRushAction : BossCombatAction
{
    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginAttackRush();
    }

    public override void PerformAction()
    {
        boss.TickAttackRush();
    }
}