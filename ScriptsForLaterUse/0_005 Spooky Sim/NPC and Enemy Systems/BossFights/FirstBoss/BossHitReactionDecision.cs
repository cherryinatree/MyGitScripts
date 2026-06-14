using UnityEngine;

public class BossHitReactionDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.ConsumePendingHitReaction();
    }
}