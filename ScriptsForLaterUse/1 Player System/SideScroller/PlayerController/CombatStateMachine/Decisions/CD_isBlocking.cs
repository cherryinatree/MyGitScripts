using UnityEngine;

public class CD_isBlocking : CombatDecision
{

    private HealthIntermediate healthIntermediate;

    public override bool Decide()
    {
        return healthIntermediate.isBlocking;
    }

    public void Start()
    {
        healthIntermediate = GetComponent<HealthIntermediate>();
    }

}