using UnityEngine;

public class BossStunnedAction : BossCombatAction
{
    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginStunned();
    }

    public override void PerformAction()
    {
    }
}