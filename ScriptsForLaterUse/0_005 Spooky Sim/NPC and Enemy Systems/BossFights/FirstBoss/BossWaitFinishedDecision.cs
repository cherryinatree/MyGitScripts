using UnityEngine;

public class BossWaitFinishedDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.IsWaitFinished();
    }
}