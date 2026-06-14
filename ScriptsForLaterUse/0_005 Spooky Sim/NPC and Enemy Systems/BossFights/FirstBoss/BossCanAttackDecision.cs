using UnityEngine;

public class BossCanAttackDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.RollForAttackStart();
    }
}