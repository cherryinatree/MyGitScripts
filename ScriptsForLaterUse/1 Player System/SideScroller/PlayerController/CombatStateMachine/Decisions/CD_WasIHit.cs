using MoreMountains.CorgiEngine;
using UnityEngine;

public class CD_WasIHit : CombatDecision
{
    private float health;
    private Health myHealth;
    private HealthIntermediate healthIntermediate;

    public override bool Decide()
    {
        if (healthIntermediate.isHit)
        {
            healthIntermediate.isHit = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Initialization()
    {
        base.Initialization();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        healthIntermediate = GetComponent<HealthIntermediate>();
        myHealth = GetComponent<Health>();
        health = myHealth.CurrentHealth;
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }


}
