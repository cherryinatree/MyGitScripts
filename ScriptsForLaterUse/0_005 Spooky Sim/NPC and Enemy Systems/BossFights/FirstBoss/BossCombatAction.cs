using UnityEngine;

public abstract class BossCombatAction : CombatAction
{
    protected PoltergeistBossBrain boss;

    public override void Initialization()
    {
        base.Initialization();
        boss = GetComponentInParent<PoltergeistBossBrain>();
    }
}