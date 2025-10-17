using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AID_isHit : AIDecision
{
    private float health;
    private Health myHealth;
    private HealthIntermediate healthIntermediate;
    public override bool Decide()
    {
        if(healthIntermediate.isHit)
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
        healthIntermediate = GetComponent<HealthIntermediate>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        myHealth = GetComponent<Health>();
        health = myHealth.CurrentHealth;
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }


}
