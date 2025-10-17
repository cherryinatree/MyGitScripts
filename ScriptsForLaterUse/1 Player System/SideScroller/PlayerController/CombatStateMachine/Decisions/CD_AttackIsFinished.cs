using UnityEngine;

public class CD_AttackIsFinished : CombatDecision
{


    public override bool Decide()
    {
        return stateMachine.isAttackFinished;
    }
}
