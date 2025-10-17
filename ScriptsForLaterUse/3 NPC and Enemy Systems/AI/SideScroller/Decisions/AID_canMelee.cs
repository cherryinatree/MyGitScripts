using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AID_canMelee : AIDecision
{
    private Ai_Eyes eyes;
    [Range(0, 5)] [Tooltip("The range at which the AI can melee the player.")]
    public float meleeRange = 0.9f;

    public override bool Decide()
    {
        if(eyes.lastPlayerSeen != null)
        {
            if(Vector3.Distance(eyes.lastPlayerSeen.transform.position, transform.position) < meleeRange) 
            {
                return true;
            }
            else
            {
                return false;
            }
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
        eyes = GetComponent<Ai_Eyes>();
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }


}
