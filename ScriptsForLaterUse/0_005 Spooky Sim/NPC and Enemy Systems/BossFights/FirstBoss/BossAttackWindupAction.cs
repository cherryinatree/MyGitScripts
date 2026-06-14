using UnityEngine;

public class BossAttackWindupAction : BossCombatAction
{
    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginAttackWindup();
    }

    public override void PerformAction()
    {
    }
}