using UnityEngine;

public class BossAttackRushFinishedDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.AttackRushFinished;
    }
}