using UnityEngine;

public class BossStunFinishedDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.IsStunFinished();
    }
}