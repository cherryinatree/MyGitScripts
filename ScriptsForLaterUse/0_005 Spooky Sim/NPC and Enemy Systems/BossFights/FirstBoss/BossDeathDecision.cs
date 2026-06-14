using UnityEngine;

public class BossDeathDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.ConsumePendingDeath();
    }
}