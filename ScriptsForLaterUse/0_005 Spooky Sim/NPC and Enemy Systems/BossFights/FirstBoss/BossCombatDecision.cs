using UnityEngine;

public abstract class BossCombatDecision : CombatDecision
{
    protected PoltergeistBossBrain boss;

    public override void Initialization()
    {
        base.Initialization();
        boss = GetComponentInParent<PoltergeistBossBrain>();
    }
}