using UnityEngine;

public class BossAttackWindupFinishedDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.IsAttackWindupFinished();
    }
}