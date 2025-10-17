using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AID_HasPlayerBeenSpotted : AIDecision
{
    private Ai_Eyes eyes;
    public override bool Decide()
    {
        if (eyes.lastPlayerSeen != null)
        {
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
        eyes = GetComponent<Ai_Eyes>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
    }

    public override void OnExitState()
    {
        base.OnExitState();
    }


}