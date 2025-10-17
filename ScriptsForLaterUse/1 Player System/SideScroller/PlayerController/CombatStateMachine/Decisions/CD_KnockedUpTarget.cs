using UnityEngine;

public class CD_KnockedUpTarget : CombatDecision
{
    public override bool Decide()
    {
        return false;
    }

    private bool CanAttackKnockedUpTarget()
    {
        return false;
    }
}